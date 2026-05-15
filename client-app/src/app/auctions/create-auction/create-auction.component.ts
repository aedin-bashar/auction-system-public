import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { AfterViewInit, Component, ElementRef, HostListener, OnDestroy, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import flatpickr from 'flatpickr';
import type { Instance as FlatpickrInstance } from 'flatpickr/dist/types/instance';
import { Subscription } from 'rxjs';

import { AuctionsService } from '../auctions.service';

type CategoryOption = {
  value: string;
  label: string;
  icon: string;
};

type CurrencyOption = {
  value: string;
  label: string;
  countryCode: string;
};

@Component({
  selector: 'app-create-auction',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create-auction.component.html',
  styleUrl: './create-auction.component.scss'
})
export class CreateAuctionComponent implements AfterViewInit, OnDestroy {
  @ViewChild('endDateInput', { static: true }) private endDateInput?: ElementRef<HTMLInputElement>;
  @ViewChild('imageUploadInput') private imageUploadInput?: ElementRef<HTMLInputElement>;
  @ViewChild('categoryDropdown') private categoryDropdown?: ElementRef<HTMLElement>;
  @ViewChild('currencyDropdown') private currencyDropdown?: ElementRef<HTMLElement>;
  isDraggingFile = false;
  selectedFileName: string | null = null;
  selectedImageFile: File | null = null;
  selectedImagePreviewUrl: string | null = null;
  isSubmitting = false;
  submitError: string | null = null;
  categoryMenuOpen = false;
  currencyMenuOpen = false;
  readonly categoryOptions: CategoryOption[] = [
    { value: 'Collectibles', label: 'Collectibles', icon: 'fa-solid fa-gem' },
    { value: 'Tech', label: 'Tech', icon: 'fa-solid fa-microchip' },
    { value: 'Sports', label: 'Sports', icon: 'fa-solid fa-football' },
    { value: 'Home', label: 'Home', icon: 'fa-solid fa-house' },
    { value: 'Gaming', label: 'Gaming', icon: 'fa-solid fa-gamepad' },
    { value: 'Music', label: 'Music', icon: 'fa-solid fa-music' },
    { value: 'Luxury', label: 'Luxury', icon: 'fa-solid fa-crown' },
    { value: 'Fashion', label: 'Fashion', icon: 'fa-solid fa-shirt' },
    { value: 'Automotive', label: 'Automotive', icon: 'fa-solid fa-car-side' },
    { value: 'Art', label: 'Art', icon: 'fa-solid fa-palette' }
  ];
  readonly currencyOptions: CurrencyOption[] = [
    { value: 'USD', label: 'USD', countryCode: 'us' },
    { value: 'EUR', label: 'EUR', countryCode: 'eu' },
    { value: 'GBP', label: 'GBP', countryCode: 'gb' },
    { value: 'SEK', label: 'SEK', countryCode: 'se' },
    { value: 'CNY', label: 'Yuan', countryCode: 'cn' }
  ];
  readonly form;
  private readonly formValueChangesSubscription: Subscription;
  private endDatePicker: FlatpickrInstance | null = null;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly auctionsService: AuctionsService,
    private readonly router: Router
  ) {
    this.form = this.formBuilder.group({
      title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(120)]],
      category: [this.categoryOptions[4].value, [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      description: ['', [Validators.maxLength(2000)]],
      startingPriceAmount: [0, [Validators.required, Validators.min(0)]],
      currency: ['USD', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
      endDateUtc: ['', [Validators.required]]
    });

    this.formValueChangesSubscription = this.form.valueChanges.subscribe(() => {
      if (this.submitError) {
        this.submitError = null;
      }
    });
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDraggingFile = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDraggingFile = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDraggingFile = false;

    const file = event.dataTransfer?.files?.item(0);
    if (!file) {
      return;
    }

    this.setSelectedImage(file);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.item(0);

    if (!file) {
      this.clearSelectedImage();
      return;
    }

    this.setSelectedImage(file);
  }

  removeSelectedImage(): void {
    this.clearSelectedImage();

    const fileInput = this.imageUploadInput?.nativeElement;
    if (fileInput) {
      fileInput.value = '';
    }
  }

  onSubmit(): void {
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      this.submitError = this.getValidationMessage();
      return;
    }

    const formValue = this.form.getRawValue();
    const endTimeUtcIso = this.toUtcIso(formValue.endDateUtc);

    if (!endTimeUtcIso) {
      this.submitError = 'End date and time is invalid.';
      return;
    }

    this.isSubmitting = true;
    this.submitError = null;

    this.auctionsService.createAuction({
      title: (formValue.title ?? '').trim(),
      category: (formValue.category ?? '').trim(),
      description: (formValue.description ?? '').trim() || null,
      startingPriceAmount: Number(formValue.startingPriceAmount ?? 0),
      currency: (formValue.currency ?? '').trim().toUpperCase(),
      endTimeUtc: endTimeUtcIso,
      images: this.selectedImageFile ? [this.selectedImageFile] : []
    }).subscribe({
      next: () => {
        this.isSubmitting = false;
        void this.router.navigate(['/']);
      },
      error: (error) => {
        this.isSubmitting = false;
        this.submitError = this.extractCreateErrorMessage(error);
      }
    });
  }

  ngAfterViewInit(): void {
    const input = this.endDateInput?.nativeElement;
    if (!input) {
      return;
    }

    const inputContainer = input.parentElement ?? undefined;

    this.endDatePicker = flatpickr(input, {
      dateFormat: 'Y-m-d',
      allowInput: false,
      disableMobile: true,
      static: true,
      appendTo: inputContainer,
      positionElement: input,
      defaultDate: this.form.controls.endDateUtc.value || undefined,
      onChange: (_, dateString) => {
        this.form.controls.endDateUtc.setValue(dateString);
      }
    });
  }

  openDatePicker(): void {
    this.endDatePicker?.open();
  }

  get endDateTimeDisplay(): string {
    const date = this.form.controls.endDateUtc.value;
    return date ? `${date} 23:59:59` : '';
  }

  get selectedCategoryOption(): CategoryOption | null {
    const current = this.form.controls.category.value;
    return this.categoryOptions.find((item) => item.value === current) ?? null;
  }

  get selectedCurrencyOption(): CurrencyOption | null {
    const current = this.form.controls.currency.value;
    return this.currencyOptions.find((item) => item.value === current) ?? null;
  }

  toggleCategoryMenu(): void {
    this.categoryMenuOpen = !this.categoryMenuOpen;
  }

  selectCategory(option: CategoryOption): void {
    this.form.controls.category.setValue(option.value);
    this.categoryMenuOpen = false;
  }

  toggleCurrencyMenu(): void {
    this.currencyMenuOpen = !this.currencyMenuOpen;
  }

  selectCurrency(option: CurrencyOption): void {
    this.form.controls.currency.setValue(option.value);
    this.currencyMenuOpen = false;
  }

  flagUrl(countryCode: string): string {
    return `https://flagcdn.com/24x18/${countryCode.toLowerCase()}.png`;
  }


  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.categoryMenuOpen && !this.currencyMenuOpen) {
      return;
    }

    const target = event.target as Node | null;
    if (!target) {
      return;
    }

    const categoryHost = this.categoryDropdown?.nativeElement;
    const currencyHost = this.currencyDropdown?.nativeElement;

    if (this.categoryMenuOpen && (!categoryHost || !categoryHost.contains(target))) {
      this.categoryMenuOpen = false;
    }

    if (this.currencyMenuOpen && (!currencyHost || !currencyHost.contains(target))) {
      this.currencyMenuOpen = false;
    }
  }

  ngOnDestroy(): void {
    this.revokePreviewUrl();
    this.formValueChangesSubscription.unsubscribe();
    this.endDatePicker?.destroy();
  }

  private toUtcIso(endDateUtc: string | null | undefined): string | null {
    if (!endDateUtc) {
      return null;
    }

    const match = /^(\d{4}-\d{2}-\d{2})$/.exec(endDateUtc.trim());
    if (!match) {
      return null;
    }

    const [, datePart] = match;
    const parsed = new Date(`${datePart}T23:59:59Z`);
    if (Number.isNaN(parsed.getTime())) {
      return null;
    }

    return parsed.toISOString();
  }

  private setSelectedImage(file: File): void {
    this.revokePreviewUrl();
    this.selectedFileName = file.name;
    this.selectedImageFile = file;
    this.selectedImagePreviewUrl = URL.createObjectURL(file);
  }

  private clearSelectedImage(): void {
    this.revokePreviewUrl();
    this.selectedFileName = null;
    this.selectedImageFile = null;
  }

  private revokePreviewUrl(): void {
    if (!this.selectedImagePreviewUrl) {
      return;
    }

    URL.revokeObjectURL(this.selectedImagePreviewUrl);
    this.selectedImagePreviewUrl = null;
  }

  private extractCreateErrorMessage(error: unknown): string {
    const fallbackMessage = 'Could not create auction. Please review your input and try again.';

    if (!(error instanceof HttpErrorResponse)) {
      return fallbackMessage;
    }

    if (error.status === 0) {
      return 'Cannot reach the API server. Ensure backend is running and reachable.';
    }

    if (error.status === 401 || error.status === 403) {
      return 'Your session is unauthorized. Sign in and try again.';
    }

    const payload = error.error as unknown;
    if (payload && typeof payload === 'object') {
      const details = (payload as { details?: unknown }).details;
      if (typeof details === 'string' && details.trim().length > 0) {
        return details;
      }

      if (Array.isArray(details)) {
        const first = details.find((item) => typeof item === 'string' && item.trim().length > 0);
        if (typeof first === 'string') {
          return first;
        }
      }

      const title = (payload as { title?: unknown }).title;
      if (typeof title === 'string' && title.trim().length > 0) {
        return title;
      }
    }

    return fallbackMessage;
  }

  private getValidationMessage(): string {
    const titleErrors = this.form.controls.title.errors;
    if (titleErrors) {
      if (titleErrors['required']) return 'Title is required.';
      if (titleErrors['minlength']) return 'Title must be at least 3 characters.';
      if (titleErrors['maxlength']) return 'Title cannot exceed 120 characters.';
    }

    const categoryErrors = this.form.controls.category.errors;
    if (categoryErrors) {
      if (categoryErrors['required']) return 'Category is required.';
      if (categoryErrors['minlength']) return 'Category must be at least 2 characters.';
      if (categoryErrors['maxlength']) return 'Category cannot exceed 50 characters.';
    }

    const priceErrors = this.form.controls.startingPriceAmount.errors;
    if (priceErrors) {
      if (priceErrors['required']) return 'Starting price is required.';
      if (priceErrors['min']) return 'Starting price must be 0 or higher.';
    }

    const currencyErrors = this.form.controls.currency.errors;
    if (currencyErrors) {
      if (currencyErrors['required']) return 'Currency is required.';
      if (currencyErrors['minlength'] || currencyErrors['maxlength']) return 'Currency must be exactly 3 letters (e.g. USD).';
    }

    const endDateErrors = this.form.controls.endDateUtc.errors;
    if (endDateErrors) {
      return 'End date is required.';
    }

    const descriptionErrors = this.form.controls.description.errors;
    if (descriptionErrors?.['maxlength']) {
      return 'Description cannot exceed 2000 characters.';
    }

    return 'Please fix the highlighted fields and try again.';
  }
}

