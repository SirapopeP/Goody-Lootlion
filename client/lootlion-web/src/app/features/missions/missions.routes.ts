import { Routes } from '@angular/router';

export const MISSIONS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./missions-report-page.component').then((m) => m.MissionsReportPageComponent)
  }
];
