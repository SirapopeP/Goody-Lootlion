import { Routes } from '@angular/router';

export const HOUSEHOLD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./household-placeholder.component').then((m) => m.HouseholdPlaceholderComponent)
  }
];
