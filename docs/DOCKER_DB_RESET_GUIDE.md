# คู่มือรีเซ็ต Docker และเริ่มฐานข้อมูลใหม่ (Development)

เอกสารนี้ใช้สำหรับกรณีต้องการ "ล้างใหม่ทั้งหมด" ฝั่ง Docker แล้วเริ่มฐานข้อมูลจากศูนย์อีกครั้งในเครื่อง dev

> ใช้เฉพาะเครื่องพัฒนา (local dev) เท่านั้น เพราะขั้นตอนนี้ลบข้อมูลถาวรใน volume

---

## เป้าหมายของเอกสารนี้

- ล้าง `container` / `volume` / `image` ที่เกี่ยวข้อง
- เริ่ม PostgreSQL ใหม่แบบสะอาด
- รัน EF migration เพื่อสร้าง schema กลับมา
- ตรวจสอบว่า API และฐานข้อมูลพร้อมใช้งาน

---

## สิ่งที่ควรรู้ก่อนเริ่ม

- ทำงานจาก root ของ repo: `Goody-Lootlion`
- คำสั่งตัวอย่างใช้ PowerShell
- ถ้ามีข้อมูลทดสอบที่ต้องเก็บ ให้สำรองก่อน (เช่น `pg_dump`)
- ปิด API ที่กำลังรันอยู่ก่อน (`Ctrl+C`) เพื่อหลีกเลี่ยงไฟล์ lock

---

## ระดับการล้างที่เลือกได้

| ระดับ | ลบอะไร | เหมาะกับกรณี |
|------|--------|--------------|
| A) รีสตาร์ทอย่างเดียว | restart container | DB ค้าง/timeout ชั่วคราว |
| B) ล้างข้อมูล DB | down + remove volume | ต้องการฐานข้อมูลว่าง แต่ไม่จำเป็นต้องล้าง image |
| C) ล้างหนัก | remove container + volume + image | สงสัย image/cache เสียหาย หรืออยาก clean จริง |

เอกสารนี้จะลงรายละเอียด **ระดับ B และ C** เป็นหลัก

---

## ขั้นตอนแบบ B: ล้างข้อมูล DB แล้วสร้างใหม่ (แนะนำก่อน)

### 1) หยุดและลบ service พร้อม volume

```powershell
docker compose down -v
```

ผลลัพธ์ที่คาดหวัง:
- container ของ compose นี้ถูกหยุดและลบ
- volume ที่ผูกกับ compose นี้ถูกลบ (ข้อมูล DB เก่าหาย)

### 2) สตาร์ต PostgreSQL ใหม่

```powershell
docker compose up -d postgres
```

### 3) เช็กสถานะ container

```powershell
docker compose ps
```

ควรเห็น `postgres` เป็นสถานะ `up ...`

### 4) สร้าง schema ใหม่ด้วย EF migration

```powershell
dotnet ef database update --project src/Lootlion.Infrastructure --startup-project src/Lootlion.Api
```

### 5) รัน API และทดสอบ

```powershell
dotnet run --project src/Lootlion.Api
```

ตรวจที่:
- Swagger: `http://localhost:5088/swagger`
- ลอง flow register/login สั้น ๆ

---

## ขั้นตอนแบบ C: ล้าง container + volume + image แล้วเริ่มใหม่ทั้งหมด

ใช้เมื่อแบบ B แล้วยังมีปัญหาเดิม

### 1) ปิด compose และลบ volume

```powershell
docker compose down -v
```

### 2) ลบ image ที่เกี่ยวข้องกับโปรเจกต์

ดูรายการ image ก่อน:

```powershell
docker images
```

ลบ image เป้าหมาย (ตัวอย่าง postgres):

```powershell
docker rmi postgres:16
```

> หากมีหลาย tag/หลาย image ให้ลบเฉพาะที่เกี่ยวข้องกับงานนี้ก่อน

### 3) (ทางเลือก) ล้างของไม่ใช้งานทั้งหมด

```powershell
docker system prune -a --volumes
```

คำเตือน:
- คำสั่งนี้ลบ resources ที่ไม่ถูกใช้งาน "ทั้งเครื่อง"
- เหมาะเมื่อเข้าใจผลกระทบแล้วเท่านั้น

### 4) ดึง image และสตาร์ต DB ใหม่

```powershell
docker compose up -d postgres
```

### 5) รอ DB พร้อมรับ connection

```powershell
docker compose logs -f postgres
```

มองหาข้อความลักษณะ:
- `database system is ready to accept connections`

### 6) รัน EF migration

```powershell
dotnet ef database update --project src/Lootlion.Infrastructure --startup-project src/Lootlion.Api
```

---

## ชุดคำสั่งสั้น (copy ได้ทันที)

```powershell
# จาก root repo
docker compose down -v
docker compose up -d postgres
docker compose ps
dotnet ef database update --project src/Lootlion.Infrastructure --startup-project src/Lootlion.Api
dotnet run --project src/Lootlion.Api
```

---

## ตรวจสอบหลังรีเซ็ต

- `docker compose ps` เห็น `postgres` เป็น `running`
- `dotnet ef database update ...` จบโดยไม่มี error
- API เปิดได้ที่ `http://localhost:5088/swagger`
- หน้า Angular login/register ใช้งานได้ปกติ

---

## ปัญหาที่พบบ่อย

- `port is already allocated`
  - มี postgres ตัวอื่นใช้พอร์ตอยู่ ให้หยุดตัวที่ชนพอร์ตก่อน
- `database update` ไม่ผ่านเพราะต่อ DB ไม่ได้
  - ตรวจ `ConnectionStrings__Default` หรือ `appsettings.json` ให้ตรงกับค่าจาก docker compose
- `MSB3027` หรือไฟล์ `Lootlion.Api.exe` โดน lock
  - ปิด process API เดิมก่อน แล้วค่อย build/run ใหม่

---

## หมายเหตุเรื่องข้อมูล

- หลัง `down -v` ข้อมูลใน DB เดิมจะหาย
- หลังรีเซ็ต ผู้ใช้ทดสอบ/ข้อมูล seed เดิม (ถ้าไม่ได้ seed อัตโนมัติ) ต้องสร้างใหม่
- ถ้าเพิ่งเพิ่ม migration ใหม่ อย่าลืม commit ไฟล์ migration ให้ครบ
