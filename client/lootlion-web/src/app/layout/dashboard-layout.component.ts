import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { parseJwtUserDisplay } from '../core/auth/jwt-payload';
import { AuthSessionService } from '../core/auth/auth-session.service';
import { LanguageToggleComponent } from '../core/i18n/language-toggle.component';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslocoPipe, LanguageToggleComponent],
  templateUrl: './dashboard-layout.component.html',
  styleUrl: './dashboard-layout.component.css',
})
export class DashboardLayoutComponent {
  readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);

  readonly userDisplay = computed(() => parseJwtUserDisplay(this.session.token()));

  readonly inventoryCap = 15;
  readonly inventoryUsed = 0;
  readonly workerCap = 10;
  readonly workerUsed = 0;

  logout(): void {
    this.session.clear();
    void this.router.navigateByUrl('/auth/login');
  }
}
