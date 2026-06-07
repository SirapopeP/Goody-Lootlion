import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { TranslocoPipe } from '@jsverse/transloco';
import { finalize } from 'rxjs';
import { parseJwtUserDisplay } from '../../core/auth/jwt-payload';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { ActiveHouseholdService } from '../../core/household/active-household.service';
import { WalletFacadeService } from '../../core/wallet/wallet-facade.service';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [TranslocoPipe, RouterLink, DatePipe],
  templateUrl: './profile-page.component.html',
  styleUrl: './profile-page.component.scss',
})
export class ProfilePageComponent {
  readonly wallet = inject(WalletFacadeService);
  readonly activeHousehold = inject(ActiveHouseholdService);
  readonly session = inject(AuthSessionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly userDisplay = () => parseJwtUserDisplay(this.session.token());

  constructor() {
    this.reload();
    this.activeHousehold.sidebarRefresh$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reload());
  }

  reload(): void {
    const hid = this.activeHousehold.activeHouseholdId();
    if (!this.session.isAuthenticated() || !hid) {
      return;
    }
    this.loading.set(true);
    this.wallet
      .loadBalance(hid)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
    this.wallet
      .loadLedger(hid, 30)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe();
  }
}
