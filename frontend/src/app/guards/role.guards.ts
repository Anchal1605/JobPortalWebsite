import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Auth } from '../services/auth';

/** Any logged-in user (candidate or recruiter). */
export const loggedInGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  const router = inject(Router);
  if (!auth.getToken()) {
    void router.navigateByUrl('/auth/login');
    return false;
  }
  return true;
};

/** Logged-in recruiters only; others sent home or to login. */
export const recruiterOnlyGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  const router = inject(Router);
  if (!auth.getToken()) {
    void router.navigateByUrl('/auth/login');
    return false;
  }
  if (auth.sessionRoleId() !== auth.recruiterRoleId) {
    void router.navigateByUrl('/');
    return false;
  }
  return true;
};

/** Logged-in candidates only; recruiters sent to their jobs list. */
export const candidateOnlyGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  const router = inject(Router);
  if (!auth.getToken()) {
    void router.navigateByUrl('/auth/login');
    return false;
  }
  if (auth.sessionRoleId() !== auth.candidateRoleId) {
    const dest =
      auth.sessionRoleId() === auth.recruiterRoleId ? '/recruiter/jobs' : '/';
    void router.navigateByUrl(dest);
    return false;
  }
  return true;
};
