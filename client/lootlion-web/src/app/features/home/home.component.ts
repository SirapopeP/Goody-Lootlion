import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { catchError, finalize, of, switchMap } from 'rxjs';
import { HouseholdsService } from '../../api/generated/api/households.service';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { ActiveHouseholdService } from '../../core/household/active-household.service';
import { MissionInstanceDto, MissionInstanceStatus } from '../../core/mission/mission.models';
import { HomeMissionCenterComponent } from './mission-center/home-mission-center.component';
import { HomeMissionPanelComponent } from './mission-panel/home-mission-panel.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [TranslocoPipe, RouterLink, HomeMissionPanelComponent, HomeMissionCenterComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent {
  private readonly householdsApi = inject(HouseholdsService);
  private readonly activeHousehold = inject(ActiveHouseholdService);
  readonly session = inject(AuthSessionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly overviewName = signal<string | null>(null);
  readonly overviewMemberCount = signal<number | null>(null);
  readonly overviewMembershipPending = signal(false);
  readonly overviewLoading = signal(false);

  private readonly householdInstances = signal<MissionInstanceDto[]>([]);
  readonly ongoingCurrent = computed(
    () => this.householdInstances().filter((m) => m.status === MissionInstanceStatus.Active).length
  );
  readonly ongoingMax = computed(() => Math.max(this.householdInstances().length, 1));

  constructor() {
    this.loadOverview();
  }

  onInstancesChanged(instances: MissionInstanceDto[]): void {
    this.householdInstances.set(instances);
  }

  private loadOverview(): void {
    if (!this.session.isAuthenticated()) {
      return;
    }
    this.overviewLoading.set(true);
    this.householdsApi
      .apiHouseholdsMineGet()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap((list) => {
          const picked = this.activeHousehold.pickHousehold(list ?? []);
          if (!picked?.id) {
            this.overviewName.set(null);
            this.overviewMembershipPending.set(false);
            return of([] as const);
          }
          this.overviewName.set(picked.name ?? null);
          const pending = (picked.membershipStatus ?? '').toLowerCase() === 'pending';
          this.overviewMembershipPending.set(pending);
          if (pending) {
            return of([] as const);
          }
          return this.householdsApi.apiHouseholdsHouseholdIdMembersGet(picked.id);
        }),
        catchError(() => {
          this.overviewName.set(null);
          this.overviewMembershipPending.set(false);
          return of([]);
        }),
        finalize(() => this.overviewLoading.set(false))
      )
      .subscribe((members) => {
        if (this.overviewMembershipPending()) {
          this.overviewMemberCount.set(null);
          return;
        }
        this.overviewMemberCount.set(Array.isArray(members) ? members.length : 0);
      });
  }
}
