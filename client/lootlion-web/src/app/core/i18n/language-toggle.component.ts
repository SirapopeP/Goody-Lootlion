import { Component, inject } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

@Component({
  selector: 'app-language-toggle',
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: './language-toggle.component.html',
  styleUrl: './language-toggle.component.css',
})
export class LanguageToggleComponent {
  private readonly transloco = inject(TranslocoService);
  readonly activeLang = this.transloco.activeLang;

  setLang(lang: 'en' | 'th'): void {
    this.transloco.setActiveLang(lang);
  }
}
