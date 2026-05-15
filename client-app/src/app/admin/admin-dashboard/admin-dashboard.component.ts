import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';

import { AdminDashboardDto, AdminReportsService } from '../admin-reports.service';

type DashboardKpi = {
  label: string;
  value: string;
  detail: string;
  icon: string;
};

type ActivityItem = {
  title: string;
  subtitle: string;
  at: string;
};

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  private readonly reportsApi = inject(AdminReportsService);

  isLoading = false;
  errorMessage = '';
  kpis: DashboardKpi[] = [];
  activity: ActivityItem[] = [];

  ngOnInit(): void {
    this.loadDashboard();
  }

  private loadDashboard(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.reportsApi.getDashboard().subscribe({
      next: (dashboard) => {
        this.kpis = this.toKpis(dashboard);
        this.activity = dashboard.recentActivity.map((item) => ({
          title: item.title,
          subtitle: item.description,
          at: this.formatRelativeTime(item.occurredAtUtc)
        }));
        this.isLoading = false;
      },
      error: () => {
        this.kpis = [];
        this.activity = [];
        this.errorMessage = 'Could not load dashboard statistics. Please try again.';
        this.isLoading = false;
      }
    });
  }

  private toKpis(dashboard: AdminDashboardDto): DashboardKpi[] {
    return [
      {
        label: 'Active Users',
        value: this.formatInt(dashboard.activeUsers),
        detail: 'All active accounts',
        icon: 'fa-solid fa-users'
      },
      {
        label: 'Live Auctions',
        value: this.formatInt(dashboard.liveAuctions),
        detail: 'Currently accepting bids',
        icon: 'fa-solid fa-gavel'
      },
      {
        label: 'Bids Today',
        value: this.formatInt(dashboard.dailyBids),
        detail: 'Placed since 00:00 UTC',
        icon: 'fa-solid fa-hand-holding-dollar'
      },
      {
        label: 'Flagged Cases',
        value: this.formatInt(dashboard.flaggedCases),
        detail: 'Open moderation queue',
        icon: 'fa-solid fa-flag'
      }
    ];
  }

  private formatInt(value: number): string {
    return new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 }).format(value);
  }

  private formatRelativeTime(value: string): string {
    const occurredAt = new Date(value).getTime();
    const diffMs = Math.max(0, Date.now() - occurredAt);
    const diffMinutes = Math.floor(diffMs / 60000);

    if (diffMinutes < 1) {
      return 'just now';
    }

    if (diffMinutes < 60) {
      return `${diffMinutes} minute${diffMinutes === 1 ? '' : 's'} ago`;
    }

    const diffHours = Math.floor(diffMinutes / 60);
    if (diffHours < 24) {
      return `${diffHours} hour${diffHours === 1 ? '' : 's'} ago`;
    }

    const diffDays = Math.floor(diffHours / 24);
    return `${diffDays} day${diffDays === 1 ? '' : 's'} ago`;
  }
}
