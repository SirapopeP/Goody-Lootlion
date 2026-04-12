import { Component } from '@angular/core';

@Component({
  selector: 'app-auth-placeholder',
  standalone: true,
  template: `<p>โมดูล Auth — ผูกฟอร์มกับ <code>POST /api/auth/login</code> และ <code>register</code></p>`
})
export class AuthPlaceholderComponent {}
