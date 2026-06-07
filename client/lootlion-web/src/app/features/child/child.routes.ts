import { Routes } from '@angular/router';

export const CHILD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./missions/child-missions-page.component').then((m) => m.ChildMissionsPageComponent),
  },
  {
    path: 'rewards',
    loadComponent: () =>
      import('./rewards/child-rewards-page.component').then((m) => m.ChildRewardsPageComponent),
  },
  {
    path: 'family',
    loadComponent: () =>
      import('./family/child-family-page.component').then((m) => m.ChildFamilyPageComponent),
  },
  {
    path: 'settings',
    loadComponent: () =>
      import('./settings/child-settings-page.component').then((m) => m.ChildSettingsPageComponent),
  },
  { path: 'wishlist', redirectTo: 'rewards', pathMatch: 'full' },
  { path: 'profile', redirectTo: 'settings', pathMatch: 'full' },
];
