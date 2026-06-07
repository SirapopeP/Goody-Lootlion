import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { RedemptionDto } from '../../../api/generated/model/redemptionDto';
import { RewardCatalogItemDto } from '../../../api/generated/model/rewardCatalogItemDto';
import { WishlistItemDto } from '../../../api/generated/model/wishlistItemDto';
import { ActiveHouseholdService } from '../../../core/household/active-household.service';
import { RewardFacadeService } from '../../../core/rewards/reward-facade.service';
import { ToastService } from '../../../core/toast/toast.service';
import { WalletFacadeService } from '../../../core/wallet/wallet-facade.service';

@Component({
  selector: 'app-child-rewards-page',
  standalone: true,
  imports: [ReactiveFormsModule, TranslocoPipe],
  templateUrl: './child-rewards-page.component.html',
  styleUrl: './child-rewards-page.component.scss',
})
export class ChildRewardsPageComponent {
  private readonly rewardsApi = inject(RewardFacadeService);
  readonly wallet = inject(WalletFacadeService);
  readonly activeHousehold = inject(ActiveHouseholdService);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly activeTab = signal<'catalog' | 'wishlist' | 'history'>('catalog');
  readonly catalog = signal<RewardCatalogItemDto[]>([]);
  readonly wishlist = signal<WishlistItemDto[]>([]);
  readonly redemptions = signal<RedemptionDto[]>([]);
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly redeemingId = signal<string | null>(null);

  readonly wishlistForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(2000)]],
    suggestedCoinCost: [10, [Validators.min(1)]],
  });

  constructor() {
    this.reload();
    this.activeHousehold.sidebarRefresh$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reload());
  }

  setTab(tab: 'catalog' | 'wishlist' | 'history'): void {
    this.activeTab.set(tab);
  }

  canAfford(cost: number | undefined): boolean {
    const balance = this.wallet.balance()?.coinBalance ?? 0;
    return balance >= (cost ?? 0);
  }

  reload(): void {
    const hid = this.activeHousehold.activeHouseholdId();
    if (!hid) {
      this.catalog.set([]);
      this.wishlist.set([]);
      this.redemptions.set([]);
      return;
    }
    this.loading.set(true);
    this.wallet.requestRefresh(hid);
    forkJoin({
      catalog: this.rewardsApi.listCatalog(hid).pipe(catchError(() => of([] as RewardCatalogItemDto[]))),
      wishlist: this.rewardsApi.listWishlist(hid).pipe(catchError(() => of([] as WishlistItemDto[]))),
      redemptions: this.rewardsApi.listRedemptions(hid).pipe(catchError(() => of([] as RedemptionDto[]))),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe(({ catalog, wishlist, redemptions }) => {
        this.catalog.set(catalog ?? []);
        this.wishlist.set(wishlist ?? []);
        this.redemptions.set(redemptions ?? []);
      });
  }

  redeem(item: RewardCatalogItemDto): void {
    const hid = this.activeHousehold.activeHouseholdId();
    if (!hid || !item.id || !this.canAfford(item.costCoin ?? 0)) {
      return;
    }
    this.redeemingId.set(item.id);
    this.rewardsApi
      .redeem(item.id, hid)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.redeemingId.set(null))
      )
      .subscribe({
        next: () => {
          this.toast.success(this.transloco.translate('rewards.redeemSuccess'));
          this.wallet.requestRefresh(hid);
          this.reload();
        },
        error: (err) => this.toast.fromApiError(err, 'rewards.redeemFailed'),
      });
  }

  submitWishlist(): void {
    const hid = this.activeHousehold.activeHouseholdId();
    if (!hid || this.wishlistForm.invalid) {
      this.wishlistForm.markAllAsTouched();
      return;
    }
    const v = this.wishlistForm.getRawValue();
    this.submitting.set(true);
    this.rewardsApi
      .createWishlistItem({
        householdId: hid,
        title: v.title.trim(),
        description: v.description.trim() || null,
        suggestedCoinCost: v.suggestedCoinCost,
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.submitting.set(false))
      )
      .subscribe({
        next: () => {
          this.toast.success(this.transloco.translate('rewards.wishlistCreateSuccess'));
          this.wishlistForm.reset({ title: '', description: '', suggestedCoinCost: 10 });
          this.reload();
        },
        error: (err) => this.toast.fromApiError(err, 'rewards.wishlistCreateFailed'),
      });
  }
}
