import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Subject, debounceTime, distinctUntilChanged, switchMap, finalize, EMPTY } from 'rxjs';
import { HouseholdsService } from '../../api/generated/api/households.service';
import { AddMemberBody } from '../../api/generated/model/addMemberBody';
import { HouseholdMineDto } from '../../api/generated/model/householdMineDto';
import { HouseholdMemberDto } from '../../api/generated/model/householdMemberDto';
import { parseJwtUserDisplay } from '../../core/auth/jwt-payload';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { MemberRole } from '../../api/generated/model/memberRole';
import { UserSearchHitDto } from '../../api/generated/model/userSearchHitDto';
import { MenuAccessService } from '../../core/auth/menu-access.service';
import { ActiveHouseholdService } from '../../core/household/active-household.service';
import { ToastService } from '../../core/toast/toast.service';

@Component({
  selector: 'app-households-page',
  standalone: true,
  imports: [ReactiveFormsModule, TranslocoPipe, RouterLink],
  templateUrl: './households-page.component.html',
  styleUrl: './households-page.component.scss',
})
export class HouseholdsPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly householdsApi = inject(HouseholdsService);
  private readonly activeHousehold = inject(ActiveHouseholdService);
  private readonly menu = inject(MenuAccessService);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  readonly session = inject(AuthSessionService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly searchInput$ = new Subject<string>();

  readonly isParentUi = computed(() => this.menu.canShowRewards());

  readonly currentUserId = computed(() => parseJwtUserDisplay(this.session.token()).userId);

  readonly households = signal<HouseholdMineDto[]>([]);
  readonly members = signal<HouseholdMemberDto[]>([]);
  readonly listLoading = signal(false);
  readonly membersLoading = signal(false);
  readonly createSubmitting = signal(false);
  readonly inviteSubmitting = signal(false);
  readonly deleteSubmitting = signal(false);

  readonly selectedHouseholdId = signal<string | null>(null);

  readonly searchResults = signal<UserSearchHitDto[]>([]);
  readonly searchLoading = signal(false);
  readonly searchDraft = signal('');

  readonly pendingInvite = signal<UserSearchHitDto | null>(null);
  readonly deleteTarget = signal<HouseholdMineDto | null>(null);
  readonly memberActionUserId = signal<string | null>(null);
  readonly deleteConfirmDraft = signal('');

  readonly createForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
  });

  readonly inviteRoleForm = this.fb.nonNullable.group({
    role: [MemberRole.Child as MemberRole, [Validators.required]],
  });

  readonly MemberRole = MemberRole;

  constructor() {
    this.reloadHouseholds();

    this.searchInput$
      .pipe(
        debounceTime(350),
        distinctUntilChanged(),
        switchMap((raw) => {
          const q = raw.trim();
          const hid = this.selectedHouseholdId();
          if (!hid || q.length < 2) {
            this.searchResults.set([]);
            this.searchLoading.set(false);
            return EMPTY;
          }
          this.searchLoading.set(true);
          return this.householdsApi.apiHouseholdsHouseholdIdInviteCandidatesGet(hid, q).pipe(
            finalize(() => this.searchLoading.set(false))
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((rows) => this.searchResults.set(rows ?? []));
  }

  selectedHouseholdName(): string {
    const id = this.selectedHouseholdId();
    const h = this.households().find((x) => x.id === id);
    return (h?.name ?? '').trim() || '—';
  }

  /** บ้านที่เลือกอยู่ระหว่างรอผู้ปกครองที่ Active อนุมัติ */
  selectedMembershipPending(): boolean {
    const id = this.selectedHouseholdId();
    if (!id) {
      return false;
    }
    const h = this.households().find((x) => x.id === id);
    return (h?.membershipStatus ?? '').toLowerCase() === 'pending';
  }

  onSearchInput(value: string): void {
    this.searchDraft.set(value);
    this.searchInput$.next(value);
  }

  private reloadHouseholds(): void {
    this.listLoading.set(true);
    this.householdsApi
      .apiHouseholdsMineGet()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.listLoading.set(false))
      )
      .subscribe({
        next: (list) => {
          const rows = list ?? [];
          this.households.set(rows);
          if (!rows.length) {
            this.selectedHouseholdId.set(null);
            this.members.set([]);
            this.clearSearchUi();
            return;
          }
          const picked = this.activeHousehold.pickHousehold(rows);
          const id = picked?.id ?? null;
          this.selectedHouseholdId.set(id);
          if (id && (picked?.membershipStatus ?? '').toLowerCase() !== 'pending') {
            this.loadMembers(id);
          } else {
            this.members.set([]);
          }
          this.clearSearchUi();
        },
        error: (err) => {
          this.toast.fromApiError(err, 'household.loadFailed');
          this.households.set([]);
        },
      });
  }

  private clearSearchUi(): void {
    this.searchDraft.set('');
    this.searchResults.set([]);
    this.pendingInvite.set(null);
  }

  selectHousehold(id: string): void {
    this.selectedHouseholdId.set(id);
    this.activeHousehold.setActiveHouseholdFromUser(id);
    const row = this.households().find((h) => h.id === id);
    if ((row?.membershipStatus ?? '').toLowerCase() === 'pending') {
      this.members.set([]);
    } else {
      this.loadMembers(id);
    }
    this.clearSearchUi();
  }

  private loadMembers(householdId: string): void {
    const row = this.households().find((h) => h.id === householdId);
    if ((row?.membershipStatus ?? '').toLowerCase() === 'pending') {
      this.members.set([]);
      return;
    }
    this.membersLoading.set(true);
    this.householdsApi
      .apiHouseholdsHouseholdIdMembersGet(householdId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.membersLoading.set(false))
      )
      .subscribe({
        next: (m) => this.members.set(m ?? []),
        error: (err) => {
          this.toast.fromApiError(err, 'household.membersLoadFailed');
          this.members.set([]);
        },
      });
  }

  submitCreate(): void {
    if (!this.isParentUi() || this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }
    const name = this.createForm.controls.name.value.trim();
    if (!name) {
      return;
    }
    this.createSubmitting.set(true);
    this.householdsApi
      .apiHouseholdsPost({ name })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.createSubmitting.set(false))
      )
      .subscribe({
        next: (created) => {
          this.toast.success(this.transloco.translate('household.createSuccess'));
          this.createForm.reset({ name: '' });
          if (created?.id) {
            this.activeHousehold.setActiveHouseholdFromUser(created.id);
          }
          this.reloadHouseholds();
        },
        error: (err) => this.toast.fromApiError(err, 'household.createFailed'),
      });
  }

  openInviteConfirm(hit: UserSearchHitDto): void {
    this.pendingInvite.set(hit);
  }

  cancelInviteConfirm(): void {
    this.pendingInvite.set(null);
  }

  confirmInvite(): void {
    const hit = this.pendingInvite();
    const hid = this.selectedHouseholdId();
    if (!hit?.userId || !hid) {
      this.pendingInvite.set(null);
      return;
    }
    const body: AddMemberBody = {
      memberUserId: hit.userId,
      role: this.inviteRoleForm.controls.role.value,
    };
    this.inviteSubmitting.set(true);
    this.householdsApi
      .apiHouseholdsHouseholdIdMembersPost(hid, body)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.inviteSubmitting.set(false))
      )
      .subscribe({
        next: () => {
          this.toast.success(this.transloco.translate('household.inviteSuccess'));
          this.pendingInvite.set(null);
          this.searchDraft.set('');
          this.searchResults.set([]);
          this.loadMembers(hid);
          this.activeHousehold.notifySidebarRefresh();
        },
        error: (err) => this.toast.fromApiError(err, 'household.inviteFailed'),
      });
  }

  approveMember(userId: string | undefined): void {
    const hid = this.selectedHouseholdId();
    if (!hid || !userId || !this.isParentUi()) {
      return;
    }
    this.memberActionUserId.set(userId);
    this.householdsApi
      .apiHouseholdsHouseholdIdMembersMemberUserIdApprovePost(hid, userId)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.memberActionUserId.set(null)))
      .subscribe({
        next: () => {
          this.toast.success(this.transloco.translate('household.approveSuccess'));
          this.loadMembers(hid);
          this.reloadHouseholds();
          this.activeHousehold.notifySidebarRefresh();
        },
        error: (err) => this.toast.fromApiError(err, 'household.approveFailed'),
      });
  }

  rejectMember(userId: string | undefined): void {
    const hid = this.selectedHouseholdId();
    if (!hid || !userId || !this.isParentUi()) {
      return;
    }
    this.memberActionUserId.set(userId);
    this.householdsApi
      .apiHouseholdsHouseholdIdMembersMemberUserIdRejectPost(hid, userId)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.memberActionUserId.set(null)))
      .subscribe({
        next: () => {
          this.toast.success(this.transloco.translate('household.rejectSuccess'));
          this.loadMembers(hid);
          this.reloadHouseholds();
          this.activeHousehold.notifySidebarRefresh();
        },
        error: (err) => this.toast.fromApiError(err, 'household.rejectFailed'),
      });
  }

  showApproveReject(m: HouseholdMemberDto): boolean {
    if (!this.isParentUi() || !m.userId) {
      return false;
    }
    if ((m.membershipStatus ?? '').toLowerCase() !== 'pending') {
      return false;
    }
    if (m.userId === this.currentUserId()) {
      return false;
    }
    return true;
  }

  openDeleteDialog(h: HouseholdMineDto): void {
    this.deleteTarget.set(h);
    this.deleteConfirmDraft.set('');
  }

  cancelDelete(): void {
    this.deleteTarget.set(null);
    this.deleteConfirmDraft.set('');
  }

  onDeleteConfirmInput(value: string): void {
    this.deleteConfirmDraft.set(value);
  }

  submitDelete(): void {
    const h = this.deleteTarget();
    if (!h?.id || !this.isParentUi()) {
      return;
    }
    const expected = (h.name ?? '').trim();
    if (this.deleteConfirmDraft().trim() !== expected) {
      this.toast.error(this.transloco.translate('household.deleteConfirmMismatch'));
      return;
    }
    this.deleteSubmitting.set(true);
    this.householdsApi
      .apiHouseholdsHouseholdIdDeletePost(h.id, { confirmationName: expected })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.deleteSubmitting.set(false))
      )
      .subscribe({
        next: () => {
          this.toast.success(this.transloco.translate('household.deleteSuccess'));
          this.deleteTarget.set(null);
          this.deleteConfirmDraft.set('');
          if (this.activeHousehold.activeHouseholdId() === h.id) {
            this.activeHousehold.clear();
          }
          this.reloadHouseholds();
        },
        error: (err) => this.toast.fromApiError(err, 'household.deleteFailed'),
      });
  }

  inviteRoleLabel(): string {
    const r = this.inviteRoleForm.controls.role.value;
    return r === MemberRole.Parent
      ? this.transloco.translate('layout.memberRoleParent')
      : this.transloco.translate('layout.memberRoleChild');
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

  /** แสดงสถานะสมาชิกในบ้าน (Pending / Active) จากค่า enum ของ API */
  memberStatusLabel(status: string | null | undefined): string {
    const s = (status ?? '').trim().toLowerCase();
    if (s === 'pending') {
      return this.transloco.translate('household.membershipPending');
    }
    if (s === 'active') {
      return this.transloco.translate('household.membershipActive');
    }
    return status?.trim() || '—';
  }
}
