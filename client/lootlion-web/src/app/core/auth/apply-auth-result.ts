import { WritableSignal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { AuthResponse } from '../../api/generated/model/authResponse';
import { AuthSessionService } from './auth-session.service';

/** รับเฉพาะ path ภายในแอป (กัน open redirect) */
export function sanitizeInternalReturnUrl(raw: string | null | undefined): string {
  if (raw == null || typeof raw !== 'string') {
    return '/';
  }
  const t = raw.trim();
  if (!t.startsWith('/') || t.startsWith('//')) {
    return '/';
  }
  return t;
}

/** เก็บ session แล้วไป `returnUrl` หรือหน้าแรก หรือตั้งข้อความเมื่อไม่มี token */
export function applyAuthResult(
  session: AuthSessionService,
  router: Router,
  transloco: TranslocoService,
  errorMessage: WritableSignal<string | null>,
  res: AuthResponse,
  returnUrl?: string | null
): void {
  if (session.storeFromAuthResponse(res)) {
    void router.navigateByUrl(sanitizeInternalReturnUrl(returnUrl));
  } else {
    errorMessage.set(transloco.translate('auth.noToken'));
  }
}
