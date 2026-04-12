import { Component } from '@angular/core';

@Component({
  selector: 'app-home',
  standalone: true,
  template: `
    <h1>Lootlion</h1>
    <p>แอปตั้งเป้าหมายและภารกิจในครอบครัว — เชื่อมต่อ API ที่รัน <code>/swagger</code> แล้วพัฒนาหน้าจอต่อได้จากเช็คลิสใน <code>docs/PHASES_CHECKLIST.md</code></p>
  `
})
export class HomeComponent {}
