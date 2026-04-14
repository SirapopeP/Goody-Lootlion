import { Component, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { MenuAccessService } from '../../core/auth/menu-access.service';

@Component({
  selector: 'app-household-placeholder',
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: './household-placeholder.component.html',
  styleUrl: './household-placeholder.component.scss',
})
export class HouseholdPlaceholderComponent {
  readonly menu = inject(MenuAccessService);
}
