import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../core/api.constants';

export type AdminAuctionStatus = 'Draft' | 'Active' | 'Ended';

export interface AdminAuctionListItemDto {
  auctionId: string;
  title: string;
  sellerId: string;
  sellerName: string;
  category: string;
  currentBidAmount: number;
  currency: string;
  bidCount: number;
  endTimeUtc: string;
  status: AdminAuctionStatus;
}

export interface AdminAuctionBidDto {
  bidId: string;
  bidderId: string;
  bidderName: string;
  amount: number;
  currency: string;
  placedAtUtc: string;
}

export interface AdminAuctionDetailDto {
  auctionId: string;
  title: string;
  sellerId: string;
  sellerName: string;
  category: string;
  description: string | null;
  startingPriceAmount: number;
  currency: string;
  currentBidAmount: number;
  bidCount: number;
  startTimeUtc: string | null;
  endTimeUtc: string;
  endedAtUtc: string | null;
  status: AdminAuctionStatus;
  highestBidderName: string | null;
  primaryImageId: string | null;
  imageCount: number;
  bids: AdminAuctionBidDto[];
}

export interface UpdateAdminAuctionRequest {
  title: string;
  category: string;
  description: string | null;
  startingPriceAmount: number;
  currency: string;
  endTimeUtc: string;
  replaceImages: boolean;
  images: File[];
}

@Injectable({ providedIn: 'root' })
export class AdminAuctionManagementService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/admin/auctions`;

  listAuctions(): Observable<AdminAuctionListItemDto[]> {
    return this.http.get<AdminAuctionListItemDto[]>(this.baseUrl);
  }

  getAuctionDetail(auctionId: string): Observable<AdminAuctionDetailDto> {
    return this.http.get<AdminAuctionDetailDto>(`${this.baseUrl}/${auctionId}`);
  }

  endAuction(auctionId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${auctionId}/end`, {});
  }

  startAuction(auctionId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${auctionId}/start`, {});
  }

  deleteAuction(auctionId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${auctionId}`);
  }

  updateAuction(auctionId: string, request: UpdateAdminAuctionRequest): Observable<void> {
    const form = new FormData();
    form.append('title', request.title);
    form.append('category', request.category);
    if (request.description) {
      form.append('description', request.description);
    }
    form.append('startingPriceAmount', String(request.startingPriceAmount));
    form.append('currency', request.currency);
    form.append('endTimeUtc', request.endTimeUtc);
    form.append('replaceImages', String(request.replaceImages));
    for (const image of request.images) {
      form.append('images', image, image.name);
    }

    return this.http.put<void>(`${this.baseUrl}/${auctionId}`, form);
  }
}
