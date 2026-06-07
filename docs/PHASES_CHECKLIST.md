# เช็คลิสงานแบ่งเฟส (Lootlion)

ใช้รายการนี้สั่งงานเพิ่มเติมทีละเฟสได้ — ติ๊กเมื่อเสร็จ

เอกสารนี้จับคู่กับ **แผนงาน UI (Figma)** โฟลว์แบบ RPG/ภารกิจ — ชื่อในรูปอาจเป็น working title (เช่น Growtale-RPG) แต่โค้ดและ API ใช้ชื่อ **Lootlion**

### สถานะปัจจุบัน (สรุป)

| เฟส | สถานะ |
|-----|--------|
| 0, 0b, 1, 2 | **ครบ** |
| 3 | **ครบ** — ครอบครัว API + UI (`/households`), onboarding, sidebar, แยกสิทธิ์ parent/child บางเมนู |
| 4c-1 | **ครบ** — Refactor `MissionTemplate` + `MissionInstance`, migration, API ใหม่ |
| 4c-2 | **ครบ** — Parent template panel (`HomeMissionPanelComponent`) — BoardClaim / DirectAssign |
| 4c-3 | **ครบ** — Panel กลาง board/claim/submit/approve (`HomeMissionCenterComponent`) |
| 4c-4 | **ครบ** — Recurrence (Daily/Weekly/Monthly/Interval) + spawn หลัง approve / manual หลัง reject |
| 4b | **ครบ** — Wallet จริงใน sidebar, leaderboard API, rank panel, `/missions` report, profile wallet |
| 4c-child | **ครบ** — Child shell + bottom nav MISSION/REWARD/FAMILY/SETTING |
| 5 | **ครบ** — Parent `/rewards` (catalog + wishlist approve) + Child rewards tab (redeem + wishlist) |
| 5b | **รอ** — Commission (defer) |
| 6 | **รอ** — Deploy / E2E |

---

## แผนงานจากภาพ → จับคู่เฟสใน repo

| บล็อกในแผนงาน | ความหมายสั้น ๆ | เฟสใน checklist |
|----------------|----------------|-------------------|
| Quest Board — panel ซ้าย MISSION/REWARDS (+) | Parent จัดการ **Template** เพิ่ม/ลบ/เปิดรอบ | **เฟส 4c-2** |
| หน้าหลัก — สร้าง Family, Dashboard, Commission | ศูนย์กลางผู้ปกครอง / ภาพรวม | เฟส 3 (ครบ) + เฟส 4b + 5b |
| สมัครสมาชิก — สร้าง user + สร้าง/เลือก join family | ลงทะเบียน + onboarding ครอบครัว | เฟส 2 + 3 (ครบ) |
| Child — แท็บ MISSION (SUBMIT, สถิติ POINT) | เด็กทำภารกิจ — เมนูแยกจาก parent | **เฟส 4c** |
| Child — แท็บ REWARD (REDEEM) | เด็กแลกรางวัล | **เฟส 5** |
| Child — แท็บ FAMILY / SETTING | ภาพรวมครอบครัว + ตั้งค่า | เฟส 4c + profile |
| เมนูจัดการภารกิจ — แก้ไข, ได้ exp/coin | CRUD เต็ม + รางวัลเมื่อทำ | เฟส 4b ขึ้นไป |
| เมนูจัดการภารกิจ (ความถี่) — รายวัน/เดือน/ปี/กำหนดเอง | ภารกิจซ้ำตามรอบ | **เฟส 4c-4** (ครบ) |
| รายงานภารกิจ / กระดาน Rank — อนุมัติ, ลีดเดอร์บอร์ด | workflow อนุมัติใน panel กลาง + Rank ภายหลัง | **4c-3** (อนุมัติครบ) + **4b** (Rank) |
| เมนูแลกรับรางวัล (Parent catalog) | จัดการ catalog + แลกของ | เฟส 5 |
| โปรไฟล์ของฉัน — Level, EXP, ปรับแต่งตัวละคร | สถานะผู้เล่น + คอสเมติก | เฟส 4b + แบ็กล็อก |

