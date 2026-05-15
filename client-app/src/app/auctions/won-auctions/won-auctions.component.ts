import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { DEFAULT_AUCTION_IMAGE_URL, setDefaultAuctionImage } from '../auction-image.util';

interface WonAuctionItem {
  id: string;
  title: string;
  imageUrl: string;
  wonAtLabel: string;
  finalPrice: number;
  myWinningBid: number;
  currency: string;
  seller: string;
  paymentStatus: 'Paid' | 'Pending';
}

@Component({
  selector: 'app-won-auctions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './won-auctions.component.html',
  styleUrl: './won-auctions.component.scss'
})
export class WonAuctionsComponent {
  readonly items: WonAuctionItem[] = [
    {
      id: 'won_001',
      title: 'Limited Edition Chronograph Watch',
      imageUrl: DEFAULT_AUCTION_IMAGE_URL,
      wonAtLabel: 'Won 2 days ago',
      finalPrice: 2480,
      myWinningBid: 2480,
      currency: 'USD',
      seller: 'Luxe Time Vault',
      paymentStatus: 'Paid'
    },
    {
      id: 'won_002',
      title: 'Rare First-Press Vinyl Collection',
      imageUrl: DEFAULT_AUCTION_IMAGE_URL,
      wonAtLabel: 'Won 6 days ago',
      finalPrice: 890,
      myWinningBid: 890,
      currency: 'USD',
      seller: 'Analog Archive',
      paymentStatus: 'Paid'
    },
    {
      id: 'won_003',
      title: 'Custom Neon Gaming Chair',
      imageUrl: DEFAULT_AUCTION_IMAGE_URL,
      wonAtLabel: 'Won 1 hour ago',
      finalPrice: 540,
      myWinningBid: 540,
      currency: 'USD',
      seller: 'CyberSeat Studio',
      paymentStatus: 'Pending'
    }
  ];

  get totalWonCount(): number {
    return this.items.length;
  }

  get totalSpent(): number {
    return this.items.reduce((accumulator, item) => accumulator + item.finalPrice, 0);
  }

  isPaid(item: WonAuctionItem): boolean {
    return item.paymentStatus === 'Paid';
  }

  trackById(_: number, item: WonAuctionItem): string {
    return item.id;
  }

  onImageError(event: Event): void {
    setDefaultAuctionImage(event);
  }
}
