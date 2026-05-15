import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../core/api.constants';

export interface ActiveAuctionDto {
  id: string;
  sellerId: string;
  title: string;
  category: string;
  description: string | null;
  primaryImageId: string | null;
  priceAmount: number;
  currency: string;
  endTimeUtc: string;
  bidCount: number;
}

export interface ActiveAuctionQuery {
  category?: string;
  minPrice?: number;
  maxPrice?: number;
  pageNumber?: number;
  pageSize?: number;
}

export interface PlaceBidRequest {
  amount: number;
  currency: string;
}

export interface ReportAuctionRequest {
  reason: string;
  details: string | null;
}

export interface CreateAuctionRequest {
  title: string;
  category: string;
  description: string | null;
  startingPriceAmount: number;
  currency: string;
  endTimeUtc: string;
  images: File[];
}

export interface PlaceBidResultDto {
  bidId: string;
  auctionId: string;
  bidderId: string;
  amount: number;
  currency: string;
  placedAtUtc: string;
  currentPriceAmount: number;
  currentPriceCurrency: string;
}

export interface MyBidItemDto {
  auctionId: string;
  title: string;
  category: string;
  myMaxBidAmount: number;
  currentHighestBidAmount: number;
  currency: string;
  bidCount: number;
  endTimeUtc: string;
  primaryImageId: string | null;
}

@Injectable({ providedIn: 'root' })
export class AuctionsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/auctions`;

  getActiveAuctions(query: ActiveAuctionQuery = {}, context?: HttpContext): Observable<ActiveAuctionDto[]> {
    let params = new HttpParams();

    if (query.category) params = params.set('category', query.category);
    if (typeof query.minPrice === 'number') params = params.set('minPrice', query.minPrice);
    if (typeof query.maxPrice === 'number') params = params.set('maxPrice', query.maxPrice);
    if (typeof query.pageNumber === 'number') params = params.set('pageNumber', query.pageNumber);
    if (typeof query.pageSize === 'number') params = params.set('pageSize', query.pageSize);

    return this.http.get<ActiveAuctionDto[]>(this.baseUrl, { params, ...(context ? { context } : {}) });
  }

  placeBid(auctionId: string, request: PlaceBidRequest): Observable<PlaceBidResultDto> {
    return this.http.post<PlaceBidResultDto>(`${this.baseUrl}/${auctionId}/bids`, request);
  }

  reportAuction(auctionId: string, request: ReportAuctionRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${auctionId}/reports`, request);
  }

  getMyBids(): Observable<MyBidItemDto[]> {
    return this.http.get<MyBidItemDto[]>(`${this.baseUrl}/my-bids`);
  }

  createAuction(request: CreateAuctionRequest): Observable<string> {
    const form = new FormData();
    form.append('title', request.title);
    form.append('category', request.category);
    if (request.description) {
      form.append('description', request.description);
    }
    form.append('startingPriceAmount', String(request.startingPriceAmount));
    form.append('currency', request.currency);
    form.append('endTimeUtc', request.endTimeUtc);
    for (const image of request.images) {
      form.append('images', image, image.name);
    }

    return this.http.post<string>(this.baseUrl, form);
  }
}
