# เช็คลิสงานแบ่งเฟส (Lootlion)

ใช้รายการนี้สั่งงานเพิ่มเติมทีละเฟสได้ — ติ๊กเมื่อเสร็จ

เอกสารนี้จับคู่กับ **แผนงาน UI (Figma)** โฟลว์แบบ RPG/ภารกิจ — ชื่อในรูปอาจเป็น working title (เช่น Growtale-RPG) แต่โค้ดและ API ใช้ชื่อ **Lootlion**

### สถานะปัจจุบัน (สรุป)

| เฟส | สถานะ |
|-----|--------|
| 0, 0b, 1, 2 | **ครบ** |
| 3 | **กำลังทำ** — API + onboarding สมัคร + shell (sidebar ครอบครัว) + แยกสิทธิ์บางส่วนทำแล้ว; หน้า `/households` ยัง placeholder, ยังไม่มี UI เชิญสมาชิก, หน้าแรกยังไม่สรุปภารกิจ/เหรียญจริง |
| 4 เป็นต้นไป | ยังไม่เริ่มตาม checklist |

---

## แผนงานจากภาพ → จับคู่เฟสใน repo

| บล็อกในแผนงาน | ความหมายสั้น ๆ | เฟสใน checklist |
|----------------|----------------|-------------------|
| หน้าหลัก — สร้าง Family, จัดการ User/ภารกิจ/รางวัล, Dashboard, Commission | ศูนย์กลับ + มุมมองผู้ดูแลครอบครัว / ภาพรวม | เฟส 3 (ครอบครัว + สมาชิก) + เฟส 4–5 (จัดการภารกิจ/รางวัล) + รายการถัดไปด้านล่าง |
| สมัครสมาชิก — สร้าง user + สร้าง/เลือก join family | ลงทะเบียน + onboarding ครอบครัว | เฟส 2 (user) + เฟส 3 (สร้าง/เข้าร่วมครอบครัว) |
| เมนูจัดการภารกิจ — สร้าง-แก้ไข, ได้ exp/coin | CRUD ภารกิจ + รางวัลเมื่อทำ | เฟส 4 |
| เมนูจัดการภารกิจ (ความถี่) — รายวัน/เดือน/ปี/กำหนดเอง | ภารกิจซ้ำตามรอบ | เฟส 4 (ขยาย — ถ้า domain รองรับ) |
| รายงานภารกิจ / กระดาน Rank — อนุมัติ, ลีดเดอร์บอร์ด | รายงาน + workflow อนุมัติ + อันดับ | เฟส 4 |
| เมนูแลกรับรางวัล | ใช้ coin แลกของใน catalog | เฟส 5 |
| โปรไฟล์ของฉัน — Level, EXP, ปรับแต่งตัวละคร | สถานะผู้เล่น + (ถ้าทำ) คอสเมติก | เฟส 4 (เลเวล/EXP/กระเป๋า) + แบ็กล็อก UI |

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

- [ ] UI สร้าง / แก้ไขภารกิจ, มอบหมายหรือส่งคำขอทำ, ได้ **exp / coin** เมื่อสำเร็จ (สอดคล้อง API)
- [ ] Workflow **อนุมัติ / ปฏิเสธ** ภารกิจ (ฝั่งผู้ปกครองตามแผนงาน)
- [ ] **รายงานภารกิจ** + **กระดาน Rank / อันดับ** (แทนที่ placeholder “Quest board” เมื่อมีข้อมูล)
- [ ] **ความถี่ภารกิจ** (รายวัน / รายเดือน / รายปี / กำหนดเอง) — ขึ้นกับว่าโมเดล `Mission` รองรับ recurrence หรือไม่; ถ้ายังไม่รองรับ ให้แยกเป็นงานย่อย backend ก่อน UI
- [ ] แสดงยอด **coin / exp / level** และ **ledger**
- [ ] หน้า **โปรไฟล์ของฉัน**: แสดง Level, EXP, ข้อมูลบัญชี (ส่วน **ปรับแต่งตัวละคร** = แบ็กล็อกถ้าต้องการเกมมิ่งเต็มรูปแบบ)

---

## เฟส 5 — รางวัล + Wishlist + แลกรับรางวัล

- [ ] **Catalog รางวัล** และ **แลกของด้วย coin** (เมนู “แลกรับรางวัล” ในแผนงาน)
- [ ] Wishlist → อนุมัติ → ปรากฏใน catalog (ตามสัญญา API ที่มี)
- [ ] (ถ้ามีใน requirement) เชื่อมกับการแสดงรางวัลบน Dashboard

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
| 1 | **เฟส 3 — ครอบครัว** | ฐานสิทธิ์และข้อมูลสำหรับภารกิจ/กระเป๋า/รางวัลที่ผูก `HouseholdId` |
| 2 | **เฟส 4 — ภารกิจ + กระเป๋า + รายงาน/Rank** | ใจกลางเกมมิชันตามแผนงาน |
| 3 | **เฟส 5 — แลกรางวัล + Wishlist** | ใช้ coin หลังมีภารกิจและยอดคงที่ |
| 4 | **เฟส 5b — Commission** | หลังมีกติกาเรื่อง coin/exp ชัด |
| 5 | **เฟส 6 — ทดสอบ + deploy + observability บน K8s** | หลังฟีเจอร์หลักคงที่ |

รายละเอียดรันท้องถิ่น: `docs/LOCAL_DEVELOPMENT.md`
