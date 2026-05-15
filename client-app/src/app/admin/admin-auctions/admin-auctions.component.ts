import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, NgZone, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { AdminAuctionManagementService, AdminAuctionStatus } from '../admin-auction-management.service';
import { DeleteAuctionModalComponent } from '../delete-auction-modal/delete-auction-modal.component';

type AdminAuctionListItem = {
  id: string;
  title: string;
  seller: string;
  category: string;
  currentBid: string;
  bidCount: number;
  endsAt: string;
  status: AdminAuctionStatus;
};

@Component({
  selector: 'app-admin-auctions',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, DeleteAuctionModalComponent],
  templateUrl: './admin-auctions.component.html',
  styleUrl: './admin-auctions.component.scss'
})
export class AdminAuctionsComponent implements OnInit {
  private readonly auctionsApi = inject(AdminAuctionManagementService);
  private readonly ngZone = inject(NgZone);
  private readonly cdr = inject(ChangeDetectorRef);

  auctions: AdminAuctionListItem[] = [];
  isLoading = false;
  isSaving = false;
  errorMessage: string | null = null;

  titleFilter = '';
  statusFilter: 'all' | AdminAuctionStatus = 'all';

  isDeleteModalOpen = false;
  selectedAuction: AdminAuctionListItem | null = null;

  get filteredAuctions(): AdminAuctionListItem[] {
    const title = this.titleFilter.trim().toLowerCase();
    return this.auctions.filter((auction) => {
      const matchesTitle = !title || auction.title.toLowerCase().includes(title) || auction.seller.toLowerCase().includes(title);
      const matchesStatus = this.statusFilter === 'all' || auction.status === this.statusFilter;
      return matchesTitle && matchesStatus;
    });
  }

  ngOnInit(): void {
    this.loadAuctions();
  }

  openDelete(auction: AdminAuctionListItem): void {
    this.selectedAuction = auction;
    this.isDeleteModalOpen = true;
  }

  onDeleteOpenChange(open: boolean): void {
    this.isDeleteModalOpen = open;
  }

  onAuctionDeleted(auctionId: string): void {
    this.errorMessage = null;
    this.isSaving = true;
    this.cdr.detectChanges();

    this.auctionsApi.deleteAuction(auctionId).subscribe({
      next: () => {
        this.ngZone.run(() => {
          this.auctions = this.auctions.filter((auction) => auction.id !== auctionId);
          this.selectedAuction = null;
          this.isDeleteModalOpen = false;
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.ngZone.run(() => {
          this.errorMessage = 'Could not delete auction. Please try again.';
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  trackByAuctionId(_: number, auction: AdminAuctionListItem): string {
    return auction.id;
  }

  private loadAuctions(): void {
    this.errorMessage = null;
    this.isLoading = true;
    this.cdr.detectChanges();

    this.auctionsApi.listAuctions().subscribe({
      next: (auctions) => {
        this.ngZone.run(() => {
          this.auctions = auctions.map((auction) => ({
            id: auction.auctionId,
            title: auction.title,
            seller: auction.sellerName,
            category: auction.category,
            currentBid: this.formatMoney(auction.currentBidAmount, auction.currency),
            bidCount: auction.bidCount,
            endsAt: this.formatDateTime(auction.endTimeUtc),
            status: auction.status
          }));
          this.isLoading = false;
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.ngZone.run(() => {
          this.errorMessage = 'Could not load auctions. Please refresh and try again.';
          this.isLoading = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  private formatMoney(amount: number, currency: string): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
      maximumFractionDigits: 2
    }).format(amount);
  }

  private formatDateTime(value: string): string {
    return new Date(value).toLocaleString('en-US', {
      month: 'short',
      day: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
