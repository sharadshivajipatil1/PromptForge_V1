import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const role = route.data['role'] as string | undefined;

  if (!role || authService.hasAnyRole([role])) {
    return true;
  }

  router.navigate(['/guest/login']);
  return false;
};
