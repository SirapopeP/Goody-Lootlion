import { Component, computed, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { catchError, finalize, of, Subject, switchMap, tap } from 'rxjs';
import { HouseholdsService } from '../../../api/generated/api/households.service';
import { HouseholdMemberDto } from '../../../api/generated/model/householdMemberDto';
import { AuthSessionService } from '../../../core/auth/auth-session.service';
import { MenuAccessService } from '../../../core/auth/menu-access.service';
import { ActiveHouseholdService } from '../../../core/household/active-household.service';
import { MissionApiService } from '../../../core/mission/mission-api.service';
import {
  CreateMissionTemplateRequest,
  MissionAssignmentMode,
  MissionRecurrenceKind,
  MissionTemplateDto,
} from '../../../core/mission/mission.models';
import { ToastService } from '../../../core/toast/toast.service';

@Component({
  selector: 'app-home-mission-panel',
  standalone: true,
  imports: [ReactiveFormsModule, TranslocoPipe],
  templateUrl: './home-mission-panel.component.html',
  styleUrl: './home-mission-panel.component.scss',
})
export class HomeMissionPanelComponent {
  private readonly missionApi = inject(MissionApiService);
  private readonly householdsApi = inject(HouseholdsService);
  readonly activeHousehold = inject(ActiveHouseholdService);
  private readonly menu = inject(MenuAccessService);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  private readonly fb = inject(FormBuilder);
  readonly session = inject(AuthSessionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly membershipPending = input(false);

  readonly templates = signal<MissionTemplateDto[]>([]);
  readonly members = signal<HouseholdMemberDto[]>([]);
  readonly listLoading = signal(false);
  readonly membersLoading = signal(false);
  readonly createOpen = signal(false);
  readonly createSubmitting = signal(false);
  readonly cancelTargetId = signal<string | null>(null);
  readonly cancelSubmitting = signal(false);
  readonly spawnSubmittingId = signal<string | null>(null);

  readonly createForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(2000)]],
    assignmentMode: [MissionAssignmentMode.BoardClaim as MissionAssignmentMode, [Validators.required]],
    assignedToUserId: [''],
    rewardCoin: [0, [Validators.required, Validators.min(0)]],
    rewardExp: [0, [Validators.required, Validators.min(0)]],
    requiresApproval: [true],
    recurrenceKind: [MissionRecurrenceKind.None as MissionRecurrenceKind, [Validators.required]],
    recurrenceIntervalDays: [7],
    recurrenceDayOfWeek: ['Monday'],
    recurrenceDayOfMonth: [1],
  });

  readonly MissionAssignmentMode = MissionAssignmentMode;
  readonly MissionRecurrenceKind = MissionRecurrenceKind;
  readonly canManage = computed(() => this.menu.canManageMissions());
  readonly canSubmitCreate = computed(
    () => !!this.activeHousehold.activeHouseholdId() && !this.membershipPending()
  );
  readonly isDirectAssign = computed(
    () => this.createForm.controls.assignmentMode.value === MissionAssignmentMode.DirectAssign
  );
  readonly recurrenceKind = computed(() => this.createForm.controls.recurrenceKind.value);

  private readonly reload$ = new Subject<string>();
  private readonly membersReload$ = new Subject<string>();

  constructor() {
    this.reload$
      .pipe(
        tap(() => this.listLoading.set(true)),
        switchMap((householdId) =>
          this.missionApi.listTemplates(householdId).pipe(
            catchError(() => of([] as MissionTemplateDto[])),
            finalize(() => this.listLoading.set(false))
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((rows) => this.templates.set(rows ?? []));

    this.membersReload$
      .pipe(
        tap(() => this.membersLoading.set(true)),
        switchMap((householdId) =>
          this.householdsApi.apiHouseholdsHouseholdIdMembersGet(householdId).pipe(
            catchError(() => of([] as HouseholdMemberDto[])),
            finalize(() => this.membersLoading.set(false))
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((rows) => {
        const active = (rows ?? []).filter(
          (m) => (m.membershipStatus ?? '').toLowerCase() === 'active' && m.userId
        );
        this.members.set(active);
      });

    effect(() => {
      const hid = this.activeHousehold.activeHouseholdId();
      const pending = this.membershipPending();
      if (!this.session.isAuthenticated() || !hid || pending) {
        this.templates.set([]);
        this.members.set([]);
        return;
      }
      this.requestReload(hid);
      this.requestMembersReload(hid);
    });

    this.activeHousehold.sidebarRefresh$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const hid = this.activeHousehold.activeHouseholdId();
        if (hid && !this.membershipPending()) {
          this.requestReload(hid);
          this.requestMembersReload(hid);
        }
      });
  }

  assignmentModeLabel(mode: MissionAssignmentMode | undefined): string {
    const key = `home.mission.assignmentMode.${(mode ?? 'unknown').toLowerCase()}`;
    const t = this.transloco.translate(key);
    return t !== key ? t : (mode ?? '—');
  }

  recurrenceLabel(kind: MissionRecurrenceKind | undefined): string {
    const key = `home.mission.recurrence.${(kind ?? 'none').toLowerCase()}`;
    const t = this.transloco.translate(key);
    return t !== key ? t : (kind ?? '—');
  }

  memberLabel(userId: string | undefined): string {
    if (!userId) {
      return '—';
    }
    const m = this.members().find((x) => x.userId === userId);
    return (m?.displayName ?? m?.email ?? userId).trim() || '—';
  }

  toggleCreate(): void {
    if (!this.canManage()) {
      return;
    }
    if (!this.canSubmitCreate()) {
      this.toastHouseholdBlocked();
      return;
    }
    const next = !this.createOpen();
    this.createOpen.set(next);
    if (next) {
      this.ensureMembersLoaded();
    }
  }

  submitCreate(): void {
    if (!this.canManage()) {
      return;
    }
    if (!this.canSubmitCreate()) {
      this.toastHouseholdBlocked();
      return;
    }
    this.syncAssigneeValidators();
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }
    const hid = this.activeHousehold.activeHouseholdId();
    if (!hid) {
      this.toastHouseholdBlocked();
      return;
    }
    const v = this.createForm.getRawValue();
    const body: CreateMissionTemplateRequest = {
      householdId: hid,
      title: v.title.trim(),
      description: v.description.trim() || null,
      rewardCoin: v.rewardCoin,
      rewardExp: v.rewardExp,
      requiresApproval: v.requiresApproval,
      assignmentMode: v.assignmentMode,
      defaultAssigneeUserId:
        v.assignmentMode === MissionAssignmentMode.DirectAssign ? v.assignedToUserId || null : null,
      recurrenceKind: v.recurrenceKind,
      recurrenceIntervalDays:
        v.recurrenceKind === MissionRecurrenceKind.IntervalDays ? v.recurrenceIntervalDays : null,
      recurrenceDayOfWeek:
        v.recurrenceKind === MissionRecurrenceKind.Weekly ? v.recurrenceDayOfWeek : null,
      recurrenceDayOfMonth:
        v.recurrenceKind === MissionRecurrenceKind.Monthly ? v.recurrenceDayOfMonth : null,
    };
    this.createSubmitting.set(true);
    this.missionApi
      .createTemplate(body)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.createSubmitting.set(false))
      )
      .subscribe({
        next: () => {
          this.toast.success(this.transloco.translate('home.mission.createSuccess'));
          this.resetCreateForm();
          this.createOpen.set(false);
          this.requestReload(hid);
        },
        error: (err) => this.toast.fromApiError(err, 'home.mission.createFailed'),
      });
  }

  openCancelConfirm(template: MissionTemplateDto): void {
    if (!this.canManage() || !template.id) {
      return;
    }
    this.cancelTargetId.set(template.id);
  }

  dismissCancelConfirm(): void {
    this.cancelTargetId.set(null);
  }

  confirmCancel(): void {
    const id = this.cancelTargetId();
    const hid = this.activeHousehold.activeHouseholdId();
    if (!id || !hid) {
      return;
    }
    this.cancelSubmitting.set(true);
    this.missionApi
      .cancelTemplate(id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.cancelSubmitting.set(false))
      )
      .subscribe({
        next: () => {
          this.toast.success(this.transloco.translate('home.mission.cancelSuccess'));
          this.cancelTargetId.set(null);
          this.requestReload(hid);
        },
        error: (err) => this.toast.fromApiError(err, 'home.mission.cancelFailed'),
      });
  }

  spawnNextRound(template: MissionTemplateDto): void {
    if (!this.canManage() || !template.id || !template.canSpawnNextRound) {
      return;
    }
    const hid = this.activeHousehold.activeHouseholdId();
    if (!hid) {
      return;
    }
    this.spawnSubmittingId.set(template.id);
    this.missionApi
      .spawnTemplate(template.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.spawnSubmittingId.set(null))
      )
      .subscribe({
        next: () => {
          this.toast.success(this.transloco.translate('home.mission.spawnSuccess'));
          this.requestReload(hid);
        },
        error: (err) => this.toast.fromApiError(err, 'home.mission.spawnFailed'),
      });
  }

  onAssignmentModeChange(): void {
    this.syncAssigneeValidators();
  }

  private syncAssigneeValidators(): void {
    const ctrl = this.createForm.controls.assignedToUserId;
    if (this.createForm.controls.assignmentMode.value === MissionAssignmentMode.DirectAssign) {
      ctrl.setValidators([Validators.required]);
    } else {
      ctrl.clearValidators();
      ctrl.setValue('');
    }
    ctrl.updateValueAndValidity();
  }

  private resetCreateForm(): void {
    this.createForm.reset({
      title: '',
      description: '',
      assignmentMode: MissionAssignmentMode.BoardClaim,
      assignedToUserId: '',
      rewardCoin: 0,
      rewardExp: 0,
      requiresApproval: true,
      recurrenceKind: MissionRecurrenceKind.None,
      recurrenceIntervalDays: 7,
      recurrenceDayOfWeek: 'Monday',
      recurrenceDayOfMonth: 1,
    });
    this.syncAssigneeValidators();
  }

  private requestReload(householdId: string): void {
    this.reload$.next(householdId);
  }

  private requestMembersReload(householdId: string): void {
    this.membersReload$.next(householdId);
  }

  private ensureMembersLoaded(): void {
    const hid = this.activeHousehold.activeHouseholdId();
    if (!hid || this.members().length > 0) {
      return;
    }
    this.requestMembersReload(hid);
  }

  private toastHouseholdBlocked(): void {
    const key = this.membershipPending()
      ? 'home.mission.pendingMembershipHint'
      : 'home.mission.noHouseholdCreateBlocked';
    this.toast.error(this.transloco.translate(key));
  }
}
