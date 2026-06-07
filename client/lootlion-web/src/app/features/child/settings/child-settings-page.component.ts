import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { parseJwtUserDisplay } from '../../../core/auth/jwt-payload';
import { AuthSessionService } from '../../../core/auth/auth-session.service';
import { ActiveHouseholdService } from '../../../core/household/active-household.service';
import { LanguageToggleComponent } from '../../../core/i18n/language-toggle.component';

@Component({
  selector: 'app-child-settings-page',
  standalone: true,
  imports: [TranslocoPipe, RouterLink, LanguageToggleComponent],
  templateUrl: './child-settings-page.component.html',
  styleUrl: './child-settings-page.component.scss',
})
export class ChildSettingsPageComponent {
  readonly session = inject(AuthSessionService);
  private readonly activeHousehold = inject(ActiveHouseholdService);
  private readonly router = inject(Router);

  readonly userDisplay = () => parseJwtUserDisplay(this.session.token());

  logout(): void {
    this.session.clear();
    this.activeHousehold.clear();
    void this.router.navigateByUrl('/auth/login');
  }
}
