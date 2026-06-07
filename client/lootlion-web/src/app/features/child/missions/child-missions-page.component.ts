import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe } from '@jsverse/transloco';
import { catchError, finalize, forkJoin, of, switchMap } from 'rxjs';
import { HouseholdsService } from '../../../api/generated/api/households.service';
import { AuthSessionService } from '../../../core/auth/auth-session.service';
import { ActiveHouseholdService } from '../../../core/household/active-household.service';
import { MissionApiService } from '../../../core/mission/mission-api.service';
import { MissionInstanceDto, MissionInstanceStatus } from '../../../core/mission/mission.models';
import { WalletFacadeService } from '../../../core/wallet/wallet-facade.service';
import { HomeMissionCenterComponent } from '../../home/mission-center/home-mission-center.component';

@Component({
  selector: 'app-child-missions-page',
  standalone: true,
  imports: [TranslocoPipe, HomeMissionCenterComponent],
  templateUrl: './child-missions-page.component.html',
  styleUrl: './child-missions-page.component.scss',
})
export class ChildMissionsPageComponent {
  private readonly missionApi = inject(MissionApiService);
  private readonly householdsApi = inject(HouseholdsService);
  readonly wallet = inject(WalletFacadeService);
  readonly activeHousehold = inject(ActiveHouseholdService);
  readonly session = inject(AuthSessionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly membershipPending = signal(false);
  readonly loading = signal(false);
  readonly activeCount = signal(0);
  readonly pendingCount = signal(0);
  readonly doneCount = signal(0);

  constructor() {
    this.reloadMeta();
    this.activeHousehold.sidebarRefresh$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reloadMeta());
  }

  onInstancesChanged(instances: MissionInstanceDto[]): void {
    this.activeCount.set(instances.filter((m) => m.status === MissionInstanceStatus.Active).length);
  }

  private reloadMeta(): void {
    if (!this.session.isAuthenticated()) {
      this.membershipPending.set(false);
      this.activeCount.set(0);
      this.pendingCount.set(0);
      this.doneCount.set(0);
      return;
    }
    this.loading.set(true);
    this.householdsApi
      .apiHouseholdsMineGet()
      .pipe(
        switchMap((households) => {
          const picked = this.activeHousehold.pickHousehold(households ?? []);
          const pending = (picked?.membershipStatus ?? '').toLowerCase() === 'pending';
          this.membershipPending.set(pending);
          const hid = picked?.id;
          if (!hid || pending) {
            return of({ mine: [] as MissionInstanceDto[], done: [] as MissionInstanceDto[], pending: [] as MissionInstanceDto[] });
          }
          this.wallet.requestRefresh(hid);
          return forkJoin({
            mine: this.missionApi.listMine(hid).pipe(catchError(() => of([] as MissionInstanceDto[]))),
            done: this.missionApi
              .listMine(hid, MissionInstanceStatus.Approved)
              .pipe(catchError(() => of([] as MissionInstanceDto[]))),
            pending: this.missionApi.listMine(hid, MissionInstanceStatus.Submitted).pipe(
              catchError(() => of([] as MissionInstanceDto[]))
            ),
          });
        }),
        catchError(() =>
          of({ mine: [] as MissionInstanceDto[], done: [] as MissionInstanceDto[], pending: [] as MissionInstanceDto[] })
        ),
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe(({ mine, done, pending }) => {
        this.activeCount.set((mine ?? []).filter((m) => m.status === MissionInstanceStatus.Active).length);
        this.pendingCount.set((pending ?? []).length);
        this.doneCount.set((done ?? []).length);
      });
  }
}
