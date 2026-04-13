import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  {
    path: '',
    loadComponent: () =>
      import('./layout/dashboard-layout.component').then((m) => m.DashboardLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () => import('./features/home/home.component').then((m) => m.HomeComponent),
      },
      {
        path: 'households',
        loadChildren: () => import('./features/household/household.routes').then((m) => m.HOUSEHOLD_ROUTES),
      },
      {
        path: 'missions',
        loadChildren: () => import('./features/missions/missions.routes').then((m) => m.MISSIONS_ROUTES),
      },
      {
        path: 'rewards',
        loadChildren: () => import('./features/rewards/rewards.routes').then((m) => m.REWARDS_ROUTES),
      },
      {
        path: 'wishlist',
        loadChildren: () => import('./features/wishlist/wishlist.routes').then((m) => m.WISHLIST_ROUTES),
      },
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.routes').then((m) => m.PROFILE_ROUTES),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
