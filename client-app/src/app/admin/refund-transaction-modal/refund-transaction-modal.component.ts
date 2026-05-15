import { CommonModule } from '@angular/common';
import { Component, EventEmitter, HostListener, Input, Output, booleanAttribute } from '@angular/core';
import { FormsModule } from '@angular/forms';

export type RefundableTransaction = {
  id: string;
  user: string;
  amount: string;
  status: 'Completed' | 'Pending' | 'Failed' | 'Refunded';
};

export type RefundTransactionPayload = {
  id: string;
  reason: string;
};

@Component({
  selector: 'app-refund-transaction-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './refund-transaction-modal.component.html',
  styleUrl: './refund-transaction-modal.component.scss'
})
export class RefundTransactionModalComponent {
  @Input({ transform: booleanAttribute })
  open = false;

  @Input()
  transaction: RefundableTransaction | null = null;

  @Output()
  openChange = new EventEmitter<boolean>();

  @Output()
  confirmed = new EventEmitter<RefundTransactionPayload>();

  reason = '';
  errorMessage: string | null = null;

  onBackdropClick(): void {
    this.close();
  }

  onDialogClick(event: MouseEvent): void {
    event.stopPropagation();
  }

  cancel(): void {
    this.close();
  }

  submit(): void {
    if (!this.transaction) {
      return;
    }

    const reason = this.reason.trim();
    if (reason.length < 5) {
      this.errorMessage = 'Reason must be at least 5 characters.';
      return;
    }

    this.confirmed.emit({
      id: this.transaction.id,
      reason
    });

    this.close();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.open) return;
    this.close();
  }

  private close(): void {
    this.open = false;
    this.openChange.emit(false);
    this.reason = '';
    this.errorMessage = null;
  }
}
