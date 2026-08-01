import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const requiredRoles = (route.data['roles'] as string[] | undefined) ?? [];

  if (!authService.isAuthenticated()) {
    router.navigate(['/guest/login']);
    return false;
  }

  if (requiredRoles.length && !authService.hasAnyRole(requiredRoles)) {
    router.navigate(['/guest/login']);
    return false;
  }

  return true;
};