**สิ่งที่แผนงามีแต่เช็คลิสเดิมยังไม่ได้แตกชัด:** ความถี่ของภารกิจ, กระดาน Rank, โปรไฟล์/เลเวล/ตัวละคร, **การตั้งค่า commission** — ได้เติมเป็นรายการย่อยด้านล่างแล้ว

---

## เฟส 0 — สภาพแวดล้อม

- [x] ติดตั้ง .NET SDK 8 และ Node.js 20+
- [x] สร้างฐานข้อมูล PostgreSQL (หรือใช้ `docker compose up postgres`)
- [x] รัน migration: `dotnet ef database update --project src/Lootlion.Infrastructure --startup-project src/Lootlion.Api`
- [x] รัน API: `dotnet run --project src/Lootlion.Api` แล้วเปิด `/swagger`
- [x] รัน Angular: `cd client/lootlion-web && npm install && npm start`

---

## เฟส 0b — Observability (สำหรับ dev แบบ local)

**Scope นี้สำหรับ “dev = รันบนเครื่อง” ถือว่าครบแล้ว:** ดู log จากเทอร์มินัลที่รัน API ได้ทันที (Serilog เป็น JSON บรรทัดเดียวต่อ event)

- [x] Backend: Serilog → stdout (RenderedCompactJsonFormatter) + request logging
- [x] Backend: `/metrics` สำหรับ Prometheus (เมื่อ deploy จริงจะให้ Prometheus scrape)
- [x] เอกสารแนวทาง Loki + Grafana + Prometheus: `docs/OBSERVABILITY_LOKI.md`

**ยังไม่บังคับใน dev local (ทำตอนขึ้น K8s / staging):** ติดตั้ง Promtail → Loki, Prometheus scrape, Grafana — อ้างอิงใน `OBSERVABILITY_LOKI.md`

---

## เฟส 1 — สัญญา API กับ Client

- [x] ส่งออก OpenAPI จาก Swagger ขณะรัน API
- [x] รัน `npm run generate:api` ใน `client/lootlion-web` (ปรับ URL ให้ตรงพอร์ต)
- [x] ตั้งค่า `HttpClient` base URL (environment) ชี้ API

---

## เฟส 2 — Auth บน Angular

- [x] หน้า login / register เรียก `POST /api/auth/login`, `register`
- [x] เก็บ JWT + refresh token (`sessionStorage` + `AuthSessionService`) — รีเฟรชแท็บแล้วยังล็อกอิน; access หมดอายุแล้ว interceptor ลอง `POST /api/Auth/refresh`
- [x] HTTP interceptor แนบ `Authorization: Bearer` และจัดการ refresh เมื่อได้ 401
- [x] i18n (Transloco) + สลับภาษา TH/EN บน UI หลัก
- [x] (ตามแผนงาน) หลังสมัคร: flow **สร้างครอบครัวใหม่ / เลือกเข้าร่วมครอบครัว** — ใน `RegisterWizard` (ผู้ปกครองสร้างใหม่หรือเข้าร่วมบ้านที่เปิดรับ, เด็กเลือกบ้าน) เรียก API ที่เกี่ยวข้อง

---

## เฟส 3 — ครอบครัว (Family) + มุมมอง “หน้าหลัก” ระดับครอบครัว

