import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../core/api.constants';

export type FlaggedCaseStatus = 'Open' | 'Resolved';

export interface AdminFlaggedCaseDto {
  caseId: string;
  auctionId: string;
  auctionTitle: string;
  reportedByUserId: string;
  reporterName: string;
  reason: string;
  details: string | null;
  status: FlaggedCaseStatus;
  createdAtUtc: string;
  updatedAtUtc: string;
  resolvedAtUtc: string | null;
  resolvedBy: string | null;
  resolutionNote: string | null;
}

@Injectable({ providedIn: 'root' })
export class AdminModerationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/admin/moderation/cases`;

  listCases(includeResolved: boolean): Observable<AdminFlaggedCaseDto[]> {
    const params = new HttpParams().set('includeResolved', includeResolved);
    return this.http.get<AdminFlaggedCaseDto[]>(this.baseUrl, { params });
  }

  resolveCase(caseId: string, resolutionNote: string): Observable<AdminFlaggedCaseDto> {
    return this.http.post<AdminFlaggedCaseDto>(`${this.baseUrl}/${caseId}/resolve`, { resolutionNote });
  }
}