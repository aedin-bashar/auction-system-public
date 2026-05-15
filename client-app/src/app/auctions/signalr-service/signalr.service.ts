import { Injectable, NgZone, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';

export type SignalRConnectionState =
  | 'disconnected'
  | 'connecting'
  | 'connected'
  | 'reconnecting';

export interface BidPlacedRealtimeEvent {
  auctionId: string;
  bidId: string;
  bidderId: string;
  amount: number;
  currency: string;
  placedAtUtc: string;
  currentPriceAmount: number;
  currentPriceCurrency: string;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly ngZone = inject(NgZone);
  private hubConnection: signalR.HubConnection | null = null;
  private connectPromise: Promise<void> | null = null;
  private readonly joinedAuctionIds = new Set<string>();

  private readonly connectionStateSubject = new BehaviorSubject<SignalRConnectionState>('disconnected');
  private readonly bidPlacedSubject = new Subject<BidPlacedRealtimeEvent>();

  readonly connectionState$ = this.connectionStateSubject.asObservable();
  readonly bidPlaced$ = this.bidPlacedSubject.asObservable();

  async connect(hubUrl: string): Promise<void> {
    if (this.isDisabledForE2E()) {
      this.connectionStateSubject.next('disconnected');
      return;
    }

    if (this.hubConnection) {
      return;
    }

    if (this.connectPromise) {
      return this.connectPromise;
    }

    this.connectionStateSubject.next('connecting');

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { withCredentials: false })
      .withAutomaticReconnect()
      .build();

    connection.onreconnecting(() => {
      this.ngZone.run(() => this.connectionStateSubject.next('reconnecting'));
    });

    connection.onreconnected(async () => {
      this.ngZone.run(() => this.connectionStateSubject.next('connected'));
      await this.rejoinAuctionsAfterReconnect();
    });

    connection.onclose(() => {
      this.ngZone.run(() => {
        this.connectionStateSubject.next('disconnected');
        this.hubConnection = null;
      });
    });

    connection.on('BidPlaced', (payload: BidPlacedRealtimeEvent) => {
      this.ngZone.run(() => this.bidPlacedSubject.next(payload));
    });

    this.connectPromise = this.startConnectionWithRetry(connection, 5, 1200);

    try {
      await this.connectPromise;
      this.hubConnection = connection;
      this.connectionStateSubject.next('connected');
      await this.rejoinAuctionsAfterReconnect();
    } finally {
      this.connectPromise = null;
    }
  }

  async disconnect(): Promise<void> {
    if (!this.hubConnection) {
      return;
    }

    const connection = this.hubConnection;
    this.hubConnection = null;

    await connection.stop();
    this.connectionStateSubject.next('disconnected');
  }

  async joinAuction(auctionId: string): Promise<void> {
    if (!auctionId || auctionId.trim().length === 0) {
      throw new Error('Auction id is required.');
    }

    const normalizedId = auctionId.trim();
    this.joinedAuctionIds.add(normalizedId);

    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('SignalR connection is not established.');
    }

    await this.hubConnection.invoke('JoinAuction', normalizedId);
  }

  async leaveAuction(auctionId: string): Promise<void> {
    if (!auctionId || auctionId.trim().length === 0) {
      return;
    }

    const normalizedId = auctionId.trim();
    this.joinedAuctionIds.delete(normalizedId);

    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('SignalR connection is not established.');
    }

    await this.hubConnection.invoke('LeaveAuction', normalizedId);
  }

  private async rejoinAuctionsAfterReconnect(): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    const auctionIds = [...this.joinedAuctionIds];
    for (const auctionId of auctionIds) {
      try {
        await this.hubConnection.invoke('JoinAuction', auctionId);
      } catch {
        // Keep trying on future reconnect cycles.
      }
    }
  }

  private async startConnectionWithRetry(
    connection: signalR.HubConnection,
    maxAttempts: number,
    delayMs: number
  ): Promise<void> {
    let attempt = 1;
    while (attempt <= maxAttempts) {
      try {
        await connection.start();
        return;
      } catch (error) {
        if (attempt === maxAttempts) {
          this.connectionStateSubject.next('disconnected');
          throw error;
        }

        await this.delay(delayMs * attempt);
        attempt += 1;
      }
    }
  }

  private async delay(ms: number): Promise<void> {
    await new Promise<void>((resolve) => setTimeout(resolve, ms));
  }

  private isDisabledForE2E(): boolean {
    if (typeof window === 'undefined') {
      return false;
    }

    return !!(window as Window & { __AUCTION_E2E_DISABLE_SIGNALR__?: boolean }).__AUCTION_E2E_DISABLE_SIGNALR__;
  }
}