- [x] Backend: ครอบครัวและสมาชิก (`POST /api/Households`, `GET /api/Households/mine`, `GET/POST .../members`, รวมรายการเปิดให้เด็กสมัคร ฯลฯ — ดู `HouseholdsController`)
- [x] หน้า `/households` — `HouseholdsPageComponent`: เลือกบ้านที่ใช้เป็นบริบท (เก็บใน `sessionStorage` ผ่าน `ActiveHouseholdService`), รายการจาก `GET mine`, สร้างบ้านใหม่ (`POST`), ดูสมาชิก, ผู้ปกครองเชิญด้วย UUID (`POST .../members`)
- [x] เชิญสมาชิกด้วย `MemberUserId` บน UI (ฟอร์ม UUID + เลือกบทบาท Parent/Child) — ค้นหาจากอีเมลเป็นงานเสริมภายหลังได้
- [x] แยกสิทธิ์บางส่วนตามบทบาท (**Parent / Child**): claim ใน JWT + `MenuAccessService` + `canMatch` บน `/rewards` (ผู้ปกครอง) และ `/wishlist` (เด็ก) — ฟีเจอร์ “จัดการ” อื่นในแผนงานยังผูกเฟสถัดไป
- [x] **Dashboard ภาพรวม** หน้าแรก: sidebar ใช้บ้านที่เลือก (ไม่ใช้แค่ `households[0]` อีกต่อไป) + `HomeComponent` มีบล็อกภาพรวมครอบครัว (ชื่อ + จำนวนสมาชิก); โซนภารกิจ/เหรียญยังเป็น placeholder จนกว่าเฟส 4
- [x] ข้อความหลักของหน้าครอบครัว/ภาพรวมหน้าแรกใช้คีย์ Transloco (th/en)

หมายเหตุ: รายการ “จัดการ User ระดบระบบทั้งโปรเจกต์” ถ้าไม่ได้ออกแบบให้มีแอดมินกลาง — ให้ตีความเป็น **จัดการสมาชิกในครอบครัว** (ผู้ปกครองจัดการเด็กในบ้าน)

---

## เฟส 4 — ภารกิจ + กระเป๋า + รายงาน / Rank

แผน UI แยก Parent / Child: [UI_NAVIGATION_PLAN.md](./UI_NAVIGATION_PLAN.md)

### เฟส 4c-1 — Domain + Migration + API (ครบ)

- [x] `MissionTemplate` + `MissionInstance` + enums (`AssignmentMode`, `RecurrenceKind`, `InstanceStatus`)
- [x] `Household.TimeZoneId` (default `Asia/Bangkok`)
- [x] Migration `MissionTemplateAndInstance` + ย้ายข้อมูลจาก `Missions` เดิม
- [x] Services: `MissionTemplateService`, `MissionInstanceService`, `MissionRecurrenceService`, `MissionSpawnService`
- [x] API ใหม่ภายใต้ `/api/Missions/templates|board|mine|pending|instances/...`

### เฟส 4c-2 — Parent: Template panel (ครบ)

`HomeMissionPanelComponent` — โหลด `GET templates/household`, สร้าง `POST templates`, ลบ `POST templates/{id}/cancel`

- [x] โหมด **BoardClaim** (ไม่บังคับ assign) / **DirectAssign**
- [x] ปุ่ม **เปิดรอบใหม่** เมื่อ `canSpawnNextRound` (หลัง reject recurring)
- [x] `MissionApiService` ฝั่ง Angular (แทน generated client ชั่วคราว)

### เฟส 4c-3 — Panel กลาง: Board + Workflow (ครบ)

`HomeMissionCenterComponent` — แท็บ Board / Mine / Pending / Done

- [x] Child: **claim** จากบอร์ด, **submit** ภารกิจของตัวเอง
- [x] Parent: **approve / reject** รายการ pending
- [x] Child bottom nav แยก — `ChildLayoutComponent` + 4 แท็บ

### เฟส 4c-4 — Recurrence (ครบ)

- [x] สร้าง template พร้อม Daily / Weekly / Monthly / IntervalDays
- [x] Spawn รอบถัดไปอัตโนมัติหลัง **approve** เท่านั้น
- [x] หลัง **reject** — หยุดจน Parent กด **เปิดรอบใหม่** (`POST templates/{id}/spawn`)

