import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../core/api.constants';

export interface AdminSystemSettingDto {
  key: string;
  value: string;
  updatedAtUtc: string;
  updatedByUserId: string;
}

@Injectable({ providedIn: 'root' })
export class AdminSystemSettingsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/admin/settings`;

  upsertSetting(key: string, value: string): Observable<AdminSystemSettingDto> {
    return this.http.put<AdminSystemSettingDto>(`${this.baseUrl}/${encodeURIComponent(key)}`, { value });
  }
}
