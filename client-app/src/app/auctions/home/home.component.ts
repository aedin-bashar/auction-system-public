import { CommonModule } from '@angular/common';
import { HttpContext, HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';

import { AuctionCardComponent, AuctionCardViewModel } from '../auction-card/auction-card.component';
import { SidebarFilterComponent, SidebarFilterValue } from '../sidebar-filter/sidebar-filter.component';
import { ActiveAuctionDto, AuctionsService, ReportAuctionRequest } from '../auctions.service';
import { AuthService } from '../../auth/auth.service';
import { Router } from '@angular/router';
import { PlaceBidModalComponent } from '../place-bid-modal/place-bid-modal.component';
import { ReportAuctionModalComponent } from '../report-auction-modal/report-auction-modal.component';
import { SignalRService } from '../signalr-service/signalr.service';
import { API_BASE_URL } from '../../core/api.constants';
import { WatchlistService } from '../watchlist/watchlist.service';
import { resolveAuctionImageUrl } from '../auction-image.util';
import { SKIP_LOADING } from '../../core/loading.interceptor';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, AuctionCardComponent, SidebarFilterComponent, PlaceBidModalComponent, ReportAuctionModalComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit, OnDestroy {
  // ─── Page configuration ────────────────────────────────────────────────────
  /** How many auctions are fetched from the API in one request. */
  private static readonly ActiveAuctionsPageSize = 200;
  /** How many auction cards are displayed per page. Change this to adjust page size. */
  readonly itemsPerPage = 6;
  // ───────────────────────────────────────────────────────────────────────────

  private readonly cdr = inject(ChangeDetectorRef);
  private readonly auctionsApi = inject(AuctionsService);
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly signalRService = inject(SignalRService);
  private readonly watchlistService = inject(WatchlistService);
  private readonly subscriptions = new Subscription();
  private readonly activeAuctionGroupIds = new Set<string>();
  private readonly userBidAuctionIds = new Set<string>();
  private readonly currentUserId = this.authService.getSession()?.userId ?? null;
  private readonly hubUrl = API_BASE_URL.replace('/api', '/hubs/auctions');
  private readonly apiOrigin = API_BASE_URL.replace(/\/api\/?$/, '');
  private refreshTimerId: ReturnType<typeof setInterval> | null = null;

  items: AuctionCardViewModel[] = [];
  categories: string[] = [];
  filteredItems: AuctionCardViewModel[] = [];
  currentPage = 1;
  selectedBidItem: AuctionCardViewModel | null = null;
  placeBidModalOpen = false;
  bidRequestPending = false;
  bidRequestError: string | null = null;
  selectedReportItem: AuctionCardViewModel | null = null;
  reportModalOpen = false;
  reportRequestPending = false;
  reportRequestError: string | null = null;
  reportRequestSuccess: string | null = null;
  private currentFilter: SidebarFilterValue = { categories: [], minPrice: null, maxPrice: null };
  private searchTerm = '';

  get isAuthenticated(): boolean {
    return this.authService.getSession() !== null;
  }

  get canCurrentUserBid(): boolean {
    return this.authService.getSession()?.role === 'Bidder';
  }

  get pagedItems(): AuctionCardViewModel[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredItems.slice(start, start + this.itemsPerPage);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredItems.length / this.itemsPerPage));
  }

  get visiblePageNumbers(): (number | '...')[] {
    const total = this.totalPages;
    const current = this.currentPage;
    if (total <= 7) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }
    const pages: (number | '...')[] = [1];
    if (current > 3) pages.push('...');
    const rangeStart = Math.max(2, current - 1);
    const rangeEnd = Math.min(total - 1, current + 1);
    for (let i = rangeStart; i <= rangeEnd; i++) pages.push(i);
    if (current < total - 2) pages.push('...');
    pages.push(total);
    return pages;
  }

  onPageChange(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage) return;
    this.currentPage = page;
    this.cdr.markForCheck();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  ngOnInit(): void {
    this.subscriptions.add(this.route.queryParamMap.subscribe((params) => {
      this.searchTerm = (params.get('search') ?? '').trim().toLowerCase();
      this.applyFilters();
    }));

    this.subscriptions.add(this.auctionsApi.getActiveAuctions({ pageNumber: 1, pageSize: HomeComponent.ActiveAuctionsPageSize }).subscribe({
      next: (auctions) => {
        this.items = auctions.map((auction) => this.mapToCard(auction));
        this.applyWatchlistState();
        this.categories = [...new Set(this.items.map((item) => item.category))]
          .sort((left, right) => left.localeCompare(right));
        void this.joinAuctionGroupsForItems();
        this.applyFilters();
      },
      error: () => {
        this.items = [];
        this.categories = [];
        this.filteredItems = [];
      }
    }));

    this.subscriptions.add(this.signalRService.bidPlaced$.subscribe((event) => {
      this.onRealtimeBidPlaced(event.auctionId, event.bidderId, event.currentPriceAmount, event.currentPriceCurrency);
    }));

    this.subscriptions.add(this.watchlistService.ids$.subscribe(() => {
      this.applyWatchlistState();
      this.applyFilters(true);
    }));

    this.startAuctionRefreshPolling();
    void this.connectRealtime();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    this.stopAuctionRefreshPolling();
    void this.leaveAllAuctionGroups();
    void this.signalRService.disconnect();
  }

  onFilterChanged(filter: SidebarFilterValue): void {
    this.currentFilter = filter;
    this.applyFilters();
  }

  onPlaceBid(item: AuctionCardViewModel): void {
    const session = this.authService.getSession();
    if (!session) {
      void this.router.navigate(['/login']);
      return;
    }

    if (session.role !== 'Bidder') {
      return;
    }

    this.selectedBidItem = item;
    this.bidRequestError = null;
    this.placeBidModalOpen = true;
  }

  onReportAuction(item: AuctionCardViewModel): void {
    const session = this.authService.getSession();
    if (!session) {
      void this.router.navigate(['/login']);
      return;
    }

    this.selectedReportItem = item;
    this.reportRequestError = null;
    this.reportRequestSuccess = null;
    this.reportModalOpen = true;
  }

  onToggleWatchlist(item: AuctionCardViewModel): void {
    const session = this.authService.getSession();
    if (!session) {
      void this.router.navigate(['/login']);
      return;
    }

    this.watchlistService.toggle(item.id);
  }

  onPlaceBidModalOpenChange(open: boolean): void {
    this.placeBidModalOpen = open;

    if (!open) {
      this.bidRequestPending = false;
      this.bidRequestError = null;
      this.selectedBidItem = null;
    }
  }

  onReportModalOpenChange(open: boolean): void {
    this.reportModalOpen = open;

    if (!open) {
      this.reportRequestPending = false;
      this.reportRequestError = null;
      this.selectedReportItem = null;
    }
  }

  onPlaceBidSubmitted(amount: number): void {
    const selectedItem = this.selectedBidItem;
    if (!selectedItem || this.bidRequestPending) {
      return;
    }

    const session = this.authService.getSession();
    if (!session) {
      this.onPlaceBidModalOpenChange(false);
      void this.router.navigate(['/login']);
      return;
    }

    this.bidRequestPending = true;
    this.bidRequestError = null;

    this.auctionsApi
      .placeBid(selectedItem.id, {
        amount,
        currency: selectedItem.currency
      })
      .subscribe({
        next: (result) => {
          this.userBidAuctionIds.add(selectedItem.id);

          const idx = this.items.findIndex((item) => item.id === selectedItem.id);
          if (idx !== -1) {
            this.items[idx] = {
              ...this.items[idx],
              currentBidAmount: result.currentPriceAmount,
              currency: result.currentPriceCurrency,
              bidRealtimeState: 'winning'
            };
          }

          this.bidRequestPending = false;
          this.onPlaceBidModalOpenChange(false);
          this.applyFilters(true);
        },
        error: (err) => {
          this.bidRequestError = this.extractApiErrorMessage(err, 'Could not place your bid. Please try again.');
          this.bidRequestPending = false;

          if (
            this.bidRequestError.toLowerCase().includes('greater than the current price')
            && this.selectedBidItem
          ) {
            this.refreshAuctionFromApi(this.selectedBidItem.id);
          }

          if (err instanceof HttpErrorResponse && (err.status === 401 || err.status === 403)) {
            this.authService.logout();
            void this.router.navigate(['/login']);
          }
        }
      });
  }

  onReportAuctionSubmitted(request: ReportAuctionRequest): void {
    const selectedItem = this.selectedReportItem;
    if (!selectedItem || this.reportRequestPending) {
      return;
    }

    const session = this.authService.getSession();
    if (!session) {
      this.onReportModalOpenChange(false);
      void this.router.navigate(['/login']);
      return;
    }

    this.reportRequestPending = true;
    this.reportRequestError = null;
    this.reportRequestSuccess = null;

    this.auctionsApi.reportAuction(selectedItem.id, request).subscribe({
      next: () => {
        this.reportRequestPending = false;
        this.onReportModalOpenChange(false);
        this.reportRequestSuccess = `Report submitted for ${selectedItem.title}. Our team will review it.`;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.reportRequestError = this.extractApiErrorMessage(err, 'Could not submit your report. Please try again.');
        this.reportRequestPending = false;

        if (err instanceof HttpErrorResponse && (err.status === 401 || err.status === 403)) {
          this.authService.logout();
          void this.router.navigate(['/login']);
        }
      }
    });
  }

  dismissReportFeedback(): void {
    this.reportRequestSuccess = null;
  }

  private applyFilters(preservePage = false): void {
    const filter = this.currentFilter;
    this.filteredItems = this.items.filter((item) => {
      var categoryMatch = filter.categories.length === 0 || filter.categories.includes(item.category);
      var minMatch = filter.minPrice === null || item.currentBidAmount >= filter.minPrice;
      var maxMatch = filter.maxPrice === null || item.currentBidAmount <= filter.maxPrice;
      var searchMatch = this.searchTerm.length === 0
        || item.title.toLowerCase().includes(this.searchTerm)
        || item.category.toLowerCase().includes(this.searchTerm);

      return categoryMatch && minMatch && maxMatch && searchMatch;
    });

    const nextTotalPages = Math.max(1, Math.ceil(this.filteredItems.length / this.itemsPerPage));

    if (!preservePage) {
      this.currentPage = 1;
    } else if (this.currentPage > nextTotalPages) {
      this.currentPage = nextTotalPages;
    }

    this.cdr.markForCheck();
  }

  trackById(_: number, item: AuctionCardViewModel): string {
    return item.id;
  }

  private mapToCard(auction: ActiveAuctionDto): AuctionCardViewModel {
    return {
      id: auction.id,
      title: auction.title,
      category: auction.category,
      imageUrl: this.resolveImageUrl(auction.id, auction.primaryImageId),
      currentBidAmount: auction.priceAmount,
      currency: auction.currency,
      bidsCount: auction.bidCount,
      timeLeft: this.formatTimeLeft(auction.endTimeUtc),
      bidRealtimeState: null,
      inWatchlist: false
    };
  }

  private resolveImageUrl(auctionId: string, primaryImageId: string | null): string {
    return resolveAuctionImageUrl(this.apiOrigin, auctionId, primaryImageId);
  }

  private async connectRealtime(): Promise<void> {
    try {
      await this.signalRService.connect(this.hubUrl);
      await this.joinAuctionGroupsForItems();
    } catch {
      // no-op: UI continues to work without realtime updates
    }
  }

  private async joinAuctionGroupsForItems(): Promise<void> {
    if (this.items.length === 0) {
      return;
    }

    const idsToJoin = this.items
      .map((item) => item.id)
      .filter((auctionId) => !this.activeAuctionGroupIds.has(auctionId));

    for (const auctionId of idsToJoin) {
      try {
        await this.signalRService.joinAuction(auctionId);
        this.activeAuctionGroupIds.add(auctionId);
      } catch {
        // Continue joining remaining auctions; a single failure should not disable all realtime groups.
        continue;
      }
    }
  }

  private async leaveAllAuctionGroups(): Promise<void> {
    const ids = [...this.activeAuctionGroupIds];
    for (const auctionId of ids) {
      try {
        await this.signalRService.leaveAuction(auctionId);
      } catch {
        // ignore cleanup failures
      }
    }

    this.activeAuctionGroupIds.clear();
  }

  private onRealtimeBidPlaced(
    auctionId: string,
    bidderId: string,
    currentPriceAmount: number,
    currentPriceCurrency: string
  ): void {
    const index = this.items.findIndex((item) => item.id === auctionId);
    if (index === -1) {
      return;
    }

    const target = this.items[index];
    let bidRealtimeState = target.bidRealtimeState;

    if (this.currentUserId) {
      if (bidderId === this.currentUserId) {
        this.userBidAuctionIds.add(auctionId);
        bidRealtimeState = 'winning';
      } else if (this.userBidAuctionIds.has(auctionId)) {
        bidRealtimeState = 'outbid';
      }
    }

    this.items[index] = {
      ...target,
      currentBidAmount: currentPriceAmount,
      currency: currentPriceCurrency,
      bidsCount: target.bidsCount + 1,
      bidRealtimeState
    };

    this.applyFilters(true);
  }

  private refreshAuctionFromApi(auctionId: string): void {
    const silentContext = new HttpContext().set(SKIP_LOADING, true);
    this.subscriptions.add(this.auctionsApi.getActiveAuctions({ pageNumber: 1, pageSize: HomeComponent.ActiveAuctionsPageSize }, silentContext).subscribe({
      next: (auctions) => {
        const refreshed = auctions.find((auction) => auction.id === auctionId);
        if (!refreshed) {
          return;
        }

        const index = this.items.findIndex((item) => item.id === auctionId);
        if (index === -1) {
          return;
        }

        this.items[index] = {
          ...this.items[index],
          currentBidAmount: refreshed.priceAmount,
          currency: refreshed.currency,
          timeLeft: this.formatTimeLeft(refreshed.endTimeUtc),
          bidsCount: refreshed.bidCount
        };

        this.bidRequestError = `Current bid updated to ${refreshed.priceAmount.toFixed(2)} ${refreshed.currency}. Enter a higher amount.`;
        this.applyFilters(true);
      },
      error: () => {
        // no-op: keep existing message if refresh fails
      }
    }));
  }

  private extractApiErrorMessage(error: unknown, fallbackMessage: string): string {
    if (!(error instanceof HttpErrorResponse)) {
      return fallbackMessage;
    }

    if (error.status === 0) {
      return 'Cannot reach the API server. Ensure backend is running and reachable.';
    }

    if (error.status === 401 || error.status === 403) {
      return 'Your session expired or is unauthorized. Please sign in again.';
    }

    const payload = error.error as unknown;

    if (payload && typeof payload === 'object') {
      const details = (payload as { details?: unknown }).details;

      if (typeof details === 'string' && details.trim().length > 0) {
        return details;
      }

      if (Array.isArray(details)) {
        const firstValidationMessage = details
          .map((item) => {
            if (typeof item === 'string') {
              return item;
            }

            if (item && typeof item === 'object' && 'errorMessage' in item) {
              const message = (item as { errorMessage?: unknown }).errorMessage;
              return typeof message === 'string' ? message : null;
            }

            return null;
          })
          .find((message) => typeof message === 'string' && message.trim().length > 0);

        if (firstValidationMessage) {
          return firstValidationMessage;
        }
      }

      const title = (payload as { title?: unknown }).title;
      if (typeof title === 'string' && title.trim().length > 0) {
        return title;
      }
    }

    return fallbackMessage;
  }

  private startAuctionRefreshPolling(): void {
    this.stopAuctionRefreshPolling();
    this.refreshTimerId = setInterval(() => {
      this.refreshVisibleAuctionsFromApi();
    }, 5000);
  }

  private stopAuctionRefreshPolling(): void {
    if (this.refreshTimerId) {
      clearInterval(this.refreshTimerId);
      this.refreshTimerId = null;
    }
  }

  private refreshVisibleAuctionsFromApi(): void {
    if (this.items.length === 0) {
      return;
    }

    const silentContext = new HttpContext().set(SKIP_LOADING, true);
    this.subscriptions.add(this.auctionsApi.getActiveAuctions({ pageNumber: 1, pageSize: HomeComponent.ActiveAuctionsPageSize }, silentContext).subscribe({
      next: (auctions) => {
        const byId = new Map(auctions.map((auction) => [auction.id, auction]));
        let changed = false;

        for (let i = 0; i < this.items.length; i++) {
          const item = this.items[i];
          const refreshed = byId.get(item.id);
          if (!refreshed) {
            continue;
          }

          const nextTimeLeft = this.formatTimeLeft(refreshed.endTimeUtc);
          const priceChanged = item.currentBidAmount !== refreshed.priceAmount || item.currency !== refreshed.currency;
          const bidsChanged = item.bidsCount !== refreshed.bidCount;
          const timeChanged = item.timeLeft !== nextTimeLeft;

          if (priceChanged || bidsChanged || timeChanged) {
            this.items[i] = {
              ...item,
              currentBidAmount: refreshed.priceAmount,
              currency: refreshed.currency,
              bidsCount: refreshed.bidCount,
              timeLeft: nextTimeLeft
            };
            changed = true;
          }
        }

        this.applyWatchlistState();
        if (changed) {
          this.applyFilters(true);
        }
      },
      error: () => {
        // no-op: realtime may still keep data current
      }
    }));
  }

  private applyWatchlistState(): void {
    const watchedIds = this.watchlistService.getIds();
    for (let i = 0; i < this.items.length; i++) {
      const item = this.items[i];
      const inWatchlist = watchedIds.has(item.id);
      if (item.inWatchlist !== inWatchlist) {
        this.items[i] = { ...item, inWatchlist };
      }
    }
  }

  private formatTimeLeft(endTimeUtc: string): string {
    const endTime = new Date(endTimeUtc).getTime();
    const now = Date.now();
    const diffMs = Math.max(0, endTime - now);
    const totalMinutes = Math.floor(diffMs / 60000);
    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;

    return `${hours.toString().padStart(2, '0')}h ${minutes.toString().padStart(2, '0')}m`;
  }
}
