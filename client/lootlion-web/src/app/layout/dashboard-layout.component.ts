import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { catchError, finalize, of, switchMap } from 'rxjs';
import { HouseholdsService } from '../api/generated/api/households.service';
import { HouseholdMemberDto } from '../api/generated/model/householdMemberDto';
import { parseJwtUserDisplay } from '../core/auth/jwt-payload';
import { AuthSessionService } from '../core/auth/auth-session.service';
import { LanguageToggleComponent } from '../core/i18n/language-toggle.component';
import { ParticlesBackgroundComponent } from '../shared/ui/particles-background/particles-background.component';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    TranslocoPipe,
    LanguageToggleComponent,
    ParticlesBackgroundComponent,
  ],
  templateUrl: './dashboard-layout.component.html',
  styleUrl: './dashboard-layout.component.css',
})
export class DashboardLayoutComponent {
  readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly householdsApi = inject(HouseholdsService);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  readonly userDisplay = computed(() => parseJwtUserDisplay(this.session.token()));

  readonly inventoryCap = 15;
  readonly inventoryUsed = 0;
  readonly workerCap = 10;
  readonly workerUsed = 0;

  readonly familyHouseholdName = signal<string | null>(null);
  readonly familyMembers = signal<HouseholdMemberDto[]>([]);
  readonly familyLoading = signal(false);

  constructor() {
    this.loadFamily();
  }

  private loadFamily(): void {
    if (!this.session.isAuthenticated()) {
      return;
    }

    this.familyLoading.set(true);
    this.householdsApi
      .apiHouseholdsMineGet()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap((households) => {
          if (!households?.length) {
            this.familyHouseholdName.set(null);
            return of([] as HouseholdMemberDto[]);
          }
          const h = households[0];
          this.familyHouseholdName.set(h.name ?? null);
          if (!h.id) {
            return of([] as HouseholdMemberDto[]);
          }
          return this.householdsApi.apiHouseholdsHouseholdIdMembersGet(h.id);
        }),
        catchError(() => {
          this.familyHouseholdName.set(null);
          return of([] as HouseholdMemberDto[]);
        }),
        finalize(() => this.familyLoading.set(false))
      )
      .subscribe((members) => {
        this.familyMembers.set(members ?? []);
      });
  }

  memberRoleLabel(role: string | null | undefined): string {
    const r = (role ?? '').trim().toLowerCase();
    if (r === 'parent') {
      return this.transloco.translate('layout.memberRoleParent');
    }
    if (r === 'child') {
      return this.transloco.translate('layout.memberRoleChild');
    }
    return role?.trim() || '—';
  }

  logout(): void {
    this.session.clear();
    void this.router.navigateByUrl('/auth/login');
  }
}
