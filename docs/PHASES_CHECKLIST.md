# เช็คลิสงานแบ่งเฟส (Lootlion)

ใช้รายการนี้สั่งงานเพิ่มเติมทีละเฟสได้ — ติ๊กเมื่อเสร็จ

## เฟส 0 — สภาพแวดล้อม

- [x] ติดตั้ง .NET SDK 8 และ Node.js 20+
- [x] สร้างฐานข้อมูล PostgreSQL (หรือใช้ `docker compose up postgres`)
- [x] รัน migration: `dotnet ef database update --project src/Lootlion.Infrastructure --startup-project src/Lootlion.Api`
- [x] รัน API: `dotnet run --project src/Lootlion.Api` แล้วเปิด `/swagger`
- [x] รัน Angular: `cd client/lootlion-web && npm install && npm start`

## เฟส 1 — สัญญา API กับ Client

- [x] ส่งออก OpenAPI จาก Swagger ขณะรัน API
- [x] รัน `npm run generate:api` ใน `client/lootlion-web` (ปรับ URL ให้ตรงพอร์ต)
- [x] ตั้งค่า `HttpClient` base URL (environment) ชี้ API

## เฟส 2 — Auth บน Angular

- [x] หน้า login / register เรียก `POST /api/auth/login`, `register`
- [x] เก็บ JWT + refresh token (`sessionStorage` + `AuthSessionService`) — รีเฟรชแท็บแล้วยังล็อกอิน; access หมดอายุแล้ว interceptor ลอง `POST /api/Auth/refresh`
- [x] HTTP interceptor แนบ `Authorization: Bearer` และจัดการ refresh เมื่อได้ 401

## เฟส 3 — ครอบครัว

- [ ] สร้าง / แสดงรายการครอบครัว (`/api/households`)
- [ ] เชิญสมาชิกด้วย `MemberUserId` (หรือเพิ่ม endpoint ค้นหาจากอีเมลภายหลัง)

## เฟส 4 — ภารกิจ + กระเป๋า

- [ ] UI สร้างภารกิจ, ส่ง, อนุมัติ/ปฏิเสธ
- [ ] แสดงยอด coin/exp และ ledger

## เฟส 5 — รางวัล + Wishlist

- [ ] Catalog รางวัลและแลก coin
- [ ] Wishlist → อนุมัติ → ปรากฏใน catalog

## เฟส 6 — คุณภาพและการ deploy

- [ ] เพิ่ม E2E / integration tests ตามความจำเป็น
- [ ] ตั้งค่า JWT signing key / connection string ใน secret จริง
- [ ] Deploy API + static frontend (หรือรวม reverse proxy)
- [ ] (ถ้าต้องการ) Capacitor / แพ็กเกจสโตร์
