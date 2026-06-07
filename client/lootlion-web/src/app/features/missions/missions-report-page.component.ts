import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { ActiveHouseholdService } from '../../core/household/active-household.service';
import { MissionApiService } from '../../core/mission/mission-api.service';
import { WalletFacadeService } from '../../core/wallet/wallet-facade.service';

@Component({
  selector: 'app-missions-report-page',
  standalone: true,
  imports: [TranslocoPipe, RouterLink],
  templateUrl: './missions-report-page.component.html',
  styleUrl: './missions-report-page.component.scss',
})
export class MissionsReportPageComponent {
  private readonly missionApi = inject(MissionApiService);
  readonly wallet = inject(WalletFacadeService);
  readonly activeHousehold = inject(ActiveHouseholdService);
  readonly session = inject(AuthSessionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly boardCount = signal(0);
  readonly pendingCount = signal(0);
  readonly templateCount = signal(0);

  constructor() {
    this.reload();
    this.activeHousehold.sidebarRefresh$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reload());
  }

  reload(): void {
    const hid = this.activeHousehold.activeHouseholdId();
    if (!this.session.isAuthenticated() || !hid) {
      this.boardCount.set(0);
      this.pendingCount.set(0);
      this.templateCount.set(0);
      return;
    }
    this.loading.set(true);
    this.wallet.loadLeaderboard(hid).pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
    forkJoin({
      board: this.missionApi.listBoard(hid).pipe(catchError(() => of([]))),
      pending: this.missionApi.listPending(hid).pipe(catchError(() => of([]))),
      templates: this.missionApi.listTemplates(hid).pipe(catchError(() => of([]))),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe(({ board, pending, templates }) => {
        this.boardCount.set(board?.length ?? 0);
        this.pendingCount.set(pending?.length ?? 0);
        this.templateCount.set(templates?.length ?? 0);
      });
  }
}
