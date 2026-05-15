import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  booleanAttribute
} from '@angular/core';

import { AuctionCardViewModel } from '../auction-card/auction-card.component';

@Component({
  selector: 'app-place-bid-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './place-bid-modal.component.html',
  styleUrl: './place-bid-modal.component.scss'
})
export class PlaceBidModalComponent implements OnChanges {
  private static readonly MinBidIncrement = 1;

  @Input({ transform: booleanAttribute })
  open = false;

  @Input()
  item: AuctionCardViewModel | null = null;

  @Input({ transform: booleanAttribute })
  pending = false;

  @Input()
  serverError: string | null = null;

  @Input()
  bidRealtimeState: 'winning' | 'outbid' | null = null;

  @Output()
  openChange = new EventEmitter<boolean>();

  @Output()
  submitted = new EventEmitter<number>();

  bidAmount = '';
  errorMessage: string | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open']) {
      this.open ? this.lockScroll() : this.unlockScroll();
    }

    if ((changes['open'] || changes['item']) && this.open) {
      this.errorMessage = null;
      this.bidAmount = this.minimumBid.toFixed(2);
    }
  }

  get minimumBid(): number {
    const currentAmount = this.item?.currentBidAmount ?? 0;
    return Number((currentAmount + PlaceBidModalComponent.MinBidIncrement).toFixed(2));
  }

  onBackdropClick(): void {
    this.close();
  }

  onDialogClick(event: MouseEvent): void {
    event.stopPropagation();
  }

  cancel(): void {
    if (this.pending) {
      return;
    }

    this.close();
  }

  submit(): void {
    this.errorMessage = null;

    const parsedAmount = Number.parseFloat(this.bidAmount);
    if (Number.isNaN(parsedAmount)) {
      this.errorMessage = 'Enter a valid bid amount.';
      return;
    }

    const minimumAmount = this.minimumBid;
    if (parsedAmount < minimumAmount) {
      this.errorMessage = `Bid amount must be at least ${minimumAmount.toFixed(2)} ${this.item?.currency ?? ''}.`;
      return;
    }

    this.submitted.emit(Number(parsedAmount.toFixed(2)));
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.open || this.pending) {
      return;
    }

    this.close();
  }

  private close(): void {
    this.errorMessage = null;
    this.open = false;
    this.openChange.emit(false);
    this.unlockScroll();
  }

  private lockScroll(): void {
    if (typeof document === 'undefined') {
      return;
    }

    document.body.style.overflow = 'hidden';
  }

  private unlockScroll(): void {
    if (typeof document === 'undefined') {
      return;
    }

    document.body.style.overflow = '';
  }
}
