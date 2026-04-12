# คู่มือแก้ปัญหา EF Core Migration และเครื่องมือ `dotnet ef`

เอกสารนี้สรุปปัญหาที่พบบ่อยตอนสร้าง migration / อัปเดตฐานข้อมูลในโปรเจกต์ Lootlion และวิธีแก้แบบทำเองได้ทีละขั้น  
รายละเอียดการรันโปรเจกต์โดยรวมอยู่ที่ [LOCAL_DEVELOPMENT.md](./LOCAL_DEVELOPMENT.md)

---

## สิ่งที่ควรมีก่อนเริ่ม (ครั้งเดียวหรือเมื่อเครื่องใหม่)

| รายการ | วิธีตรวจ / ติดตั้ง |
|--------|---------------------|
| **.NET SDK 8** | `dotnet --version` |
| **เครื่องมือ EF CLI** | `dotnet tool install --global dotnet-ef` แล้วเปิดเทอร์มินัลใหม่ |
| **อัปเดตเครื่องมือ** (ถ้าเคยติดตั้งแล้ว) | `dotnet tool update --global dotnet-ef` |
| **ตรวจว่าใช้ได้** | `dotnet ef --version` |

---

## ลำดับที่แนะนำ (ทุกครั้งที่ clone ใหม่ หรือหลังลบ `bin` / `obj`)

รันจาก **root ของ repo** (`Goody-Lootlion`):

1. **Restore แพ็กเกจ** (สำคัญ — ถ้าข้ามมักเจอ `NETSDK1004`)

   ```powershell
   dotnet restore
   ```

2. **สร้าง migration** (เฉพาะเมื่อยังไม่มี migration หรือต้องการ migration ชุดใหม่ — ชื่อ `Initial` เป็นตัวอย่าง)

   ```powershell
   dotnet ef migrations add Initial --project src/Lootlion.Infrastructure --startup-project src/Lootlion.Api
   ```

3. **สร้าง/อัปเดต schema ในฐานข้อมูล**

   ```powershell
   dotnet ef database update --project src/Lootlion.Infrastructure --startup-project src/Lootlion.Api
   ```

4. ตรวจว่า **PostgreSQL รันอยู่** และ **connection string** ใน `src/Lootlion.Api/appsettings.json` (หรือ `ConnectionStrings__Default`) ตรงกับ user / password / host / database จริง

---

## ปัญหาและวิธีแก้

### 1) `NETSDK1004` — `project.assets.json` not found / Run a NuGet package restore

**สาเหตุ:** ยังไม่ restore หรือลบโฟลเดอร์ `obj` แล้วยังไม่สร้าง `project.assets.json` ใหม่

**แก้:**

```powershell
dotnet restore
```

หรือ `dotnet build` (จะ restore ให้ด้วย) แล้วค่อยรัน `dotnet ef` อีกครั้ง

---

### 2) `Could not execute` / `dotnet-ef does not exist`

**สาเหตุ:** ยังไม่ติดตั้งเครื่องมือ EF แบบ global

**แก้:**

```powershell
dotnet tool install --global dotnet-ef
```

จากนั้นปิดแล้วเปิดเทอร์มินัลใหม่ หรือตรวจว่า PATH รวมโฟลเดอร์ tools ของ .NET แล้วลอง `dotnet ef --version` อีกครั้ง

---

### 3) Build ล้ม — `SignInManager<>` could not be found (ใน `Lootlion.Infrastructure`)

**สาเหตุ:** คลาสเช่น `SignInManager` อยู่ใน ASP.NET Core shared framework แต่โปรเจกต์ Infrastructure เป็น class library ที่ยังไม่ได้อ้างอิง framework นั้น

**แก้:** ใน `src/Lootlion.Infrastructure/Lootlion.Infrastructure.csproj` ให้มี:

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

จากนั้นรัน `dotnet build` อีกครั้ง

---

### 4) `dotnet ef` รายงานว่า startup project ไม่มี `Microsoft.EntityFrameworkCore.Design`

**สาเหตุ:** เครื่องมือ `dotnet ef` ต้องการให้โปรเจกต์ **startup** (ที่ใช้กับ `--startup-project`) มีแพ็กเกจ Design เพื่อให้ tooling ทำงานได้ถูกต้อง

**แก้:** ใน `src/Lootlion.Api/Lootlion.Api.csproj` ให้มี `PackageReference` ไปที่ `Microsoft.EntityFrameworkCore.Design` เวอร์ชันเดียวกับ EF Core ในโปรเจกต์ (เช่น `8.0.11`) พร้อม `PrivateAssets` แบบเดียวกับที่ใช้ใน Infrastructure ได้

จากนั้นรัน `dotnet restore` แล้วลอง `dotnet ef` อีกครั้ง

---

### 5) ฐานข้อมูลเชื่อมไม่ติด / migration รันไม่ขึ้น

**ตรวจทีละข้อ:**

- คอนเทนเนอร์ Postgres รันแล้วหรือยัง (`docker compose up -d postgres` เป็นต้น)
- Connection string ตรงกับ host (เช่น `localhost`), พอร์ต (`5432`), user, password, database name
- ไฟร์วอลล์หรือพอร์ตไม่ถูกบล็อก

---

## สรุปคำสั่งสั้น ๆ

| งาน | คำสั่ง |
|-----|--------|
| Restore | `dotnet restore` |
| สร้าง migration ชื่อ X | `dotnet ef migrations add X --project src/Lootlion.Infrastructure --startup-project src/Lootlion.Api` |
| อัปเดต DB ตาม migration ล่าสุด | `dotnet ef database update --project src/Lootlion.Infrastructure --startup-project src/Lootlion.Api` |
| ย้อน migration ล่าสุด (ถ้าต้องการ) | `dotnet ef migrations remove --project src/Lootlion.Infrastructure --startup-project src/Lootlion.Api` |

---

## เอกสารที่เกี่ยวข้อง

- [LOCAL_DEVELOPMENT.md](./LOCAL_DEVELOPMENT.md) — ลำดับรัน API, Angular, Docker, และ connection string
