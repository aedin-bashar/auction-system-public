import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AdminReportDto, AdminReportsService } from '../admin-reports.service';

type ReportRange = 'Today' | '7 Days' | '30 Days' | '90 Days';

type ReportKpi = {
  label: string;
  value: string;
  trend: string;
  trendUp: boolean;
};

type TopAuction = {
  title: string;
  bids: number;
  highestBid: string;
  status: 'Active' | 'Ended';
};

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-reports.component.html',
  styleUrl: './admin-reports.component.scss'
})
export class AdminReportsComponent implements OnInit {
  private readonly reportsApi = inject(AdminReportsService);

  readonly ranges: ReportRange[] = ['Today', '7 Days', '30 Days', '90 Days'];
  selectedRange: ReportRange = '30 Days';
  isLoading = false;
  errorMessage = '';
  lastGeneratedAt: string | null = null;

  kpis: ReportKpi[] = [];
  topAuctions: TopAuction[] = [];
  activityFeed: string[] = [];

  ngOnInit(): void {
    this.loadReport();
  }

  onRangeChanged(): void {
    this.loadReport();
  }

  private loadReport(): void {
    const { rangeStartUtc, rangeEndUtc } = this.resolveRange(this.selectedRange);
    this.isLoading = true;
    this.errorMessage = '';

    this.reportsApi.generateReport({
      reportType: 'overview',
      rangeStartUtc,
      rangeEndUtc
    }).subscribe({
      next: (report) => {
        this.lastGeneratedAt = report.generatedAtUtc;
        this.kpis = this.toKpis(report);
        this.topAuctions = [];
        this.activityFeed = this.toActivityFeed(report);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Could not load reports. Please try again.';
      }
    });
  }

  private toKpis(report: AdminReportDto): ReportKpi[] {
    const bidVolume = this.metric(report, 'bidVolume');
    const averageBid = this.metric(report, 'averageBid');
    const refundValue = this.metric(report, 'refundValue');
    const totalBids = this.total(report, 'bids');
    const totalAuctions = this.total(report, 'auctions');
    const refunds = this.total(report, 'refunds');
    const refundRatio = totalBids > 0 ? (refunds / totalBids) * 100 : 0;

    return [
      { label: 'Bid Volume', value: this.formatCurrency(bidVolume), trend: 'Range aggregate', trendUp: true },
      { label: 'Total Bids', value: this.formatInt(totalBids), trend: 'Reported count', trendUp: true },
      { label: 'Auctions', value: this.formatInt(totalAuctions), trend: 'Reported count', trendUp: true },
      { label: 'Refund Ratio', value: `${refundRatio.toFixed(2)}%`, trend: this.formatCurrency(refundValue), trendUp: true },
      { label: 'Average Bid', value: this.formatCurrency(averageBid), trend: 'Per bid', trendUp: true }
    ];
  }

  private toActivityFeed(report: AdminReportDto): string[] {
    const totalBids = this.total(report, 'bids');
    const totalAuctions = this.total(report, 'auctions');
    const activeUsers = this.total(report, 'activeUsers');
    const refunds = this.total(report, 'refunds');

    return [
      `Report type: ${report.reportType}`,
      `Active users in range: ${this.formatInt(activeUsers)}`,
      `Auctions created in range: ${this.formatInt(totalAuctions)}`,
      `Bids placed in range: ${this.formatInt(totalBids)}`,
      `Refunds processed in range: ${this.formatInt(refunds)}`
    ];
  }

  private metric(report: AdminReportDto, key: string): number {
    const value = report.metrics[key];
    return typeof value === 'number' ? value : 0;
  }

  private total(report: AdminReportDto, key: string): number {
    const value = report.totals[key];
    return typeof value === 'number' ? value : 0;
  }

  private formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 2 }).format(amount);
  }

  private formatInt(value: number): string {
    return new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 }).format(value);
  }

  private resolveRange(range: ReportRange): { rangeStartUtc: string; rangeEndUtc: string } {
    const now = new Date();
    const rangeEndUtc = now.toISOString();

    const start = new Date(now);
    switch (range) {
      case 'Today':
        start.setUTCHours(0, 0, 0, 0);
        break;
      case '7 Days':
        start.setUTCDate(start.getUTCDate() - 7);
        break;
      case '30 Days':
        start.setUTCDate(start.getUTCDate() - 30);
        break;
      case '90 Days':
        start.setUTCDate(start.getUTCDate() - 90);
        break;
    }

    return {
      rangeStartUtc: start.toISOString(),
      rangeEndUtc
    };
  }
}
