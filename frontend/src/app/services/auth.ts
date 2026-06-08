import { inject, Injectable, signal } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response';

export interface RegisterRequest {
  name: string | null;
  email: string | null;
  password: string | null;
  roleId: number | null;
}

export interface LoginRequest {
  email: string | null;
  password: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

@Injectable({
  providedIn: 'root',
})
export class Auth {
  private readonly http = inject(HttpClient);

  private static readonly ROLE_CLAIM_URI =
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

  /** Role id stored in the JWT (matches backend `ClaimTypes.Role`). */
  readonly recruiterRoleId = 2;

  readonly candidateRoleId = 3;

  /** `true` when there is a saved token; templates use `auth.loggedIn()`. */
  readonly loggedIn = signal(!!localStorage.getItem('token'));

  /** Parsed from JWT; used to show candidate-only nav (e.g. My applications). */
  readonly sessionRoleId = signal<number | null>(Auth.parseRoleFromToken(localStorage.getItem('token')));

  register(payload: RegisterRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${environment.apiUrl}/api/Auth/register`, payload);
  }

  login(payload: LoginRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${environment.apiUrl}/api/Auth/login`, payload);
  }

  changePassword(payload: ChangePasswordRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${environment.apiUrl}/api/Auth/change-password`,
      payload
    );
  }
  saveToken(token: string): void {
    localStorage.setItem('token', token);
    this.loggedIn.set(true);
    this.sessionRoleId.set(Auth.parseRoleFromToken(token));
  }
  getToken(): string | null {
    return localStorage.getItem('token');
  }
  logout(): void {
    localStorage.removeItem('token');
    this.loggedIn.set(false);
    this.sessionRoleId.set(null);
  }

  onLoginSuccess(token: string): void {
    this.saveToken(token);
  }
  //decoding jWT payload to get the role id
  private static parseRoleFromToken(token: string | null): number | null {
    const payload = Auth.decodeJwtPayload(token);
    if (!payload) return null;
    const raw = payload[Auth.ROLE_CLAIM_URI] ?? payload['role'];
    if (raw === undefined || raw === null) return null;
    const n = typeof raw === 'string' ? Number(raw) : Number(raw);
    return Number.isFinite(n) ? n : null;
  }

  private static decodeJwtPayload(token: string | null): Record<string, unknown> | null {
    if (!token) return null;
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      let base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const pad = base64.length % 4;
      if (pad) base64 += '='.repeat(4 - pad);
      return JSON.parse(atob(base64)) as Record<string, unknown>;
    } catch {
      return null;
    }
  }
}
