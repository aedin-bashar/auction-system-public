import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

import {
  PaymentMethodInput,
  PaymentMethodModalComponent,
  PaymentMethodModalMode,
  PaymentMethodSavePayload
} from '../payment-method-modal/payment-method-modal.component';

type PaymentMethod = PaymentMethodInput;

@Component({
  selector: 'app-payment-methods',
  standalone: true,
  imports: [CommonModule, PaymentMethodModalComponent],
  templateUrl: './payment-methods.component.html',
  styleUrl: './payment-methods.component.scss'
})
export class PaymentMethodsComponent {
  methods: PaymentMethod[] = [
    {
      id: 'pm_1',
      brand: 'Visa',
      holderName: 'John Doe',
      last4: '4242',
      expiry: '10/29',
      isDefault: true
    },
    {
      id: 'pm_2',
      brand: 'Mastercard',
      holderName: 'John Doe',
      last4: '4444',
      expiry: '07/28',
      isDefault: false
    }
  ];

  isModalOpen = false;
  modalMode: PaymentMethodModalMode = 'add';
  selectedMethod: PaymentMethod | null = null;

  openAdd(): void {
    this.modalMode = 'add';
    this.selectedMethod = null;
    this.isModalOpen = true;
  }

  openEdit(method: PaymentMethod): void {
    this.modalMode = 'edit';
    this.selectedMethod = method;
    this.isModalOpen = true;
  }

  openRemove(method: PaymentMethod): void {
    this.modalMode = 'remove';
    this.selectedMethod = method;
    this.isModalOpen = true;
  }

  onModalOpenChange(open: boolean): void {
    this.isModalOpen = open;
  }

  onSaved(payload: PaymentMethodSavePayload): void {
    if (this.modalMode === 'add') {
      const newMethod: PaymentMethod = {
        id: crypto.randomUUID(),
        brand: this.detectBrand(payload.cardNumber),
        holderName: payload.holderName,
        last4: payload.cardNumber.slice(-4),
        expiry: payload.expiry,
        isDefault: payload.makeDefault
      };

      if (newMethod.isDefault) {
        this.methods = this.methods.map((m) => ({ ...m, isDefault: false }));
      }

      this.methods = [newMethod, ...this.methods];
      return;
    }

    if (this.modalMode === 'edit' && payload.id) {
      const cardNumber = payload.cardNumber.trim();
      const hasNewCardNumber = cardNumber.length > 0;

      this.methods = this.methods.map((m) => {
        if (m.id !== payload.id) {
          return payload.makeDefault ? { ...m, isDefault: false } : m;
        }

        return {
          ...m,
          holderName: payload.holderName,
          expiry: payload.expiry,
          isDefault: payload.makeDefault,
          ...(hasNewCardNumber
            ? {
                brand: this.detectBrand(cardNumber),
                last4: cardNumber.slice(-4)
              }
            : {})
        };
      });
    }
  }

  onRemoved(id: string): void {
    const next = this.methods.filter((m) => m.id !== id);
    const removedWasDefault = this.methods.find((m) => m.id === id)?.isDefault ?? false;

    if (removedWasDefault && next.length > 0) {
      next[0] = { ...next[0], isDefault: true };
    }

    this.methods = next;
  }

  trackById(_: number, method: PaymentMethod): string {
    return method.id;
  }

  private detectBrand(cardNumber: string): string {
    if (cardNumber.startsWith('4')) return 'Visa';
    if (/^5[1-5]/.test(cardNumber)) return 'Mastercard';
    if (/^3[47]/.test(cardNumber)) return 'American Express';
    return 'Card';
  }
}
