import { Injectable, signal } from '@angular/core';
import { Observable, Subject } from 'rxjs';
/** รายการจาก GET /api/Households/mine — ต้องมี id + (ถ้ามี) membershipStatus */
export type HouseholdListEntry = {
  id?: string;
  name?: string | null;
  membershipStatus?: string | null;
};

const STORAGE_KEY = 'lootlion_active_household_id';

/**
 * บ้านที่ใช้เป็นบริบทใน UI (sidebar, หน้าแรก)
 * - แยกจาก JWT: เซิร์ฟเวอ้ให้ claim แค่ household_role (parent/child รวมทุกบ้าน) ไม่มี household_id
 * - เก็บ id ที่ผู้ใช้เลือกใน sessionStorage เพื่อให้สอดคล้องเมื่อมีหลายครอบครัว
 */
@Injectable({ providedIn: 'root' })
export class ActiveHouseholdService {
  private readonly _activeHouseholdId = signal<string | null>(null);

  private readonly sidebarRefresh = new Subject<void>();

  /** แจ้งให้แดชบอร์ดรีเฟรช sidebar เมื่อผู้ใช้เปลี่ยนบ้านจากหน้าจัดการ (ไม่ emit ตอน pick เงียบๆ ใน loadFamily) */
  readonly sidebarRefresh$: Observable<void> = this.sidebarRefresh.asObservable();

  readonly activeHouseholdId = this._activeHouseholdId.asReadonly();

  constructor() {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    this._activeHouseholdId.set(raw && raw.length > 0 ? raw : null);
  }

  private applyId(id: string | null): void {
    this._activeHouseholdId.set(id);
    if (id) {
      sessionStorage.setItem(STORAGE_KEY, id);
    } else {
      sessionStorage.removeItem(STORAGE_KEY);
    }
  }

  /**
   * เลือกบ้านหลังโหลดรายการจาก API — ไม่ยิง sidebarRefresh (ใช้ในแดชบอร์ด/โหลดซ้ำ)
   */
  pickHousehold(households: HouseholdListEntry[]): HouseholdListEntry | null {
    if (!households?.length) {
      this.applyId(null);
      return null;
    }
    const validIds = new Set(
      households.map((h) => h.id).filter((x): x is string => typeof x === 'string' && x.length > 0)
    );
    const current = this._activeHouseholdId();
    if (current && validIds.has(current)) {
      return households.find((h) => h.id === current) ?? households[0];
    }
    const first = households[0];
    if (first?.id) {
      this.applyId(first.id);
    }
    return first ?? null;
  }

  /** ผู้ใช้เลือกบ้านจาก UI — แจ้งให้ sidebar โหลดใหม่ */
  setActiveHouseholdFromUser(householdId: string): void {
    const cur = this._activeHouseholdId();
    if (cur === householdId) {
      return;
    }
    this.applyId(householdId);
    this.sidebarRefresh.next();
  }

  /** รีเฟรช sidebar เมื่อรายชื่อสมาชิกเปลี่ยน (เช่น เชิญสมาชิก) โดยไม่เปลี่ยนบ้านที่เลือก */
  notifySidebarRefresh(): void {
    this.sidebarRefresh.next();
  }

  clear(): void {
    this.applyId(null);
  }
}
