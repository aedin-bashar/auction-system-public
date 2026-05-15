import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

import { AuthService } from '../../auth/auth.service';

@Injectable({ providedIn: 'root' })
export class WatchlistService {
  private static readonly StoragePrefix = 'auction.watchlist.';
  private readonly authService = inject(AuthService);
  private readonly idsSubject = new BehaviorSubject<Set<string>>(new Set());

  readonly ids$: Observable<Set<string>> = this.idsSubject.asObservable();

  constructor() {
    this.reload();
  }

  getIds(): Set<string> {
    return new Set(this.idsSubject.value);
  }

  isWatched(auctionId: string): boolean {
    return this.idsSubject.value.has(auctionId);
  }

  add(auctionId: string): boolean {
    if (!this.isAuthenticated() || !auctionId) {
      return false;
    }

    const next = new Set(this.idsSubject.value);
    next.add(auctionId);
    this.persist(next);
    return true;
  }

  remove(auctionId: string): boolean {
    if (!this.isAuthenticated() || !auctionId) {
      return false;
    }

    const next = new Set(this.idsSubject.value);
    const removed = next.delete(auctionId);
    if (!removed) {
      return false;
    }

    this.persist(next);
    return true;
  }

  toggle(auctionId: string): boolean {
    if (!this.isAuthenticated() || !auctionId) {
      return false;
    }

    if (this.isWatched(auctionId)) {
      this.remove(auctionId);
      return true;
    }

    this.add(auctionId);
    return true;
  }

  reload(): void {
    if (!this.isAuthenticated()) {
      this.idsSubject.next(new Set());
      return;
    }

    const raw = localStorage.getItem(this.storageKey());
    if (!raw) {
      this.idsSubject.next(new Set());
      return;
    }

    try {
      const parsed = JSON.parse(raw) as unknown;
      if (!Array.isArray(parsed)) {
        this.idsSubject.next(new Set());
        return;
      }

      const ids = new Set(
        parsed
          .filter((value): value is string => typeof value === 'string')
          .map((value) => value.trim())
          .filter((value) => value.length > 0)
      );

      this.idsSubject.next(ids);
    } catch {
      this.idsSubject.next(new Set());
    }
  }

  private persist(ids: Set<string>): void {
    localStorage.setItem(this.storageKey(), JSON.stringify([...ids]));
    this.idsSubject.next(new Set(ids));
  }

  private storageKey(): string {
    const userId = this.authService.getSession()?.userId ?? 'anonymous';
    return `${WatchlistService.StoragePrefix}${userId}`;
  }

  private isAuthenticated(): boolean {
    return !!this.authService.getSession();
  }
}
