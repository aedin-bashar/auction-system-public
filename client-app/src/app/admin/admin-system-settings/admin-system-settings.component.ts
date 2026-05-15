import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';

import { AdminSystemSettingDto, AdminSystemSettingsService } from '../admin-system-settings.service';

type SettingsSection = {
  title: string;
  description: string;
  enabled: boolean;
};

@Component({
  selector: 'app-admin-system-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-system-settings.component.html',
  styleUrl: './admin-system-settings.component.scss'
})
export class AdminSystemSettingsComponent {
  private readonly settingsApi = inject(AdminSystemSettingsService);

  maintenanceMode = false;
  allowGuestBidding = false;

  minBidIncrement = 5;
  auctionExtensionMinutes = 2;
  settlementWindowHours = 48;

  readonly sections: SettingsSection[] = [
    {
      title: 'Auction Engine',
      description: 'Controls over time extension and minimum bid step.',
      enabled: true
    },
    {
      title: 'Security',
      description: 'Administrative access and operational safeguards.',
      enabled: true
    },
    {
      title: 'Payments',
      description: 'Settlement timing and transaction processing safeguards.',
      enabled: true
    }
  ];

  lastSavedAt: Date | null = null;
  isSaving = false;
  errorMessage = '';

  saveChanges(): void {
    this.isSaving = true;
    this.errorMessage = '';

    const payload: Record<string, string> = {
      'maintenance.mode': String(this.maintenanceMode),
      'auction.allowGuestBidding': String(this.allowGuestBidding),
      'auction.minBidIncrement': String(this.minBidIncrement),
      'auction.extensionMinutes': String(this.auctionExtensionMinutes),
      'payments.settlementWindowHours': String(this.settlementWindowHours)
    };

    const requests = Object.entries(payload).map(([key, value]) => this.settingsApi.upsertSetting(key, value));

    forkJoin(requests).subscribe({
      next: (saved) => {
        const latest = this.findLatestSavedAt(saved);
        this.lastSavedAt = latest ? new Date(latest) : new Date();
        this.isSaving = false;
      },
      error: () => {
        this.errorMessage = 'Could not save settings. Please try again.';
        this.isSaving = false;
      }
    });
  }

  private findLatestSavedAt(saved: AdminSystemSettingDto[]): string | null {
    if (saved.length === 0) {
      return null;
    }

    return saved
      .map((x) => x.updatedAtUtc)
      .sort((a, b) => new Date(b).getTime() - new Date(a).getTime())[0] ?? null;
  }
}
