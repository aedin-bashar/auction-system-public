import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../core/api.constants';

export type AdminUserRole = 'Admin' | 'Seller' | 'Bidder';

export interface AdminUserDto {
  userId: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  role: AdminUserRole;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface UpdateAdminUserRequest {
  email: string;
  fullName: string;
  phoneNumber: string | null;
  role: AdminUserRole;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class AdminUserManagementService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/admin/users`;

  listUsers(): Observable<AdminUserDto[]> {
    return this.http.get<AdminUserDto[]>(this.baseUrl);
  }

  updateUser(userId: string, request: UpdateAdminUserRequest): Observable<AdminUserDto> {
    return this.http.put<AdminUserDto>(`${this.baseUrl}/${userId}`, request);
  }

  deleteUser(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${userId}`);
  }
}
