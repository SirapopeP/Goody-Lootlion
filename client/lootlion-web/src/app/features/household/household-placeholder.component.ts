import { Component } from '@angular/core';

@Component({
  selector: 'app-household-placeholder',
  standalone: true,
  template: `<p>โมดูลครอบครัว — <code>GET /api/households/mine</code>, <code>POST /api/households</code></p>`
})
export class HouseholdPlaceholderComponent {}
