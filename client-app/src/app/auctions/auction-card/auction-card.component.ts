import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { BidRealtimeState, BidStateFeedbackComponent } from '../bid-state-feedback/bid-state-feedback.component';
import { setDefaultAuctionImage } from '../auction-image.util';

export interface AuctionCardViewModel {
  id: string;
  title: string;
  category: string;
  imageUrl: string;
  currentBidAmount: number;
  currency: string;
  bidsCount: number;
  timeLeft: string;
  bidRealtimeState: BidRealtimeState;
  inWatchlist: boolean;
}

@Component({
  selector: 'app-auction-card',
  standalone: true,
  imports: [CommonModule, BidStateFeedbackComponent],
  templateUrl: './auction-card.component.html',
  styleUrl: './auction-card.component.scss'
})
export class AuctionCardComponent {
  @Input({ required: true })
  item!: AuctionCardViewModel;

  @Input()
  bidDisabled = false;

  @Input()
  bidDisabledReason = 'Only bidder accounts can place bids.';

  @Input()
  bidButtonText = 'Place Bid';

  @Output()
  placeBid = new EventEmitter<AuctionCardViewModel>();

  @Output()
  reportAuction = new EventEmitter<AuctionCardViewModel>();

  @Output()
  toggleWatchlist = new EventEmitter<AuctionCardViewModel>();

  onPlaceBid(): void {
    if (this.bidDisabled) {
      return;
    }

    this.placeBid.emit(this.item);
  }

  onReportAuction(): void {
    this.reportAuction.emit(this.item);
  }

  onToggleWatchlist(): void {
    this.toggleWatchlist.emit(this.item);
  }

  onImageError(event: Event): void {
    setDefaultAuctionImage(event);
  }
}
