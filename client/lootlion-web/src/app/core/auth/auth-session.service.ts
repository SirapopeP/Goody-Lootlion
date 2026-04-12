import { Injectable, signal } from '@angular/core';
import { AuthResponse } from '../../api/generated/model/authResponse';

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

  /** @returns true ถ้ามีคู่ token และบันทึกแล้ว */
  storeFromAuthResponse(res: AuthResponse): boolean {
    const access = res.accessToken;
    const refresh = res.refreshToken;
    if (!access || !refresh) {
      return false;
    }
    this.setSession(access, refresh);
    return true;
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
