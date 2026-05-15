import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../core/api.constants';

export interface UserProfileDto {
  userId: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  role: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface UpdateProfileRequest {
  email: string;
  fullName: string;
  phoneNumber: string | null;
}

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/users/profile`;

  getProfile(): Observable<UserProfileDto> {
    return this.http.get<UserProfileDto>(this.baseUrl);
  }

  updateProfile(request: UpdateProfileRequest): Observable<UserProfileDto> {
    return this.http.put<UserProfileDto>(this.baseUrl, request);
  }
}
