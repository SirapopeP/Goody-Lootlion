import { Injectable, inject, signal } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { readApiErrorMessage } from '../auth/api-error';
import type { ToastItem, ToastKind } from './toast.model';

const DEFAULT_DURATION: Record<ToastKind, number> = {
  success: 4200,
  error: 7200,
  info: 5000,
  warning: 6000,
};

const MAX_VISIBLE = 6;

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly transloco = inject(TranslocoService);

  private readonly _items = signal<ToastItem[]>([]);
  /** รายการ toast ปัจจุบัน — ให้ container อ่าน */
  readonly items = this._items.asReadonly();

  private readonly timers = new Map<string, ReturnType<typeof setTimeout>>();

  success(message: string, durationMs = DEFAULT_DURATION.success): void {
    this.show({ kind: 'success', message, durationMs });
  }

  error(message: string, durationMs = DEFAULT_DURATION.error): void {
    this.show({ kind: 'error', message, durationMs });
  }

  info(message: string, durationMs = DEFAULT_DURATION.info): void {
    this.show({ kind: 'info', message, durationMs });
  }

  warning(message: string, durationMs = DEFAULT_DURATION.warning): void {
    this.show({ kind: 'warning', message, durationMs });
  }

  /**
   * แสดงข้อความจาก HTTP error หรือ fallback จากคีย์ i18n (ข้อความระดับผู้ใช้)
   */
  fromApiError(err: unknown, fallbackMessageKey: string): void {
    const fallback = this.transloco.translate(fallbackMessageKey);
    this.error(readApiErrorMessage(err, fallback));
  }

  show(payload: { kind: ToastKind; message: string; durationMs?: number }): void {
    const durationMs = payload.durationMs ?? DEFAULT_DURATION[payload.kind];
    const id = crypto.randomUUID();
    const item: ToastItem = {
      id,
      kind: payload.kind,
      message: payload.message.trim() || this.transloco.translate('toast.fallbackMessage'),
      durationMs,
    };

    this._items.update((list) => {
      const next = [...list, item];
      if (next.length <= MAX_VISIBLE) {
        return next;
      }
      const trimmed = next.slice(-MAX_VISIBLE);
      for (const dropped of next.slice(0, next.length - MAX_VISIBLE)) {
        const tm = this.timers.get(dropped.id);
        if (tm !== undefined) {
          clearTimeout(tm);
        }
        this.timers.delete(dropped.id);
      }
      return trimmed;
    });

    const timer = setTimeout(() => this.dismiss(id), durationMs);
    this.timers.set(id, timer);
  }

  dismiss(id: string): void {
    const t = this.timers.get(id);
    if (t !== undefined) {
      clearTimeout(t);
      this.timers.delete(id);
    }
    this._items.update((list) => list.filter((x) => x.id !== id));
  }

  clear(): void {
    for (const t of this.timers.values()) {
      clearTimeout(t);
    }
    this.timers.clear();
    this._items.set([]);
  }
}
