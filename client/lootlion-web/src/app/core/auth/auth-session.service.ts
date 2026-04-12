import { Injectable, signal } from '@angular/core';

const ACCESS_KEY = 'lootlion.accessToken';
const REFRESH_KEY = 'lootlion.refreshToken';

/**
 * เก็บ access JWT + refresh token ใน sessionStorage (ปิดแท็บแล้วล้าง)
 */
@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly _token = signal<string | null>(this.readAccessFromStorage());

  readonly token = this._token.asReadonly();

  getAccessToken(): string | null {
    return this._token();
  }

  getRefreshToken(): string | null {
    return sessionStorage.getItem(REFRESH_KEY);
  }

  isAuthenticated(): boolean {
    return !!this.getAccessToken();
  }

  setSession(accessToken: string, refreshToken: string): void {
    sessionStorage.setItem(ACCESS_KEY, accessToken);
    sessionStorage.setItem(REFRESH_KEY, refreshToken);
    this._token.set(accessToken);
  }

  clear(): void {
    sessionStorage.removeItem(ACCESS_KEY);
    sessionStorage.removeItem(REFRESH_KEY);
    this._token.set(null);
  }

  private readAccessFromStorage(): string | null {
    return sessionStorage.getItem(ACCESS_KEY);
  }
}
