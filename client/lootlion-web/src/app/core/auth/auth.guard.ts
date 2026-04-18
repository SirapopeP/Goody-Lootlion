import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse } from '../../api/generated/model/authResponse';
import { AuthSessionService } from './auth-session.service';
import { isJwtAccessExpired } from './jwt-payload';

const REFRESH_PATH = '/api/Auth/refresh';

/**
 * ป้องกัน shell หลัก (Loot / dashboard): ต้องมี session ที่ใช้งานได้
 *
 * - ไม่มี access และไม่มี refresh → `/auth/login?returnUrl=...`
 * - access ยังไม่หมดอายุ (JWT `exp`) → ผ่าน
 * - access หมดอายุหรือไม่มี แต่มี refresh → `POST /api/Auth/refresh` แบบเงียบ แล้วค่อยผ่าน
 * - refresh ล้มเหลว → เคลียร์ session แล้วไป login
 *
 * หมายเหตุ: การยืนยันตัวตนจริงยังอยู่ที่ API; ฝั่งนี้เป็นเกณฑ์มาตรฐานของ SPA
 */
export const authCanMatch: CanMatchFn = () => {
  const session = inject(AuthSessionService);
  const router = inject(Router);
  const http = inject(HttpClient);

  const returnUrl = normalizeReturnUrl(router.routerState.snapshot.url);

  const loginTree = () =>
    router.createUrlTree(['/auth/login'], {
      queryParams: { returnUrl },
    });

  const access = session.getAccessToken();
  const refresh = session.getRefreshToken();

  if (!access && !refresh) {
    session.clear();
    return loginTree();
  }

  if (access && !isJwtAccessExpired(access)) {
    return true;
  }

  if (!refresh) {
    session.clear();
    return loginTree();
  }

  const url = `${environment.apiBaseUrl}${REFRESH_PATH}`;
  return http.post<AuthResponse>(url, { refreshToken: refresh }).pipe(
    map((res) => {
      if (!session.storeFromAuthResponse(res)) {
        session.clear();
        return loginTree();
      }
      return true;
    }),
    catchError(() => {
      session.clear();
      return of(loginTree());
    })
  );
};

function normalizeReturnUrl(raw: string): string {
  const t = (raw ?? '').trim();
  if (t === '' || t === '/') {
    return '/';
  }
  return t.startsWith('/') ? t : `/${t}`;
}
