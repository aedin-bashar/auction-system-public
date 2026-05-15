import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';

import { ActiveAuctionDto, AuctionsService } from '../auctions.service';
import { AuthService } from '../../auth/auth.service';
import { AuctionCardViewModel } from '../auction-card/auction-card.component';
import { PlaceBidModalComponent } from '../place-bid-modal/place-bid-modal.component';
import { SignalRService } from '../signalr-service/signalr.service';
import { WatchlistService } from './watchlist.service';
import { API_BASE_URL } from '../../core/api.constants';
import { resolveAuctionImageUrl, setDefaultAuctionImage } from '../auction-image.util';

interface WatchlistItem {
  id: string;
  title: string;
  category: string;
  imageUrl: string;
  currentBid: number;
  minNextBid: number;
  currency: string;
  timeLeft: string;
  bidsCount: number;
  trend: 'rising' | 'steady';
}

@Component({
  selector: 'app-watchlist',
  standalone: true,
  imports: [CommonModule, PlaceBidModalComponent],
  templateUrl: './watchlist.component.html',
  styleUrl: './watchlist.component.scss'
})
export class WatchlistComponent implements OnInit, OnDestroy {
  private readonly auctionsApi = inject(AuctionsService);
  private readonly authService = inject(AuthService);
  private readonly watchlistService = inject(WatchlistService);
  private readonly signalRService = inject(SignalRService);
  private readonly router = inject(Router);
  private readonly subscriptions = new Subscription();
  private readonly activeAuctionGroupIds = new Set<string>();
  private readonly hubUrl = API_BASE_URL.replace('/api', '/hubs/auctions');
  private readonly apiOrigin = API_BASE_URL.replace(/\/api\/?$/, '');
  private refreshTimerId: ReturnType<typeof setInterval> | null = null;
  private isBackgroundRefreshInFlight = false;

  items: WatchlistItem[] = [];
  isLoading = true;
  loadError: string | null = null;
  selectedBidItem: WatchlistItem | null = null;
  selectedBidModalItem: AuctionCardViewModel | null = null;
  placeBidModalOpen = false;
  bidRequestPending = false;
  bidRequestError: string | null = null;

  get isAuthenticated(): boolean {
    return this.authService.getSession() !== null;
  }

  get canCurrentUserBid(): boolean {
    return this.authService.getSession()?.role === 'Bidder';
  }

  ngOnInit(): void {
    this.watchlistService.reload();
    this.loadWatchlist(false);

    this.subscriptions.add(this.watchlistService.ids$.subscribe(() => {
      this.loadWatchlist(true);
    }));

    this.subscriptions.add(this.signalRService.bidPlaced$.subscribe((event) => {
      if (!this.items.some((item) => item.id === event.auctionId)) {
        return;
      }

      this.loadWatchlist(true);
    }));

    this.startRefreshPolling();
    void this.connectRealtime();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    this.stopRefreshPolling();
    void this.leaveAllAuctionGroups();
    void this.signalRService.disconnect();
  }

  get totalWatching(): number {
    return this.items.length;
  }

  get endingSoonCount(): number {
    return this.items.filter((item) => item.timeLeft.startsWith('00h') || item.timeLeft.startsWith('01h')).length;
  }

  trackById(_: number, item: WatchlistItem): string {
    return item.id;
  }

  isRising(item: WatchlistItem): boolean {
    return item.trend === 'rising';
  }

  onImageError(event: Event): void {
    setDefaultAuctionImage(event);
  }

  onRemove(item: WatchlistItem): void {
    this.watchlistService.remove(item.id);
  }

  onPlaceBid(item: WatchlistItem): void {
    const session = this.authService.getSession();
    if (!session) {
      void this.router.navigate(['/login']);
      return;
    }

    if (session.role !== 'Bidder') {
      return;
    }

    this.selectedBidItem = item;
    this.selectedBidModalItem = this.toBidModalItem(item);
    this.bidRequestError = null;
    this.placeBidModalOpen = true;
    this.stopRefreshPolling();
  }

  onPlaceBidModalOpenChange(open: boolean): void {
    this.placeBidModalOpen = open;

    if (!open) {
      this.bidRequestPending = false;
      this.bidRequestError = null;
      this.selectedBidItem = null;
      this.selectedBidModalItem = null;
      this.startRefreshPolling();
    }
  }

  onPlaceBidSubmitted(amount: number): void {
    const selectedItem = this.selectedBidItem;
    if (!selectedItem || this.bidRequestPending) {
      return;
    }

    const currentItem = this.items.find((item) => item.id === selectedItem.id) ?? selectedItem;

    const session = this.authService.getSession();
    if (!session) {
      this.onPlaceBidModalOpenChange(false);
      void this.router.navigate(['/login']);
      return;
    }

    this.bidRequestPending = true;
    this.bidRequestError = null;

    this.auctionsApi.placeBid(currentItem.id, {
      amount,
      currency: currentItem.currency
    }).subscribe({
      next: (result) => {
        this.applyBidResultToVisibleItem(currentItem.id, result.currentPriceAmount, result.currentPriceCurrency);

        this.bidRequestPending = false;
        this.onPlaceBidModalOpenChange(false);
        this.loadWatchlist(true);
      },
      error: (err) => {
        this.bidRequestError = this.extractBidErrorMessage(err);
        this.bidRequestPending = false;

        if (this.bidRequestError.toLowerCase().includes('current price')) {
          this.loadWatchlist(true);
        }

        if (err instanceof HttpErrorResponse && (err.status === 401 || err.status === 403)) {
          void this.router.navigate(['/login']);
        }
      }
    });
  }

