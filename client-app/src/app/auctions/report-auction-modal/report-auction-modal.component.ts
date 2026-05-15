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

export interface ReportAuctionRequest {
  reason: string;
  details: string | null;
}

@Component({
  selector: 'app-report-auction-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './report-auction-modal.component.html',
  styleUrl: './report-auction-modal.component.scss'
})
export class ReportAuctionModalComponent implements OnChanges {
  readonly reasons = [
    'Suspicious listing',
    'Counterfeit concern',
    'Prohibited item',
    'Misleading description',
    'Other'
  ];

  @Input({ transform: booleanAttribute })
  open = false;

  @Input()
  item: AuctionCardViewModel | null = null;

  @Input({ transform: booleanAttribute })
  pending = false;

  @Input()
  serverError: string | null = null;

  @Output()
  openChange = new EventEmitter<boolean>();

  @Output()
  submitted = new EventEmitter<ReportAuctionRequest>();

  reason = this.reasons[0];
  details = '';
  errorMessage: string | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open']) {
      this.open ? this.lockScroll() : this.unlockScroll();
    }

    if ((changes['open'] || changes['item']) && this.open) {
      this.errorMessage = null;
      this.reason = this.reasons[0];
      this.details = '';
    }
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
    const reason = this.reason.trim();
    const details = this.details.trim();

    this.errorMessage = null;

    if (reason.length < 3) {
      this.errorMessage = 'Choose a valid reason for the report.';
      return;
    }

    if (details.length > 1000) {
      this.errorMessage = 'Details must be 1000 characters or fewer.';
      return;
    }

    this.submitted.emit({
      reason,
      details: details.length > 0 ? details : null
    });
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