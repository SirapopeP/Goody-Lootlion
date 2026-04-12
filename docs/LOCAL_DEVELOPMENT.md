# คู่มือรันและทดสอบในเครื่อง (Lootlion)

เอกสารนี้สรุปลำดับการเริ่ม **หลังบ้าน (ASP.NET Core API)** และ **หน้าบ้าน (Angular)** เพื่อให้กลับมาอ่านแล้วทำตามได้โดยไม่ต้องจำทุกคำสั่ง

---

## สิ่งที่ต้องติดตั้งก่อน (ครั้งเดียว)

| เครื่องมือ | ใช้ทำอะไร |
|------------|------------|
| **.NET SDK 8** | build / run API, `dotnet ef` |
| **Node.js 20+** และ **npm** | ติดตั้งแพ็กเกจและรัน Angular |
| **PostgreSQL 14+** หรือ **Docker** | ฐานข้อมูลของ API |

ทางเลือกสำหรับเครื่องมือ EF (ใช้สั่ง migration):

```powershell
dotnet tool install --global dotnet-ef
```

---

## พอร์ตที่ควรจำ

| บริการ | URL ตัวอย่าง | หมายเหตุ |
|--------|----------------|----------|
| API | `http://localhost:5088` | ตั้งใน `src/Lootlion.Api/Properties/launchSettings.json` |
| Swagger (โหมด Development) | `http://localhost:5088/swagger` | ใช้ทดสอบ endpoint และ JWT |
| Angular (`ng serve`) | `http://localhost:4200` | พอร์ตเริ่มต้นของ Angular CLI |

ในโหมด **Development** API อนุญาต **CORS** จาก `http://localhost:4200` เพื่อให้เบราว์เซอร์เรียก API ข้ามพอร์ตได้

---

## ลำดับแนะนำ (ครั้งแรกหรือหลัง clone)

### 1) ให้ PostgreSQL พร้อม

- เปิดบริการ PostgreSQL บนเครื่อง **หรือ**
- รันเฉพาะ DB ด้วย Docker จาก root โปรเจกต์:

  ```powershell
  docker compose up -d postgres
  ```

- ปรับ connection string ให้ตรงกับ user/password/database ของคุณใน  
  `src/Lootlion.Api/appsettings.json`  
  (หรือใช้ environment variable `ConnectionStrings__Default`)

### 2) สร้าง / อัปเดต schema ฐานข้อมูล

รันจาก **root ของ repo** (`Goody-Lootlion`):

```powershell
dotnet ef migrations add Initial --project src/Lootlion.Infrastructure --startup-project src/Lootlion.Api
```

ถ้ามี migration อยู่แล้ว ให้ข้ามคำสั่ง `migrations add` แล้วใช้แค่:

```powershell
dotnet ef database update --project src/Lootlion.Infrastructure --startup-project src/Lootlion.Api
```

คำสั่งนี้สร้างตาราง (รวม Identity + ตารางโดเมน) ในฐานข้อมูลที่ connection string ชี้ไป

### 3) รันหลังบ้าน (API)

จาก root โปรเจกต์:

```powershell
dotnet run --project src/Lootlion.Api
```

- เปิดเบราว์เซอร์ไปที่ **Swagger**: `http://localhost:5088/swagger`
- ทดสอบเริ่มต้น: ใช้ `POST /api/auth/register` แล้ว `POST /api/auth/login` เพื่อรับ JWT  
  จากนั้นใน Swagger กด **Authorize** แล้วใส่ `Bearer <token>` สำหรับ endpoint ที่ต้องล็อกอิน

### 4) รันหน้าบ้าน (Angular)

เปิดเทอร์มินัล **อีกหน้าต่างหนึ่ง** (API ยังรันค้างไว้):

```powershell
cd client/lootlion-web
npm install
npm start
```

- เปิด `http://localhost:4200` เพื่อดู UI  
- ตอนนี้หน้าจอหลายส่วนอาจยังเป็น placeholder — การทดสอบ API แบบเต็มทำที่ Swagger ได้ก่อน

### 5) สร้าง TypeScript client จาก OpenAPI (เฟส 1)

1. ให้ **API รันที่ `http://localhost:5088`** (Swagger: `/swagger/v1/swagger.json`)
2. ต้องมี **Docker** (สคริปต์ใช้ image `openapitools/openapi-generator-cli` — ไม่ต้องติดตั้ง Java บนเครื่อง)
3. **เข้าโฟลเดอร์ Angular ก่อน** (ถ้ารันจาก root ของ repo จะ error ว่าไม่มี `package.json`):

