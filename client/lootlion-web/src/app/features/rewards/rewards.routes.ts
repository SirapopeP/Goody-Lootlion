import { Routes } from '@angular/router';

export const REWARDS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./rewards-placeholder.component').then((m) => m.RewardsPlaceholderComponent)
  }
];
