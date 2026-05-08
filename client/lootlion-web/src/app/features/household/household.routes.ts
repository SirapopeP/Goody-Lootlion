import { Routes } from '@angular/router';

export const HOUSEHOLD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./households-page.component').then((m) => m.HouseholdsPageComponent),
  },
];
