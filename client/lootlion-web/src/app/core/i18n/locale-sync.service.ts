import { inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoService } from '@jsverse/transloco';

const STORAGE_KEY = 'lootlion.lang';

@Injectable({ providedIn: 'root' })
export class LocaleSyncService {
  private readonly transloco = inject(TranslocoService);

  constructor() {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'en' || stored === 'th') {
      this.transloco.setActiveLang(stored);
    }
    this.applyDocumentLang(this.transloco.getActiveLang());

    this.transloco.langChanges$
      .pipe(takeUntilDestroyed())
      .subscribe((lang) => {
        localStorage.setItem(STORAGE_KEY, lang);
        this.applyDocumentLang(lang);
      });
  }

  private applyDocumentLang(lang: string): void {
    if (typeof document !== 'undefined') {
      document.documentElement.lang = lang;
    }
  }
}
