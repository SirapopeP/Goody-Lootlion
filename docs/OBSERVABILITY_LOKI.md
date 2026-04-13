# Observability: Loki + Grafana + Prometheus

แบ็กเอนด์ `Lootlion.Api` ตั้งค่าให้เหมาะกับสแตกนี้แล้ว:

## Log → Loki (ผ่าน Promtail หรือ Grafana Agent)

- **Serilog** เขียน log เป็น **NDJSON (RenderedCompactJsonFormatter)** ไปที่ **stdout** — บรรทัดละหนึ่ง JSON ง่ายต่อการ parse
- มี property `Application` = `Lootlion.Api` และ Serilog request logging (HTTP method/path/status/elapsed)
- ใน Kubernetes ให้ **Promtail** (หรือ Grafana Alloy / Fluent Bit) เก็บ log จาก container stdout แล้วส่งเข้า **Loki**
- ใน Grafana เพิ่ม **Data source: Loki** แล้วใช้ LogQL ค้นหา เช่น `{namespace="prod"} |= "Lootlion"` หรือ parse JSON field ตามที่ Serilog ส่งออก

## Metrics → Prometheus → Grafana

- Endpoint **`/metrics`** เปิดโดย **prometheus-net** (`MapMetrics()`)
- **HTTP metrics** จาก middleware `UseHttpMetrics()` (เช่น latency histogram ตาม route)
- ใน Prometheus ตั้ง **scrape** ไปที่ Pod/Service (เช่น `http://lootlion-api:8080/metrics`)
- ใน Grafana เพิ่ม **Data source: Prometheus** แล้วสร้าง dashboard หรือใช้แดชบอร์ดสำเร็จรูปสำหรับ ASP.NET / HTTP

## ความปลอดภัย

- `/metrics` ไม่ควรเปิดสู่สาธารณะ: จำกัดด้วย NetworkPolicy, แยก port ภายใน, หรือให้ Prometheus scrape จากภายใน cluster เท่านั้น
- ไม่ log ข้อมูลลับ (รหัสผ่าน, token เต็ม)

## ตัวอย่าง ServiceMonitor (แนวคิด)

ถ้าใช้ Prometheus Operator ใน Kubernetes ให้สร้าง `ServiceMonitor` ชี้ไปที่ Service ของ API และ path `/metrics` (ปรับ port ให้ตรงกับที่ deploy)
