import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../core/api.constants';

@Injectable({ providedIn: 'root' })
export class UserSecurityService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/users/security`;

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/change-password`, {
      currentPassword,
      newPassword
    });
  }
}
