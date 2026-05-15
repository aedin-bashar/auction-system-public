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
import { FormsModule } from '@angular/forms';

export type PaymentMethodModalMode = 'add' | 'edit' | 'remove';

export type PaymentMethodInput = {
  id: string;
  brand: string;
  holderName: string;
  last4: string;
  expiry: string;
  isDefault: boolean;
};

export type PaymentMethodSavePayload = {
  id?: string;
  holderName: string;
  cardNumber: string;
  expiry: string;
  makeDefault: boolean;
};

@Component({
  selector: 'app-payment-method-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payment-method-modal.component.html',
  styleUrl: './payment-method-modal.component.scss'
})
export class PaymentMethodModalComponent implements OnChanges {
  @Input({ transform: booleanAttribute })
  open = false;

  @Input()
  mode: PaymentMethodModalMode = 'add';

  @Input()
  method: PaymentMethodInput | null = null;

  @Output()
  openChange = new EventEmitter<boolean>();

  @Output()
  saved = new EventEmitter<PaymentMethodSavePayload>();

  @Output()
  removed = new EventEmitter<string>();

  holderName = '';
  cardNumber = '';
  expiry = '';
  cvv = '';
  makeDefault = false;
  errorMessage: string | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open']) {
      this.open ? this.lockScroll() : this.unlockScroll();
    }

    if (changes['open'] || changes['mode'] || changes['method']) {
      this.resetForm();
    }
  }

  get title(): string {
    if (this.mode === 'add') return 'Add Payment Method';
    if (this.mode === 'edit') return 'Edit Payment Method';
    return 'Remove Payment Method';
  }

  get description(): string {
    if (this.mode === 'add') return 'Add a new card to your wallet.';
    if (this.mode === 'edit') return 'Update your card details.';
    return 'This action will permanently remove this card.';
  }

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
    this.errorMessage = null;

    if (this.mode === 'remove') {
      if (this.method) {
        this.removed.emit(this.method.id);
      }

      this.close();
      return;
    }

    const holder = this.holderName.trim();
    const cardDigits = this.cardNumber.replace(/\s+/g, '');
    const expiry = this.expiry.trim();

    if (!holder || holder.length > 100) {
      this.errorMessage = 'Card holder name is required (max 100 characters).';
      return;
    }

    const isAdd = this.mode === 'add';
    if (isAdd && !/^\d{13,19}$/.test(cardDigits)) {
      this.errorMessage = 'Enter a valid card number (13-19 digits).';
      return;
    }

    if (!/^(0[1-9]|1[0-2])\/\d{2}$/.test(expiry)) {
      this.errorMessage = 'Use expiry format MM/YY.';
      return;
    }

    if (isAdd && !/^\d{3,4}$/.test(this.cvv.trim())) {
      this.errorMessage = 'Enter a valid CVV (3-4 digits).';
      return;
    }

    this.saved.emit({
      id: this.method?.id,
      holderName: holder,
      cardNumber: cardDigits,
      expiry,
      makeDefault: this.makeDefault
    });

    this.close();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.open) return;
    this.close();
  }

  private resetForm(): void {
    this.errorMessage = null;

    if (!this.open) {
      return;
    }

    this.holderName = this.method?.holderName ?? '';
    this.cardNumber = '';
    this.expiry = this.method?.expiry ?? '';
    this.cvv = '';
    this.makeDefault = this.method?.isDefault ?? this.mode === 'add';
  }

  private close(): void {
    this.open = false;
    this.openChange.emit(false);
    this.unlockScroll();
    this.errorMessage = null;
  }

  private lockScroll(): void {
    if (typeof document === 'undefined') return;
    document.body.style.overflow = 'hidden';
  }

  private unlockScroll(): void {
    if (typeof document === 'undefined') return;
    document.body.style.overflow = '';
  }
}
