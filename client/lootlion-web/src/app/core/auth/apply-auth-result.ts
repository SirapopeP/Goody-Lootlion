import { WritableSignal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { AuthResponse } from '../../api/generated/model/authResponse';
import { AuthSessionService } from './auth-session.service';

/** เก็บ session แล้วไปหน้าแรก หรือตั้งข้อความเมื่อไม่มี token */
export function applyAuthResult(
  session: AuthSessionService,
  router: Router,
  transloco: TranslocoService,
  errorMessage: WritableSignal<string | null>,
  res: AuthResponse
): void {
  if (session.storeFromAuthResponse(res)) {
    void router.navigateByUrl('/');
  } else {
    errorMessage.set(transloco.translate('auth.noToken'));
  }
}
