# แผน Navigation และมุมมอง UI (Lootlion)

เอกสารนี้จับคู่ **แผนงาน Figma / mockup** กับโครงสร้าง Angular ปัจจุบัน

อ้างอิงเช็คลิสงาน: [PHASES_CHECKLIST.md](./PHASES_CHECKLIST.md)

---

## สรุปแนวทาง

| บทบาท | โฟกัส UI | Navigation หลัก |
|--------|-----------|-----------------|
| **Parent** | Quest Board — จัดการ **Template** (panel ซ้าย), อนุมัติ (panel กลาง) | Top nav + sidebar |
| **Child** | รับจากบอร์ด / ส่งงาน / แลกรางวัล | Panel กลางบนหน้าแรก (desktop) — **bottom nav ยังไม่ implement** |

โมเดลข้อมูล: **MissionTemplate** (แม่แบบ) + **MissionInstance** (รอบงาน)

---

## Parent — Quest Board หน้าแรก (`/`)

### โครงสร้าง (implement แล้ว)

| Panel | Component | API |
|-------|-----------|-----|
| ซ้าย — Template | `HomeMissionPanelComponent` | `GET/POST /api/Missions/templates/...` |
| กลาง — Instance | `HomeMissionCenterComponent` | `board`, `mine`, `pending`, `instances/...` |

### Panel ซ้าย (Parent)

- สร้าง template: **BoardClaim** (ไม่บังคับ assign) หรือ **DirectAssign**
- Recurrence: None / Daily / Weekly / Monthly / IntervalDays
- ลบ template: `POST templates/{id}/cancel`
- เปิดรอบใหม่หลัง reject: `POST templates/{id}/spawn` เมื่อ `canSpawnNextRound`

### Panel กลาง

| แท็บ | ผู้ใช้ | การทำงาน |
|------|--------|----------|
| Board | Child | `claim` |
| My missions | ทุกคน | `submit` (Active) |
| Pending | Parent | `approve` / `reject` |
| Done | ทุกคน | รายการ Approved |

---

## Child — มุมมอง Mobile (backlog)

Bottom nav **MISSION | REWARD | FAMILY | SETTING** — ยังไม่แยก layout

บน desktop ปัจจุบัน child ใช้ `HomeMissionCenterComponent` แท็บ **Board** + **Mine** บนหน้าแรก

---

## กติกาธุรกิจ (ยืนยันแล้ว)

- **Rejected + recurring:** ไม่ spawn อัตโนมัติ — Parent กด **เปิดรอบใหม่**
- **Approved + recurring:** spawn รอบถัดไปอัตโนมัติ
- **Timezone:** `Household.TimeZoneId` (IANA) สำหรับ `PeriodKey`
- **BoardClaim:** 1 instance ต่อ `(TemplateId, PeriodKey)`

---

## ลำดับถัดไป

1. **4b** — Wallet ใน sidebar + Rank
2. **4c-child** — Bottom nav ตาม mockup mobile
3. **5** — Rewards / Wishlist UI
