import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../core/api.constants';

export type AdminTransactionStatus = 'Completed' | 'Refunded';

export interface AdminTransactionListItemDto {
  transactionId: string;
  userId: string;
  userName: string;
  type: string;
  amount: number;
  currency: string;
  status: AdminTransactionStatus;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface AdminTransactionDetailDto {
  transactionId: string;
  userId: string;
  userName: string;
  type: string;
  amount: number;
  currency: string;
  status: AdminTransactionStatus;
  reference: string | null;
  description: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  refundedAtUtc: string | null;
  refundedBy: string | null;
  refundReason: string | null;
  walletBalanceAmount: number | null;
  walletBalanceCurrency: string | null;
}

@Injectable({ providedIn: 'root' })
export class AdminTransactionManagementService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/admin/transactions`;

  listTransactions(): Observable<AdminTransactionListItemDto[]> {
    return this.http.get<AdminTransactionListItemDto[]>(this.baseUrl);
  }

  getTransactionDetail(transactionId: string): Observable<AdminTransactionDetailDto> {
    return this.http.get<AdminTransactionDetailDto>(`${this.baseUrl}/${transactionId}`);
  }

  refundTransaction(transactionId: string, reason: string): Observable<AdminTransactionDetailDto> {
    return this.http.post<AdminTransactionDetailDto>(`${this.baseUrl}/${transactionId}/refund`, { reason });
  }
}
