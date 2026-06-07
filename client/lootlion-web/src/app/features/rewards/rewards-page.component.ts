import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { RewardCatalogItemDto } from '../../api/generated/model/rewardCatalogItemDto';
import { WishlistItemDto } from '../../api/generated/model/wishlistItemDto';
import { ActiveHouseholdService } from '../../core/household/active-household.service';
import { RewardFacadeService } from '../../core/rewards/reward-facade.service';
import { ToastService } from '../../core/toast/toast.service';
import { WalletFacadeService } from '../../core/wallet/wallet-facade.service';

@Component({
  selector: 'app-rewards-page',
  standalone: true,
  imports: [ReactiveFormsModule, TranslocoPipe, RouterLink],
  templateUrl: './rewards-page.component.html',
  styleUrl: './rewards-page.component.scss',
})
export class RewardsPageComponent {
  private readonly rewardsApi = inject(RewardFacadeService);
  readonly wallet = inject(WalletFacadeService);
  readonly activeHousehold = inject(ActiveHouseholdService);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly activeTab = signal<'catalog' | 'wishlist' | 'create'>('catalog');
  readonly catalog = signal<RewardCatalogItemDto[]>([]);
  readonly wishlist = signal<WishlistItemDto[]>([]);
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly approveTargetId = signal<string | null>(null);
  readonly approveCost = signal(10);

  readonly createForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(2000)]],
    costCoin: [10, [Validators.required, Validators.min(1)]],
  });

  constructor() {
    this.reload();
    this.activeHousehold.sidebarRefresh$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reload());
  }

  setTab(tab: 'catalog' | 'wishlist' | 'create'): void {
    this.activeTab.set(tab);
  }

  pendingWishlist(): WishlistItemDto[] {
    return this.wishlist().filter((w) => (w.status ?? '').toLowerCase() === 'pending');
  }

  reload(): void {
    const hid = this.activeHousehold.activeHouseholdId();
    if (!hid) {
      this.catalog.set([]);
      this.wishlist.set([]);
      return;
    }
    this.loading.set(true);
    this.wallet.requestRefresh(hid);
    forkJoin({
      catalog: this.rewardsApi.listCatalog(hid).pipe(catchError(() => of([] as RewardCatalogItemDto[]))),
      wishlist: this.rewardsApi.listWishlist(hid).pipe(catchError(() => of([] as WishlistItemDto[]))),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe(({ catalog, wishlist }) => {
        this.catalog.set(catalog ?? []);
        this.wishlist.set(wishlist ?? []);
      });
  }

  submitCreate(): void {
    const hid = this.activeHousehold.activeHouseholdId();
    if (!hid || this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }
    const v = this.createForm.getRawValue();
    this.submitting.set(true);
    this.rewardsApi
      .createCatalogItem({
        householdId: hid,
        title: v.title.trim(),
        description: v.description.trim() || null,
        costCoin: v.costCoin,
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.submitting.set(false))
      )
      .subscribe({
        next: () => {
          this.toast.success(this.transloco.translate('rewards.createSuccess'));
          this.createForm.reset({ title: '', description: '', costCoin: 10 });
          this.activeTab.set('catalog');
          this.reload();
        },
        error: (err) => this.toast.fromApiError(err, 'rewards.createFailed'),
      });
  }

  openApprove(item: WishlistItemDto): void {
    if (!item.id) {
      return;
    }
    this.approveTargetId.set(item.id);
    this.approveCost.set(item.suggestedCoinCost ?? 10);
  }

  dismissApprove(): void {
    this.approveTargetId.set(null);
  }

  confirmApprove(): void {
    const id = this.approveTargetId();
    if (!id) {
      return;
    }
    this.submitting.set(true);
    this.rewardsApi
      .approveWishlist(id, { finalCostCoin: this.approveCost() })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.submitting.set(false))
      )
      .subscribe({
        next: () => {
          this.toast.success(this.transloco.translate('rewards.approveSuccess'));
          this.approveTargetId.set(null);
          this.reload();
        },
        error: (err) => this.toast.fromApiError(err, 'rewards.approveFailed'),
      });
  }

  reject(item: WishlistItemDto): void {
    if (!item.id) {
      return;
    }
    this.submitting.set(true);
    this.rewardsApi
      .rejectWishlist(item.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.submitting.set(false))
      )
      .subscribe({
        next: () => {
          this.toast.success(this.transloco.translate('rewards.rejectSuccess'));
          this.reload();
        },
        error: (err) => this.toast.fromApiError(err, 'rewards.rejectFailed'),
      });
  }
}