### เฟส 4b — กระเป๋า + Rank (ครบ)

- [x] แสดงยอด **coin / exp / level** จริงใน sidebar (`WalletFacadeService` + `GET .../balance`)
- [x] **กระดาน Rank** — `GET .../leaderboard` + `HomeRankPanelComponent`
- [x] หน้า `/missions` — `MissionsReportPageComponent` (สถิติ + leaderboard)
- [x] หน้า `/profile` — balance + ledger ล่าสุด

### เฟส 4c-child — Child: มุมมองและเมนูแยก (ครบ)

อ้างอิง mockup mobile — แท็บล่าง **MISSION | REWARD | FAMILY | SETTING**

- [x] Layout แยกตาม role (`childShellCanMatch` + `ChildLayoutComponent`)
- [x] แท็บ **MISSION** — `ChildMissionsPageComponent` + `HomeMissionCenterComponent`
- [x] แท็บ **FAMILY** — `ChildFamilyPageComponent`
- [x] แท็บ **SETTING** — `ChildSettingsPageComponent`

### เฟส 4 — ขยาย (หลัง 4c)

- [ ] UI **แก้ไข** template (ต้องมี API)
- [ ] หน้า **โปรไฟล์ของฉัน**: Level, EXP, ข้อมูลบัญชี (ส่วน **ปรับแต่งตัวละคร** = แบ็กล็อก)

---

## เฟส 5 — รางวัล + Wishlist + แลกรับรางวัล (ครบ)

- [x] **Catalog รางวัล** — Parent `RewardsPageComponent` (`/rewards`)
- [x] Wishlist → อนุมัติ/ปฏิเสธ → catalog (`RewardFacadeService`)
- [x] Child **แลกของด้วย coin** — `ChildRewardsPageComponent` แท็บ REWARD

---

## เฟส 5b — การตั้งค่า Commission (ตามแผนงาน — ยังไม่มีใน API เดิม)

- [ ] นิยามความหมายในระบบ: สัดส่วน coin/exp, ค่าคอมมิชชันให้ผู้ปกครอง, หรือการหักเปอร์เซ็นต์เมื่อแลกรางวัล
- [ ] ออกแบบ API + UI (มักผูกกับ **Household** หรือบทบาท Parent) แล้วค่อยใส่ในเฟสที่เหมาะสม (มักหลังเฟส 4–5)

---

## เฟส 6 — คุณภาพและการ deploy

- [ ] เพิ่ม E2E / integration tests ตามความจำเป็น
- [ ] ตั้งค่า JWT signing key / connection string ใน secret จริง (ไม่ commit ลง repo)
- [ ] Deploy API + static frontend (หรือรวม reverse proxy)
- [ ] (ถ้าต้องการ) Capacitor / แพ็กเกจสโตร์
- [ ] บน cluster: ต่อ Promtail (หรือ Alloy) → Loki + ตั้ง Prometheus scrape `/metrics` + Grafana (ดู `OBSERVABILITY_LOKI.md`)

---

## แนวทางเป้าหมายต่อไป (สรุปลำดับแนะนำ)

| ลำดับ | โฟกัส | เหตุผลสั้น ๆ |
|--------|--------|----------------|
| 1 | **เฟส 5b — Commission** | หลังมีกติกาเรื่อง coin/exp ชัด (defer) |
| 2 | **เฟส 6 — ทดสอบ + deploy + observability บน K8s** | หลังฟีเจอร์หลักคงที่ |
| — | แก้ไข Mission Template, cosmetic profile | backlog |

รายละเอียด navigation Parent/Child: [UI_NAVIGATION_PLAN.md](./UI_NAVIGATION_PLAN.md)  
รายละเอียดรันท้องถิ่น: [LOCAL_DEVELOPMENT.md](./LOCAL_DEVELOPMENT.md)