```powershell
cd client/lootlion-web
npm run generate:api
```

ผลลัพธ์อยู่ที่ `src/app/api/generated`  
สเปกดึงจาก `http://host.docker.internal:5088/...` (มองจากคอนเทนเนอร์ generator เข้าถึง API บนเครื่อง host)

- เปลี่ยน URL สเปกชั่วคราว: ตั้ง `OPENAPI_SPEC_URL` แล้วรันคำสั่งเดิม  
- **Base URL ของ client** ตั้งใน `src/environments/environment.ts` (`apiBaseUrl`) — production ใช้ `environment.prod.ts` (แทนที่ตอน `ng build --configuration production`)

**Auth (เฟส 2):** หน้า `/auth/login`, `/auth/register` — เก็บ **access token** + **refresh token** ใน `sessionStorage` (คีย์ `lootlion.accessToken`, `lootlion.refreshToken`) แล้ว interceptor แนบ `Authorization` ให้คำขอไป API (ยกเว้น login/register/refresh) — ถ้าได้ `401` จะลอง `POST /api/Auth/refresh` แล้วส่งคำขอซ้ำหนึ่งครั้ง

**เช็คว่ามี token หรือไม่ (Chrome DevTools):** เปิดแท็บ **Application** → **Session Storage** → เลือก origin `http://localhost:4200` → ดูคีย์ด้านบน หรือในแท็บ **Network** เลือกคำขอ **login** (POST) แล้วดู **Response** ว่ามี `accessToken` / `refreshToken`

**ส่งออกไฟล์ OpenAPI แบบ manual:** เปิด Swagger UI แล้วดาวน์โหลด JSON หรือเรียก  
`http://localhost:5088/swagger/v1/swagger.json`

---

## สรุปภาพรวม “สองเทอร์มินัล”

```text
[เทอร์มินัล A]  dotnet run --project src/Lootlion.Api     →  :5088 /swagger
[เทอร์มินัล B]  cd client/lootlion-web && npm start        →  :4200
```

---

## เมื่อมีปัญหา

- **API ขึ้น error เรื่อง connection string / PostgreSQL**  
  ตรวจว่า Postgres รันอยู่, user/password/database ตรงกับ `appsettings.json`, และพอร์ต (ค่าเริ่มต้น `5432`)

- **พอร์ต 5088 หรือ 4200 ถูกใช้แล้ว**  
  ปิดโปรเซสเดิม หรือเปลี่ยนพอร์ตใน `launchSettings.json` / สั่ง `ng serve --port 4300` เป็นต้น (ถ้าเปลี่ยนพอร์ต Angular ต้องอัปเดต CORS ใน API ให้ตรงกัน)

- **เรียก API จาก Angular แล้วโดน CORS**  
  ตรวจว่า API รันด้วย `ASPNETCORE_ENVIRONMENT=Development` และ origin เป็น `http://localhost:4200` ตามที่ตั้งไว้

- **`npm run generate:api` ขึ้น `ENOENT` / หา `package.json` ไม่เจอ**  
  ต้องรันจากโฟลเดอร์ `client/lootlion-web` (มี `package.json` ของ Angular) ไม่ใช่แค่ root ของ repo `Goody-Lootlion`

- **`dotnet build` / `dotnet run` ขึ้น `MSB3027` / `MSB3021` — ล็อกไฟล์ `Lootlion.Api.exe`**  
  มักเกิดเพราะ **ยังมีโปรเซส API รันอยู่** (เทอร์มินัลอื่น, หรือรัน `dotnet run` ค้างไว้) — ให้หยุดก่อน: ไปที่เทอร์มินัลที่รัน API แล้วกด **Ctrl+C** หรือปิดโปรเซส `Lootlion.Api` ใน Task Manager แล้วค่อย build/run ใหม่

---

## เอกสารอื่นในโฟลเดอร์ `docs`

- [PHASES_CHECKLIST.md](./PHASES_CHECKLIST.md) — แบ่งงานเป็นฟีเจอร์/เฟส
- [CLIENT_DECISION.md](./CLIENT_DECISION.md) — ทำไมเริ่มจาก PWA ก่อน Capacitor
