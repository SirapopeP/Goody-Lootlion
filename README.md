# Lootlion

แอปตั้งเป้าหมายและภารกิจในครอบครัว — **Angular (PWA)** + **ASP.NET Core 8** + **PostgreSQL**

## โครงสร้างโปรเจกต์

| โฟลเดอร์ | คำอธิบาย |
|-----------|-----------|
| [src/Lootlion.Domain](src/Lootlion.Domain) | เอนทิตีและ enum |
| [src/Lootlion.Application](src/Lootlion.Application) | บริการ use case, DTO, `ILootlionDbContext` |
| [src/Lootlion.Infrastructure](src/Lootlion.Infrastructure) | EF Core, Identity, JWT |
| [src/Lootlion.Api](src/Lootlion.Api) | Web API, Swagger, JWT |
| [client/lootlion-web](client/lootlion-web) | Angular 19 standalone + PWA shell |
| [docs/LOCAL_DEVELOPMENT.md](docs/LOCAL_DEVELOPMENT.md) | รันและทดสอบ API + Angular ในเครื่อง |
| [docs/PHASES_CHECKLIST.md](docs/PHASES_CHECKLIST.md) | เช็คลิสงานแบ่งเฟสสำหรับสั่งงานต่อ |
| [docs/CLIENT_DECISION.md](docs/CLIENT_DECISION.md) | การตัดสินใจ PWA ก่อน Capacitor |

## ความต้องการของระบบ

- .NET SDK 8
- Node.js 20+ (สำหรับ Angular)
- PostgreSQL 14+ (หรือใช้ Docker)

## ฐานข้อมูลและ migration

1. ปรับ connection string ใน `src/Lootlion.Api/appsettings.json` หรือใช้ environment variable `ConnectionStrings__Default`
2. ติดตั้งเครื่องมือ EF (ครั้งเดียว):  
   `dotnet tool install --global dotnet-ef`
3. สร้าง migration ครั้งแรก:  
   `dotnet ef migrations add Initial --project src/Lootlion.Infrastructure --startup-project src/Lootlion.Api`
4. อัปเดตฐานข้อมูล:  
   `dotnet ef database update --project src/Lootlion.Infrastructure --startup-project src/Lootlion.Api`

## รัน API

```powershell
dotnet run --project src/Lootlion.Api
```

เปิด Swagger (โหมด Development): `http://localhost:5088/swagger`

**JWT**: ตั้งค่า `Jwt:SigningKey` ให้ยาวและเก็บเป็นความลับใน production

## รัน Angular

```powershell
cd client/lootlion-web
npm install
npm start
```

สร้าง client จาก OpenAPI — ต้องให้ **API รันที่พอร์ต 5088** และมี **Docker** (สคริปต์เรียก OpenAPI Generator ผ่าน Docker):

```powershell
cd client/lootlion-web
npm run generate:api
```

Base URL ของ API ฝั่ง Angular ตั้งใน `client/lootlion-web/src/environments/environment.ts` (`apiBaseUrl`)

## Docker

```powershell
docker compose up --build
```

API จะฟังที่ `http://localhost:5088` (แมปจากคอนเทนเนอร์พอร์ต 8080) — หลังขึ้นคอนเทนเนอร์ ต้องรัน **EF database update** จากเครื่อง host หรือเพิ่มขั้นตอน migrate ใน entrypoint ตามนโยบายทีม

## หมายเหตุด้านความปลอดัย

- เปลี่ยน `Jwt:SigningKey` และรหัสผ่านฐานข้อมูลก่อน deploy จริง
- ข้อมูลเด็ก: พิจารณา consent และสิทธิ์ผู้ปกครองตามกฎหมายที่เกี่ยวข้อง
