import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { Subscription } from 'rxjs';

import { AuctionsService, MyBidItemDto } from '../auctions.service';
import { SignalRService } from '../signalr-service/signalr.service';
import { API_BASE_URL } from '../../core/api.constants';
import { resolveAuctionImageUrl, setDefaultAuctionImage } from '../auction-image.util';

interface MyBidItem {
  auctionId: string;
  title: string;
  imageUrl: string;
  endTimeLabel: string;
  myMaxBid: number;
  currentHighestBid: number;
  currency: string;
  bidsCount: number;
}

@Component({
  selector: 'app-my-bids',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './my-bids.component.html',
  styleUrl: './my-bids.component.scss'
})
export class MyBidsComponent implements OnInit, OnDestroy {
  private readonly auctionsApi = inject(AuctionsService);
  private readonly signalRService = inject(SignalRService);
  private readonly subscriptions = new Subscription();
  private readonly activeAuctionGroupIds = new Set<string>();
  private readonly hubUrl = API_BASE_URL.replace('/api', '/hubs/auctions');
  private readonly apiOrigin = API_BASE_URL.replace(/\/api\/?$/, '');
  private isBackgroundRefreshInFlight = false;

  items: MyBidItem[] = [];
  isLoading = true;
  loadError: string | null = null;

  ngOnInit(): void {
    this.loadMyBids(false);

    this.subscriptions.add(this.signalRService.bidPlaced$.subscribe((event) => {
      if (!this.items.some((item) => item.auctionId === event.auctionId)) {
        return;
      }

      this.loadMyBids(true);
    }));

    void this.connectRealtime();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    void this.leaveAllAuctionGroups();
    void this.signalRService.disconnect();
  }

  get winningCount(): number {
    return this.items.filter((item) => this.isWinning(item)).length;
  }

  get outbidCount(): number {
    return this.items.length - this.winningCount;
  }

  isWinning(item: MyBidItem): boolean {
    return item.myMaxBid >= item.currentHighestBid;
  }

  getStatusLabel(item: MyBidItem): string {
    return this.isWinning(item) ? 'Winning' : 'Outbid';
  }

  getOutbidDifference(item: MyBidItem): number {
    if (this.isWinning(item)) {
      return 0;
    }

    return item.currentHighestBid - item.myMaxBid;
  }

  trackByAuctionId(_: number, item: MyBidItem): string {
    return item.auctionId;
  }

  onImageError(event: Event): void {
    setDefaultAuctionImage(event);
  }

  private loadMyBids(background: boolean): void {
    if (background && this.isBackgroundRefreshInFlight) {
      return;
    }

    if (background) {
      this.isBackgroundRefreshInFlight = true;
    } else {
      this.isLoading = true;
    }

    this.subscriptions.add(this.auctionsApi.getMyBids().subscribe({
      next: (items) => {
        this.items = items.map((item) => this.mapItem(item));
        this.loadError = null;
        this.isLoading = false;
        void this.joinAuctionGroupsForItems();

        if (background) {
          this.isBackgroundRefreshInFlight = false;
        }
      },
      error: () => {
        if (!background) {
          this.items = [];
          this.loadError = 'Could not load your bids right now. Please try again.';
          this.isLoading = false;
        }

        if (background) {
          this.isBackgroundRefreshInFlight = false;
        }
      }
    }));
  }

  private async connectRealtime(): Promise<void> {
    try {
      await this.signalRService.connect(this.hubUrl);
      await this.joinAuctionGroupsForItems();
    } catch {
      // no-op: My Bids still works with API-driven refresh.
    }
  }

  private async joinAuctionGroupsForItems(): Promise<void> {
    if (this.items.length === 0) {
      return;
    }

    const idsToJoin = this.items
      .map((item) => item.auctionId)
      .filter((auctionId) => !this.activeAuctionGroupIds.has(auctionId));

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

  private mapItem(item: MyBidItemDto): MyBidItem {
    return {
      auctionId: item.auctionId,
      title: item.title,
      imageUrl: resolveAuctionImageUrl(this.apiOrigin, item.auctionId, item.primaryImageId),
      endTimeLabel: this.formatEndTimeLabel(item.endTimeUtc),
      myMaxBid: item.myMaxBidAmount,
      currentHighestBid: item.currentHighestBidAmount,
      currency: item.currency,
      bidsCount: item.bidCount
    };
  }

  private formatEndTimeLabel(endTimeUtc: string): string {
    const endMs = new Date(endTimeUtc).getTime();
    const nowMs = Date.now();

    if (Number.isNaN(endMs) || endMs <= nowMs) {
      return 'Ended';
    }

    const diffMinutes = Math.floor((endMs - nowMs) / 60000);
    const hours = Math.floor(diffMinutes / 60);
    const minutes = diffMinutes % 60;
    return `Ends in ${hours.toString().padStart(2, '0')}h ${minutes.toString().padStart(2, '0')}m`;
  }

}
