import { computed, Injectable, inject } from '@angular/core';
import { AuthSessionService } from './auth-session.service';
import { parseJwtUserDisplay } from './jwt-payload';

/**
 * สิทธิ์ตาม household role ใน JWT (และนโยบายเมื่อไม่มี role)
 *
 * ไม่มี token / ไม่มี claim household_role: ซ่อน Rewards และ Wishlist (ไม่ลิงก์ไป route ที่ guard)
 */
@Injectable({ providedIn: 'root' })
export class MenuAccessService {
  private readonly session = inject(AuthSessionService);

  private readonly display = computed(() => parseJwtUserDisplay(this.session.token()));

  readonly canShowQuestBoard = computed(() => true);
  readonly canShowQuestManagement = computed(() => true);
  readonly canShowSettings = computed(() => true);

  readonly canShowRewards = computed(() => {
    if (!this.session.isAuthenticated()) {
      return false;
    }
    return this.display().householdRole === 'parent';
  });

  readonly canShowWishlist = computed(() => {
    if (!this.session.isAuthenticated()) {
      return false;
    }
    return this.display().householdRole === 'child';
  });
}
