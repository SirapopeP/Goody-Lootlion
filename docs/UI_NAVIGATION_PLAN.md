# แผน Navigation และมุมมอง UI (Lootlion)

อ้างอิงเช็คลิสงาน: [PHASES_CHECKLIST.md](./PHASES_CHECKLIST.md)

---

## สรุปแนวทาง

| บทบาท | Shell | Navigation |
|--------|-------|------------|
| **Parent** | `DashboardLayoutComponent` | Top nav + sidebar (coin/EXP/level จริง) |
| **Child** | `ChildLayoutComponent` | Bottom nav: MISSION \| REWARD \| FAMILY \| SETTING |

Route แยกด้วย `childShellCanMatch` / `parentShellCanMatch` ใน [`app.routes.ts`](../client/lootlion-web/src/app/app.routes.ts)

---

## Parent — Quest Board (`/`)

| Panel | Component | API |
|-------|-----------|-----|
| Overview | `HomeComponent` | Households mine |
| Rank (top 5) | `HomeRankPanelComponent` | `GET /api/Wallet/.../leaderboard` |
| Template ซ้าย | `HomeMissionPanelComponent` | Missions templates |
| Instance กลาง | `HomeMissionCenterComponent` | board / mine / pending |

| Route | หน้า |
|-------|------|
| `/missions` | `MissionsReportPageComponent` — สถิติ + leaderboard เต็ม |
| `/rewards` | `RewardsPageComponent` — catalog + wishlist approve |
| `/profile` | `ProfilePageComponent` — wallet + ledger |

---

## Child — Mobile shell

| แท็บ | Route | Component |
|------|-------|-------------|
| MISSION | `/` | `ChildMissionsPageComponent` |
| REWARD | `/rewards` | `ChildRewardsPageComponent` |
| FAMILY | `/family` | `ChildFamilyPageComponent` |
| SETTING | `/settings` | `ChildSettingsPageComponent` |

`/households` ใช้ร่วมได้จาก child shell (จัดการครอบครัว)

---

## Wallet + Rank (เฟส 4b)

- Sidebar: `WalletFacadeService` → balance + level progress
- Family list: level จาก leaderboard
- หลัง approve ภารกิจ / redeem รางวัล → `wallet.requestRefresh()`

---

## ถัดไป (ยังไม่ทำ)

- **5b** Commission
- **6** Deploy / E2E
- แก้ไข Mission Template, ตัวละคร/cosmetic
