import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../core/api.constants';

export interface AdminReportDto {
  reportType: string;
  rangeStartUtc: string;
  rangeEndUtc: string;
  generatedAtUtc: string;
  metrics: Record<string, number>;
  totals: Record<string, number>;
}

export interface GenerateAdminReportRequest {
  reportType: string;
  rangeStartUtc: string;
  rangeEndUtc: string;
}

export interface AdminDashboardActivityDto {
  kind: string;
  title: string;
  description: string;
  occurredAtUtc: string;
}

export interface AdminDashboardDto {
  generatedAtUtc: string;
  activeUsers: number;
  liveAuctions: number;
  dailyBids: number;
  flaggedCases: number;
  recentActivity: AdminDashboardActivityDto[];
}

@Injectable({ providedIn: 'root' })
export class AdminReportsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/admin/reports`;

  getDashboard(): Observable<AdminDashboardDto> {
    return this.http.get<AdminDashboardDto>(`${this.baseUrl}/dashboard`);
  }

  generateReport(request: GenerateAdminReportRequest): Observable<AdminReportDto> {
    return this.http.post<AdminReportDto>(`${this.baseUrl}/generate`, request);
  }
}
