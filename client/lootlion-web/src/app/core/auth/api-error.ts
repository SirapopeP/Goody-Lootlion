import { HttpErrorResponse } from '@angular/common/http';

/** อ่านข้อความจาก API (`{ error: "..." }`) หรือ fallback */
export function readApiErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse) {
    const body = err.error as { error?: string } | undefined;
    if (typeof body?.error === 'string' && body.error.length > 0) {
      return body.error;
    }
    if (err.message) {
      return err.message;
    }
  }
  return fallback;
}
