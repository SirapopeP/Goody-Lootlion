import { Routes } from '@angular/router';

export const MISSIONS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./missions-placeholder.component').then((m) => m.MissionsPlaceholderComponent)
  }
];
