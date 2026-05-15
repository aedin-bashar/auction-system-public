import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, ElementRef, HostListener, NgZone, OnInit, ViewChild, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { AdminAuctionDetailDto, AdminAuctionManagementService } from '../admin-auction-management.service';
import { API_BASE_URL } from '../../core/api.constants';
import { resolveAuctionImageUrl, setDefaultAuctionImage } from '../../auctions/auction-image.util';

type AuctionBidItem = {
  bidder: string;
  amount: string;
  at: string;
};

type CurrencyOption = {
  value: string;
  label: string;
  countryCode: string;
};

type AdminAuctionDetail = {
  id: string;
  title: string;
  seller: string;
  category: string;
  description: string;
  status: 'Draft' | 'Active' | 'Ended';
  startingPrice: string;
  currentBid: string;
  bidCount: number;
  startedAt: string;
  endsAt: string;
  highestBidder: string;
  imageUrl: string;
  imageCount: number;
  bids: AuctionBidItem[];
};

@Component({
  selector: 'app-admin-auction-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './admin-auction-detail.component.html',
  styleUrl: './admin-auction-detail.component.scss'
})
export class AdminAuctionDetailComponent implements OnInit {
  @ViewChild('currencyDropdown') private currencyDropdown?: ElementRef<HTMLElement>;
  private readonly route = inject(ActivatedRoute);
  private readonly auctionsApi = inject(AdminAuctionManagementService);
  private readonly ngZone = inject(NgZone);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly formBuilder = inject(FormBuilder);
  private readonly apiOrigin = API_BASE_URL.replace(/\/api\/?$/, '');

