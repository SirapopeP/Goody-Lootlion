import { Routes } from '@angular/router';

export const REWARDS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./rewards-page.component').then((m) => m.RewardsPageComponent)
  }
];