  private loadWatchlist(background: boolean): void {
    if (background && this.isBackgroundRefreshInFlight) {
      return;
    }

    const watchedIds = this.watchlistService.getIds();
    if (!background) {
      this.isLoading = true;
      this.loadError = null;
    } else {
      this.isBackgroundRefreshInFlight = true;
    }

    if (watchedIds.size === 0) {
      this.items = [];
      this.isLoading = false;
      this.loadError = null;
      if (background) {
        this.isBackgroundRefreshInFlight = false;
      }
      return;
    }

    this.subscriptions.add(this.auctionsApi.getActiveAuctions({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: (auctions) => {
        this.items = auctions
          .filter((auction) => watchedIds.has(auction.id))
          .map((auction) => this.mapItem(auction))
          .sort((a, b) => a.timeLeft.localeCompare(b.timeLeft));

        this.refreshSelectedBidItemFromVisibleList();

        this.isLoading = false;
        this.loadError = null;
        if (background) {
          this.isBackgroundRefreshInFlight = false;
        }
        void this.joinAuctionGroupsForItems();
      },
      error: () => {
        if (!background) {
          this.items = [];
          this.isLoading = false;
          this.loadError = 'Could not load watchlist right now. Please try again.';
        }

        if (background) {
          this.isBackgroundRefreshInFlight = false;
        }
      }
    }));
  }

  private mapItem(auction: ActiveAuctionDto): WatchlistItem {
    return {
      id: auction.id,
      title: auction.title,
      category: auction.category,
      imageUrl: resolveAuctionImageUrl(this.apiOrigin, auction.id, auction.primaryImageId),
      currentBid: auction.priceAmount,
      minNextBid: Number((auction.priceAmount + 1).toFixed(2)),
      currency: auction.currency,
      timeLeft: this.formatTimeLeft(auction.endTimeUtc),
      bidsCount: auction.bidCount,
      trend: auction.bidCount > 0 ? 'rising' : 'steady'
    };
  }

  private async connectRealtime(): Promise<void> {
    try {
      await this.signalRService.connect(this.hubUrl);
      await this.joinAuctionGroupsForItems();
    } catch {
      // no-op: polling still keeps watchlist fresh
    }
  }

  private async joinAuctionGroupsForItems(): Promise<void> {
    const idsToJoin = this.items
      .map((item) => item.id)
      .filter((id) => !this.activeAuctionGroupIds.has(id));

    for (const auctionId of idsToJoin) {
      try {
        await this.signalRService.joinAuction(auctionId);
        this.activeAuctionGroupIds.add(auctionId);
      } catch {
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

  private startRefreshPolling(): void {
    this.stopRefreshPolling();
    this.refreshTimerId = setInterval(() => {
      this.loadWatchlist(true);
    }, 5000);
  }

  private stopRefreshPolling(): void {
    if (this.refreshTimerId) {
      clearInterval(this.refreshTimerId);
      this.refreshTimerId = null;
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

  private refreshSelectedBidItemFromVisibleList(): void {
    if (!this.selectedBidItem) {
      return;
    }

    const refreshed = this.items.find((item) => item.id === this.selectedBidItem!.id);
    if (!refreshed) {
      return;
    }

    this.selectedBidItem = refreshed;
    this.selectedBidModalItem = this.toBidModalItem(refreshed);
  }

  private applyBidResultToVisibleItem(auctionId: string, priceAmount: number, currency: string): void {
    const target = this.items.find((item) => item.id === auctionId);
    if (!target) {
      return;
    }

    target.currentBid = priceAmount;
    target.currency = currency;
    target.minNextBid = Number((priceAmount + 1).toFixed(2));
    target.bidsCount += 1;
    target.trend = 'rising';
  }

  private toBidModalItem(item: WatchlistItem | null): AuctionCardViewModel | null {
    if (!item) {
      return null;
    }

    return {
      id: item.id,
      title: item.title,
      category: item.category,
      imageUrl: item.imageUrl,
      currentBidAmount: item.currentBid,
      currency: item.currency,
      bidsCount: item.bidsCount,
      timeLeft: item.timeLeft,
      bidRealtimeState: null,
      inWatchlist: true
    };
  }

  private extractBidErrorMessage(error: unknown): string {
    const fallbackMessage = 'Could not place your bid. Please try again.';

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
    }

    return fallbackMessage;
  }
}
