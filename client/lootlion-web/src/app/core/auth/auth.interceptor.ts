import { HttpBackend, HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthSessionService } from './auth-session.service';
import { environment } from '../../../environments/environment';
import { AuthResponse } from '../../api/generated/model/authResponse';
import { catchError, switchMap, throwError } from 'rxjs';

const SKIP_REFRESH = 'X-Skip-Auth-Refresh';
const REFRESH_PATH = '/api/Auth/refresh';

function isAuthPublicPath(url: string): boolean {
  const u = url.toLowerCase();
  return u.includes('/api/auth/login') || u.includes('/api/auth/register') || u.includes('/api/auth/refresh');
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(AuthSessionService);
  const backend = inject(HttpBackend);

  if (req.headers.has(SKIP_REFRESH)) {
    return next(req);
  }

  let outgoing = req;
  if (!isAuthPublicPath(req.url)) {
    const token = session.getAccessToken();
    if (token) {
      outgoing = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
    }
  }

  return next(outgoing).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status !== 401 || isAuthPublicPath(req.url) || req.headers.has(SKIP_REFRESH)) {
        return throwError(() => err);
      }
      const rt = session.getRefreshToken();
      if (!rt) {
        session.clear();
        return throwError(() => err);
      }
      const url = `${environment.apiBaseUrl}${REFRESH_PATH}`;
      return new HttpClient(backend).post<AuthResponse>(url, { refreshToken: rt }).pipe(
        switchMap((res) => {
          if (!session.storeFromAuthResponse(res)) {
            session.clear();
            return throwError(() => err);
          }
          const access = res.accessToken!;
          const retry = req.clone({
            setHeaders: {
              [SKIP_REFRESH]: '1',
              Authorization: `Bearer ${access}`,
            },
          });
          return next(retry);
        }),
        catchError(() => {
          session.clear();
          return throwError(() => err);
        })
      );
    })
  );
};