  auction: AdminAuctionDetail | null = null;
  isLoading = false;
  isSaving = false;
  errorMessage: string | null = null;
  selectedImageFiles: File[] = [];
  selectedImageFileNames: string[] = [];
  currencyMenuOpen = false;
  readonly categoryOptions = [
    'Collectibles', 'Tech', 'Sports', 'Home', 'Gaming', 'Music', 'Luxury', 'Fashion', 'Automotive', 'Art'
  ];
  readonly currencyOptions: CurrencyOption[] = [
    { value: 'USD', label: 'USD', countryCode: 'us' },
    { value: 'EUR', label: 'EUR', countryCode: 'eu' },
    { value: 'GBP', label: 'GBP', countryCode: 'gb' },
    { value: 'SEK', label: 'SEK', countryCode: 'se' },
    { value: 'CNY', label: 'Yuan', countryCode: 'cn' }
  ];
  readonly editForm = this.formBuilder.group({
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(120)]],
    category: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
    description: ['', [Validators.maxLength(2000)]],
    startingPriceAmount: [0, [Validators.required, Validators.min(0)]],
    currency: ['USD', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
    endTimeUtc: ['', [Validators.required]]
  });

  ngOnInit(): void {
    const auctionId = this.route.snapshot.paramMap.get('auctionId');
    if (!auctionId) {
      this.errorMessage = 'Auction id is missing from the route.';
      return;
    }

    this.loadAuction(auctionId);
  }

  canEndAuction(): boolean {
    return !!this.auction && this.auction.status === 'Active' && !this.isSaving;
  }

  canStartAuction(): boolean {
    return !!this.auction && this.auction.status === 'Draft' && !this.isSaving;
  }

  canSaveEdit(): boolean {
    return !!this.auction && this.auction.status !== 'Ended' && !this.isSaving;
  }

  get selectedCurrencyOption(): CurrencyOption | null {
    const current = this.editForm.controls.currency.value;
    return this.currencyOptions.find((item) => item.value === current) ?? null;
  }

  toggleCurrencyMenu(): void {
    this.currencyMenuOpen = !this.currencyMenuOpen;
  }

  selectCurrency(option: CurrencyOption): void {
    this.editForm.controls.currency.setValue(option.value);
    this.currencyMenuOpen = false;
  }

  flagUrl(countryCode: string): string {
    return `https://flagcdn.com/24x18/${countryCode.toLowerCase()}.png`;
  }

  onImageError(event: Event): void {
    setDefaultAuctionImage(event);
  }

  startAuction(): void {
    if (!this.auction || this.auction.status !== 'Draft') {
      return;
    }

    this.errorMessage = null;
    this.isSaving = true;
    this.cdr.detectChanges();

    this.auctionsApi.startAuction(this.auction.id).subscribe({
      next: () => {
        this.ngZone.run(() => {
          this.auction = {
            ...this.auction!,
            status: 'Active',
            startedAt: this.formatDateTime(new Date().toISOString())
          };
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.ngZone.run(() => {
          this.errorMessage = 'Could not start auction. Please try again.';
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  endAuction(): void {
    if (!this.auction || this.auction.status !== 'Active') {
      return;
    }

    this.errorMessage = null;
    this.isSaving = true;
    this.cdr.detectChanges();

    this.auctionsApi.endAuction(this.auction.id).subscribe({
      next: () => {
        this.ngZone.run(() => {
          this.auction = {
            ...this.auction!,
            status: 'Ended'
          };
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.ngZone.run(() => {
          this.errorMessage = 'Could not end auction. Please try again.';
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  onImagesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    this.selectedImageFiles = files;
    this.selectedImageFileNames = files.map((file) => file.name);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.currencyMenuOpen) {
      return;
    }

    const target = event.target as Node | null;
    if (!target) {
      return;
    }

    const currencyHost = this.currencyDropdown?.nativeElement;
    if (!currencyHost || !currencyHost.contains(target)) {
      this.currencyMenuOpen = false;
    }
  }

  saveAuctionChanges(): void {
    if (!this.auction || this.editForm.invalid || this.isSaving) {
      this.editForm.markAllAsTouched();
      return;
    }

    const value = this.editForm.getRawValue();
    const endUtcIso = this.toUtcIso(value.endTimeUtc ?? '');
    if (!endUtcIso) {
      this.errorMessage = 'End date/time is invalid.';
      return;
    }

    this.errorMessage = null;
    this.isSaving = true;
    this.cdr.detectChanges();

    this.auctionsApi.updateAuction(this.auction.id, {
      title: (value.title ?? '').trim(),
      category: (value.category ?? '').trim(),
      description: (value.description ?? '').trim() || null,
      startingPriceAmount: Number(value.startingPriceAmount ?? 0),
      currency: (value.currency ?? '').trim().toUpperCase(),
      endTimeUtc: endUtcIso,
      replaceImages: this.selectedImageFiles.length > 0,
      images: this.selectedImageFiles
    }).subscribe({
      next: () => {
        this.ngZone.run(() => {
          this.selectedImageFiles = [];
          this.selectedImageFileNames = [];
          this.loadAuction(this.auction!.id);
        });
      },
      error: (error) => {
        this.ngZone.run(() => {
          this.errorMessage = this.extractErrorMessage(error);
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  private loadAuction(auctionId: string): void {
    this.errorMessage = null;
    this.isLoading = true;
    this.cdr.detectChanges();

    this.auctionsApi.getAuctionDetail(auctionId).subscribe({
      next: (auction) => {
        this.ngZone.run(() => {
          this.auction = this.mapToViewModel(auction);
          this.patchForm(auction);
          this.isLoading = false;
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.ngZone.run(() => {
          this.auction = null;
          this.errorMessage = 'Could not load auction detail.';
          this.isLoading = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  private patchForm(auction: AdminAuctionDetailDto): void {
    this.editForm.patchValue({
      title: auction.title,
      category: auction.category,
      description: auction.description ?? '',
      startingPriceAmount: auction.startingPriceAmount,
      currency: auction.currency,
      endTimeUtc: this.toDisplayDateTime(auction.endTimeUtc)
    });
  }

  private mapToViewModel(auction: AdminAuctionDetailDto): AdminAuctionDetail {
    return {
      id: auction.auctionId,
      title: auction.title,
      seller: auction.sellerName,
      category: auction.category,
      description: auction.description ?? 'No description provided.',
      status: auction.status,
      startingPrice: this.formatMoney(auction.startingPriceAmount, auction.currency),
      currentBid: this.formatMoney(auction.currentBidAmount, auction.currency),
      bidCount: auction.bidCount,
      startedAt: this.formatDateTime(auction.startTimeUtc),
      endsAt: this.formatDateTime(auction.endTimeUtc),
      highestBidder: auction.highestBidderName ?? 'No bids yet',
      imageUrl: this.resolveImageUrl(auction.auctionId, auction.primaryImageId),
      imageCount: auction.imageCount,
      bids: auction.bids.map((bid) => ({
        bidder: bid.bidderName,
        amount: this.formatMoney(bid.amount, bid.currency),
        at: this.formatDateTime(bid.placedAtUtc)
      }))
    };
  }

  private resolveImageUrl(auctionId: string, primaryImageId: string | null): string {
    return resolveAuctionImageUrl(this.apiOrigin, auctionId, primaryImageId);
  }

  private toDisplayDateTime(valueUtc: string | null): string {
    if (!valueUtc) {
      return '';
    }

    const parsed = new Date(valueUtc);
    if (Number.isNaN(parsed.getTime())) {
      return '';
    }

    const year = parsed.getFullYear();
    const month = (parsed.getMonth() + 1).toString().padStart(2, '0');
    const day = parsed.getDate().toString().padStart(2, '0');

    const hours24 = parsed.getHours();
    const minutes = parsed.getMinutes().toString().padStart(2, '0');
    const meridiem = hours24 >= 12 ? 'PM' : 'AM';
    const hours12 = (hours24 % 12 || 12).toString().padStart(2, '0');

    return `${year}-${month}-${day} ${hours12}:${minutes} ${meridiem}`;
  }

  private toUtcIso(value: string): string | null {
    if (!value) {
      return null;
    }

    const trimmed = value.trim();
    const amPmMatch = /^(\d{4})-(\d{2})-(\d{2}) (\d{2}):(\d{2}) (AM|PM)$/i.exec(trimmed);
    if (amPmMatch) {
      const year = Number(amPmMatch[1]);
      const month = Number(amPmMatch[2]);
      const day = Number(amPmMatch[3]);
      const hours12 = Number(amPmMatch[4]);
      const minutes = Number(amPmMatch[5]);
      const meridiem = amPmMatch[6].toUpperCase();

      if (
        Number.isNaN(year) || Number.isNaN(month) || Number.isNaN(day)
        || Number.isNaN(hours12) || Number.isNaN(minutes)
        || month < 1 || month > 12 || day < 1 || day > 31
        || hours12 < 1 || hours12 > 12 || minutes < 0 || minutes > 59
      ) {
        return null;
      }

      const hours24 = meridiem === 'PM'
        ? (hours12 % 12) + 12
        : (hours12 % 12);

      const local = new Date(year, month - 1, day, hours24, minutes, 0, 0);
      if (Number.isNaN(local.getTime())) {
        return null;
      }

      return local.toISOString();
    }

    // Backward compatibility for previous value style.
    const fallbackLocal = new Date(trimmed);
    if (Number.isNaN(fallbackLocal.getTime())) {
      return null;
    }

    return fallbackLocal.toISOString();
  }

  private extractErrorMessage(error: unknown): string {
    const payload = (error as { error?: { details?: string; title?: string } })?.error;
    if (payload?.details) {
      return payload.details;
    }
    if (payload?.title) {
      return payload.title;
    }
    return 'Could not update auction. Please review the inputs and try again.';
  }

  private formatMoney(amount: number, currency: string): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
      maximumFractionDigits: 2
    }).format(amount);
  }

  private formatDateTime(value: string | null): string {
    if (!value) {
      return 'N/A';
    }

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
      return 'N/A';
    }

    const year = parsed.getFullYear();
    const month = (parsed.getMonth() + 1).toString().padStart(2, '0');
    const day = parsed.getDate().toString().padStart(2, '0');

    const hours24 = parsed.getHours();
    const minutes = parsed.getMinutes().toString().padStart(2, '0');
    const meridiem = hours24 >= 12 ? 'PM' : 'AM';
    const hours12 = (hours24 % 12 || 12).toString().padStart(2, '0');

    return `${year}-${month}-${day}, ${hours12}:${minutes} ${meridiem}`;
  }
}
