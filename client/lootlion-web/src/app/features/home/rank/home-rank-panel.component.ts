import { Component, DestroyRef, effect, inject, input } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthSessionService } from '../../../core/auth/auth-session.service';
import { ActiveHouseholdService } from '../../../core/household/active-household.service';
import { WalletFacadeService } from '../../../core/wallet/wallet-facade.service';

@Component({
  selector: 'app-home-rank-panel',
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: './home-rank-panel.component.html',
  styleUrl: './home-rank-panel.component.scss',
})
export class HomeRankPanelComponent {
  readonly membershipPending = input(false);

  readonly wallet = inject(WalletFacadeService);
  readonly activeHousehold = inject(ActiveHouseholdService);
  readonly session = inject(AuthSessionService);
  private readonly destroyRef = inject(DestroyRef);

  constructor() {
    effect(() => {
      const hid = this.activeHousehold.activeHouseholdId();
      const pending = this.membershipPending();
      if (!this.session.isAuthenticated() || !hid || pending) {
        return;
      }
      this.wallet.loadLeaderboard(hid).pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
    });
  }
}
