import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { RewardsService } from '../../api/generated/api/rewards.service';
import { WishlistService } from '../../api/generated/api/wishlist.service';
import { ApproveWishlistRequest } from '../../api/generated/model/approveWishlistRequest';
import { CreateRewardRequest } from '../../api/generated/model/createRewardRequest';
import { CreateWishlistRequest } from '../../api/generated/model/createWishlistRequest';
import { RedeemBody } from '../../api/generated/model/redeemBody';
import { RedemptionDto } from '../../api/generated/model/redemptionDto';
import { RewardCatalogItemDto } from '../../api/generated/model/rewardCatalogItemDto';
import { WishlistItemDto } from '../../api/generated/model/wishlistItemDto';

@Injectable({ providedIn: 'root' })
export class RewardFacadeService {
  private readonly rewards = inject(RewardsService);
  private readonly wishlist = inject(WishlistService);

  listCatalog(householdId: string): Observable<RewardCatalogItemDto[]> {
    return this.rewards.apiRewardsHouseholdHouseholdIdCatalogGet(householdId);
  }

  createCatalogItem(body: CreateRewardRequest): Observable<RewardCatalogItemDto> {
    return this.rewards.apiRewardsCatalogPost(body);
  }

  listWishlist(householdId: string): Observable<WishlistItemDto[]> {
    return this.wishlist.apiWishlistHouseholdHouseholdIdGet(householdId);
  }

  createWishlistItem(body: CreateWishlistRequest): Observable<WishlistItemDto> {
    return this.wishlist.apiWishlistPost(body);
  }

  approveWishlist(wishlistItemId: string, body: ApproveWishlistRequest): Observable<RewardCatalogItemDto> {
    return this.wishlist.apiWishlistWishlistItemIdApprovePost(wishlistItemId, body);
  }

  rejectWishlist(wishlistItemId: string): Observable<WishlistItemDto> {
    return this.wishlist.apiWishlistWishlistItemIdRejectPost(wishlistItemId);
  }

  redeem(rewardId: string, householdId: string): Observable<RedemptionDto> {
    const body: RedeemBody = { householdId };
    return this.rewards.apiRewardsRewardIdRedeemPost(rewardId, body);
  }

  listRedemptions(householdId: string): Observable<RedemptionDto[]> {
    return this.rewards.apiRewardsHouseholdHouseholdIdRedemptionsGet(householdId);
  }
}
