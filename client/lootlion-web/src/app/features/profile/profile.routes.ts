import { Routes } from '@angular/router';

export const PROFILE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./profile-placeholder.component').then((m) => m.ProfilePlaceholderComponent)
  }
];
