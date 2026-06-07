import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { catchError, finalize, of, switchMap } from 'rxjs';
import { HouseholdsService } from '../../../api/generated/api/households.service';
import { HouseholdMemberDto } from '../../../api/generated/model/householdMemberDto';
import { AuthSessionService } from '../../../core/auth/auth-session.service';
import { ActiveHouseholdService } from '../../../core/household/active-household.service';
import { WalletFacadeService } from '../../../core/wallet/wallet-facade.service';

@Component({
  selector: 'app-child-family-page',
  standalone: true,
  imports: [TranslocoPipe, RouterLink],
  templateUrl: './child-family-page.component.html',
  styleUrl: './child-family-page.component.scss',
})
export class ChildFamilyPageComponent {
  private readonly householdsApi = inject(HouseholdsService);
  readonly wallet = inject(WalletFacadeService);
  readonly activeHousehold = inject(ActiveHouseholdService);
  readonly session = inject(AuthSessionService);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  readonly householdName = signal<string | null>(null);
  readonly members = signal<HouseholdMemberDto[]>([]);
  readonly loading = signal(false);
  readonly membershipPending = signal(false);

  constructor() {
    this.reload();
    this.activeHousehold.sidebarRefresh$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reload());
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

  memberLevelLabel(userId: string | undefined): string {
    const lv = this.wallet.levelByUserId(userId);
    if (lv === null) {
      return this.transloco.translate('layout.levelPlaceholder');
    }
    return this.transloco.translate('wallet.levelShort', { n: lv });
  }

  private reload(): void {
    if (!this.session.isAuthenticated()) {
      return;
    }
    this.loading.set(true);
    this.householdsApi
      .apiHouseholdsMineGet()
      .pipe(
        switchMap((households) => {
          const picked = this.activeHousehold.pickHousehold(households ?? []);
          this.householdName.set(picked?.name ?? null);
          const pending = (picked?.membershipStatus ?? '').toLowerCase() === 'pending';
          this.membershipPending.set(pending);
          if (!picked?.id || pending) {
            return of([] as HouseholdMemberDto[]);
          }
          this.wallet.requestRefresh(picked.id);
          return this.householdsApi.apiHouseholdsHouseholdIdMembersGet(picked.id);
        }),
        catchError(() => of([] as HouseholdMemberDto[])),
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((rows) => this.members.set(rows ?? []));
  }
}
