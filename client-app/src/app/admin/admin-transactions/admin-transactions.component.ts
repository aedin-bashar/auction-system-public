import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, NgZone, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  AdminTransactionDetailDto,
  AdminTransactionListItemDto,
  AdminTransactionManagementService
} from '../admin-transaction-management.service';
import { DeleteTransactionModalComponent } from '../delete-transaction-modal/delete-transaction-modal.component';
import { RefundTransactionModalComponent } from '../refund-transaction-modal/refund-transaction-modal.component';

type TransactionStatus = 'Completed' | 'Refunded';

type AdminTransaction = {
  id: string;
  userId: string;
  user: string;
  type: string;
  amount: number;
  currency: string;
  status: TransactionStatus;
  createdAt: string;
  reference: string | null;
  description: string | null;
  walletBalance: number | null;
  walletBalanceCurrency: string | null;
  refundReason?: string;
};

@Component({
  selector: 'app-admin-transactions',
  standalone: true,
  imports: [CommonModule, FormsModule, RefundTransactionModalComponent, DeleteTransactionModalComponent],
  templateUrl: './admin-transactions.component.html',
  styleUrl: './admin-transactions.component.scss'
})
export class AdminTransactionsComponent implements OnInit {
  private readonly transactionsApi = inject(AdminTransactionManagementService);
  private readonly ngZone = inject(NgZone);
  private readonly cdr = inject(ChangeDetectorRef);

  transactions: AdminTransaction[] = [];
  selectedTransactionId: string | null = null;
  selectedTransaction: AdminTransaction | null = null;
  draftDescription = '';

  isLoading = false;
  isSaving = false;
  errorMessage: string | null = null;

  isRefundModalOpen = false;
  isDeleteModalOpen = false;

  ngOnInit(): void {
    this.loadTransactions();
  }

  selectTransaction(transactionId: string): void {
    if (this.selectedTransactionId === transactionId && this.selectedTransaction?.id === transactionId) {
      return;
    }

    this.selectedTransactionId = transactionId;
    this.loadTransactionDetail(transactionId);
  }

  saveDescription(description?: string): void {
    const selected = this.selectedTransaction;
    if (!selected) {
      return;
    }

    const nextDescription = (description ?? this.draftDescription).trim() || selected.description || null;

    this.selectedTransaction = {
      ...selected,
      description: nextDescription
    };
    this.draftDescription = nextDescription ?? '';
    this.transactions = this.transactions.map((item) =>
      item.id === selected.id
        ? {
            ...item,
            description: nextDescription
          }
        : item
    );
    this.cdr.detectChanges();
  }

  openRefund(): void {
    if (!this.selectedTransaction) {
      return;
    }

    this.isRefundModalOpen = true;
  }

  onRefundModalOpenChange(open: boolean): void {
    this.isRefundModalOpen = open;
  }

  onRefundConfirmed(payload: { id: string; reason: string }): void {
    this.errorMessage = null;
    this.isSaving = true;
    this.cdr.detectChanges();

    this.transactionsApi.refundTransaction(payload.id, payload.reason).subscribe({
      next: (transaction) => {
        this.ngZone.run(() => {
          this.applyTransactionDetail(transaction);
          this.isRefundModalOpen = false;
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.ngZone.run(() => {
          this.errorMessage = 'Could not process refund. Please try again.';
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  openDelete(): void {
    if (!this.selectedTransaction) {
      return;
    }

    this.isDeleteModalOpen = true;
  }

  onDeleteModalOpenChange(open: boolean): void {
    this.isDeleteModalOpen = open;
  }

  onDeleteConfirmed(transactionId: string): void {
    const nextTransactions = this.transactions.filter((transaction) => transaction.id !== transactionId);
    this.transactions = nextTransactions;

    if (this.selectedTransactionId === transactionId) {
      this.selectedTransactionId = nextTransactions[0]?.id ?? null;
      this.selectedTransaction = null;
      if (this.selectedTransactionId) {
        this.loadTransactionDetail(this.selectedTransactionId);
      }
    }
  }

  trackByTransactionId(_: number, transaction: AdminTransaction): string {
    return transaction.id;
  }

  asMoney(amount: number, currency: string): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
      maximumFractionDigits: 2
    }).format(amount);
  }

  asDate(value: string): string {
    return new Date(value).toLocaleString('en-US', {
      month: 'short',
      day: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  private loadTransactions(): void {
    this.errorMessage = null;
    this.isLoading = true;
    this.cdr.detectChanges();

    this.transactionsApi.listTransactions().subscribe({
      next: (transactions) => {
        this.ngZone.run(() => {
          this.transactions = transactions.map((x) => this.mapListItem(x));
          this.selectedTransactionId = this.transactions[0]?.id ?? null;
          this.isLoading = false;

          if (this.selectedTransactionId) {
            this.loadTransactionDetail(this.selectedTransactionId);
          }

          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.ngZone.run(() => {
          this.errorMessage = 'Could not load transactions. Please refresh and try again.';
          this.isLoading = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  private loadTransactionDetail(transactionId: string): void {
    this.errorMessage = null;

    this.transactionsApi.getTransactionDetail(transactionId).subscribe({
      next: (transaction) => {
        this.ngZone.run(() => {
          this.applyTransactionDetail(transaction);
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.ngZone.run(() => {
          this.errorMessage = 'Could not load transaction detail. Please try again.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  private applyTransactionDetail(transaction: AdminTransactionDetailDto): void {
    const detail = this.mapDetailItem(transaction);

    this.selectedTransactionId = detail.id;
    this.selectedTransaction = detail;
    this.draftDescription = detail.description ?? '';

    this.transactions = this.transactions.map((item) =>
      item.id === detail.id
        ? {
            ...item,
            status: detail.status,
            reference: detail.reference,
            description: detail.description,
            walletBalance: detail.walletBalance,
            walletBalanceCurrency: detail.walletBalanceCurrency,
            refundReason: detail.refundReason
          }
        : item
    );
  }

  private mapListItem(item: AdminTransactionListItemDto): AdminTransaction {
    return {
      id: item.transactionId,
      userId: item.userId,
      user: item.userName,
      type: item.type,
      amount: item.amount,
      currency: item.currency,
      status: item.status,
      createdAt: item.createdAtUtc,
      reference: null,
      description: null,
      walletBalance: null,
      walletBalanceCurrency: null,
      refundReason: undefined
    };
  }

  private mapDetailItem(item: AdminTransactionDetailDto): AdminTransaction {
    return {
      id: item.transactionId,
      userId: item.userId,
      user: item.userName,
      type: item.type,
      amount: item.amount,
      currency: item.currency,
      status: item.status,
      createdAt: item.createdAtUtc,
      reference: item.reference,
      description: item.description,
      walletBalance: item.walletBalanceAmount,
      walletBalanceCurrency: item.walletBalanceCurrency,
      refundReason: item.refundReason ?? undefined
    };
  }
}
