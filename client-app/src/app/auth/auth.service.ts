import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { API_BASE_URL } from '../core/api.constants';
import { AuthSession, LoginRequest, LoginResult, RegisterRequest } from './auth.models';

const AUTH_STORAGE_KEY = 'auction.auth.session';
const GUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/auth`;

  login(request: LoginRequest): Observable<LoginResult> {
    return this.http.post<LoginResult>(`${this.baseUrl}/login`, request).pipe(
      tap((result) => this.persistSession(result))
    );
  }

  register(request: RegisterRequest): Observable<LoginResult> {
    return this.http.post<LoginResult>(`${this.baseUrl}/register`, request).pipe(
      tap((result) => this.persistSession(result))
    );
  }

  logout(): void {
    if (!this.isStorageAvailable()) {
      return;
    }

    localStorage.removeItem(AUTH_STORAGE_KEY);
  }

  getSession(): AuthSession | null {
    if (!this.isStorageAvailable()) {
      return null;
    }

    const raw = localStorage.getItem(AUTH_STORAGE_KEY);
    if (!raw) {
      return null;
    }

    try {
      const parsed = JSON.parse(raw) as Partial<AuthSession>;
      if (!this.isValidSession(parsed)) {
        localStorage.removeItem(AUTH_STORAGE_KEY);
        return null;
      }

      return parsed as AuthSession;
    } catch {
      localStorage.removeItem(AUTH_STORAGE_KEY);
      return null;
    }
  }

  getAccessToken(): string | null {
    return this.getSession()?.accessToken ?? null;
  }

  private persistSession(result: LoginResult): void {
    if (!this.isStorageAvailable()) {
      return;
    }

    const session: AuthSession = {
      userId: result.userId,
      email: result.email,
      fullName: result.fullName,
      role: result.role,
      avatarUrl: result.avatarUrl ?? null,
      accessToken: result.accessToken,
      expiresAtUtc: result.expiresAtUtc
    };

    localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session));
  }

  private isStorageAvailable(): boolean {
    return typeof localStorage !== 'undefined';
  }

  private isValidSession(session: Partial<AuthSession> | null | undefined): boolean {
    if (!session) {
      return false;
    }

    const hasRequiredStrings =
      typeof session.userId === 'string' &&
      typeof session.email === 'string' &&
      typeof session.fullName === 'string' &&
      typeof session.role === 'string' &&
      typeof session.accessToken === 'string' &&
      typeof session.expiresAtUtc === 'string';

    if (!hasRequiredStrings) {
      return false;
    }

    const userId = session.userId;
    const expiresAtUtc = session.expiresAtUtc;

    if (typeof userId !== 'string' || typeof expiresAtUtc !== 'string') {
      return false;
    }

    if (!GUID_REGEX.test(userId)) {
      return false;
    }

    const expiresAtMs = Date.parse(expiresAtUtc);
    if (Number.isNaN(expiresAtMs)) {
      return false;
    }

    return expiresAtMs > Date.now();
  }
}
