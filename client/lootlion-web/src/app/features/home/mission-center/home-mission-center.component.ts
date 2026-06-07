import { Component, computed, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { AuthSessionService } from '../../../core/auth/auth-session.service';
import { MenuAccessService } from '../../../core/auth/menu-access.service';
import { ActiveHouseholdService } from '../../../core/household/active-household.service';
import { MissionApiService } from '../../../core/mission/mission-api.service';
import { MissionInstanceDto, MissionInstanceStatus } from '../../../core/mission/mission.models';
import { ToastService } from '../../../core/toast/toast.service';

type CenterTab = 'board' | 'mine' | 'pending' | 'done';

@Component({
  selector: 'app-home-mission-center',
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: './home-mission-center.component.html',
  styleUrl: './home-mission-center.component.scss',
})
export class HomeMissionCenterComponent {
  private readonly missionApi = inject(MissionApiService);
  readonly activeHousehold = inject(ActiveHouseholdService);
  private readonly menu = inject(MenuAccessService);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  readonly session = inject(AuthSessionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly membershipPending = input(false);
  readonly instancesChanged = output<MissionInstanceDto[]>();

  readonly activeTab = signal<CenterTab>('mine');
  readonly board = signal<MissionInstanceDto[]>([]);
  readonly mine = signal<MissionInstanceDto[]>([]);
  readonly pending = signal<MissionInstanceDto[]>([]);
  readonly done = signal<MissionInstanceDto[]>([]);
  readonly listLoading = signal(false);
  readonly actionInstanceId = signal<string | null>(null);

  readonly isParent = computed(() => this.menu.canManageMissions());
  readonly isChild = computed(() => this.menu.canShowWishlist());
  readonly MissionInstanceStatus = MissionInstanceStatus;

  readonly ongoingCurrent = computed(
    () => this.mine().filter((m) => m.status === MissionInstanceStatus.Active).length
  );
  readonly ongoingMax = computed(() => Math.max(this.mine().length, 1));

  constructor() {
    effect(() => {
      const hid = this.activeHousehold.activeHouseholdId();
      const pending = this.membershipPending();
      if (!this.session.isAuthenticated() || !hid || pending) {
        this.clearLists();
        return;
      }
      this.reloadAll(hid);
    });

    this.activeHousehold.sidebarRefresh$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const hid = this.activeHousehold.activeHouseholdId();
        if (hid && !this.membershipPending()) {
          this.reloadAll(hid);
        }
      });
  }

  setTab(tab: CenterTab): void {
    this.activeTab.set(tab);
  }

  statusLabel(status: MissionInstanceStatus | undefined): string {
    const key = `home.mission.instanceStatus.${(status ?? 'unknown').toLowerCase()}`;
    const t = this.transloco.translate(key);
    return t !== key ? t : (status ?? '—');
  }

  claim(instance: MissionInstanceDto): void {
    if (!instance.id) {
      return;
    }
    this.runAction(instance.id, () => this.missionApi.claim(instance.id!), 'home.mission.claimSuccess', 'home.mission.claimFailed');
  }

  submit(instance: MissionInstanceDto): void {
    if (!instance.id) {
      return;
    }
    this.runAction(instance.id, () => this.missionApi.submit(instance.id!), 'home.mission.submitSuccess', 'home.mission.submitFailed');
  }

  approve(instance: MissionInstanceDto): void {
    if (!instance.id) {
      return;
    }
    this.runAction(instance.id, () => this.missionApi.approve(instance.id!), 'home.mission.approveSuccess', 'home.mission.approveFailed');
  }

  reject(instance: MissionInstanceDto): void {
    if (!instance.id) {
      return;
    }
    this.runAction(instance.id, () => this.missionApi.reject(instance.id!), 'home.mission.rejectSuccess', 'home.mission.rejectFailed');
  }

  private runAction(
    instanceId: string,
    call: () => ReturnType<MissionApiService['claim']>,
    successKey: string,
    failKey: string
  ): void {
    const hid = this.activeHousehold.activeHouseholdId();
    if (!hid) {
      return;
    }
    this.actionInstanceId.set(instanceId);
    call()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.actionInstanceId.set(null))
      )
      .subscribe({
        next: () => {
          this.toast.success(this.transloco.translate(successKey));
          this.reloadAll(hid);
        },
        error: (err) => this.toast.fromApiError(err, failKey),
      });
  }

  private reloadAll(householdId: string): void {
    this.listLoading.set(true);
    const isParent = this.isParent();

    forkJoin({
      board: this.isChild()
        ? this.missionApi.listBoard(householdId).pipe(catchError(() => of([] as MissionInstanceDto[])))
        : of([] as MissionInstanceDto[]),
      mine: this.missionApi.listMine(householdId).pipe(catchError(() => of([] as MissionInstanceDto[]))),
      pending: isParent
        ? this.missionApi.listPending(householdId).pipe(catchError(() => of([] as MissionInstanceDto[])))
        : of([] as MissionInstanceDto[]),
      done: this.missionApi
        .listMine(householdId, MissionInstanceStatus.Approved)
        .pipe(catchError(() => of([] as MissionInstanceDto[]))),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.listLoading.set(false))
      )
      .subscribe(({ board, mine, pending, done }) => {
        this.board.set(board ?? []);
        this.mine.set(mine ?? []);
        this.pending.set(pending ?? []);
        this.done.set(done ?? []);
        this.instancesChanged.emit(mine ?? []);

        if (this.isChild() && board.length > 0 && this.activeTab() === 'mine' && mine.length === 0) {
          this.activeTab.set('board');
        }
        if (!this.isParent() && this.activeTab() === 'pending') {
          this.activeTab.set('mine');
        }
      });
  }

  private clearLists(): void {
    this.board.set([]);
    this.mine.set([]);
    this.pending.set([]);
    this.done.set([]);
    this.instancesChanged.emit([]);
  }
}
