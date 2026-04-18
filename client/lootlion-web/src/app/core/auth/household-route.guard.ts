import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthSessionService } from './auth-session.service';
import { parseJwtUserDisplay } from './jwt-payload';

function loginUrlTree(router: Router) {
  const raw = router.routerState.snapshot.url || '/';
  const returnUrl = raw === '' ? '/' : raw.startsWith('/') ? raw : `/${raw}`;
  return router.createUrlTree(['/auth/login'], { queryParams: { returnUrl } });
}

export const rewardsParentCanMatch: CanMatchFn = () => {
  const session = inject(AuthSessionService);
  const router = inject(Router);
  if (!session.isAuthenticated()) {
    return loginUrlTree(router);
  }
  const role = parseJwtUserDisplay(session.token()).householdRole;
  if (role === 'parent') {
    return true;
  }
  if (role === 'child') {
    return router.createUrlTree(['/wishlist']);
  }
  return router.createUrlTree(['/']);
};

export const wishlistChildCanMatch: CanMatchFn = () => {
  const session = inject(AuthSessionService);
  const router = inject(Router);
  if (!session.isAuthenticated()) {
    return loginUrlTree(router);
  }
  const role = parseJwtUserDisplay(session.token()).householdRole;
  if (role === 'child') {
    return true;
  }
  if (role === 'parent') {
    return router.createUrlTree(['/rewards']);
  }
  return router.createUrlTree(['/']);
};
