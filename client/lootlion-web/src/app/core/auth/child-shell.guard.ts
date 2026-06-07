import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthSessionService } from './auth-session.service';
import { parseJwtUserDisplay } from './jwt-payload';

function loginUrlTree(router: Router) {
  const raw = router.routerState.snapshot.url || '/';
  const returnUrl = raw === '' ? '/' : raw.startsWith('/') ? raw : `/${raw}`;
  return router.createUrlTree(['/auth/login'], { queryParams: { returnUrl } });
}

export const childShellCanMatch: CanMatchFn = () => {
  const session = inject(AuthSessionService);
  const router = inject(Router);
  if (!session.isAuthenticated()) {
    return loginUrlTree(router);
  }
  return parseJwtUserDisplay(session.token()).householdRole === 'child';
};

export const parentShellCanMatch: CanMatchFn = () => {
  const session = inject(AuthSessionService);
  const router = inject(Router);
  if (!session.isAuthenticated()) {
    return loginUrlTree(router);
  }
  const role = parseJwtUserDisplay(session.token()).householdRole;
  return role !== 'child';
};

export const missionsParentCanMatch: CanMatchFn = () => {
  const session = inject(AuthSessionService);
  const router = inject(Router);
  if (!session.isAuthenticated()) {
    return loginUrlTree(router);
  }
  const role = parseJwtUserDisplay(session.token()).householdRole;
  if (role === 'parent') {
    return true;
  }
  return router.createUrlTree(['/']);
};
