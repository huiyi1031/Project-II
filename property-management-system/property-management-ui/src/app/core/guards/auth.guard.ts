import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    // STEP 1: Check if user is logged in
    if (!this.authService.isLoggedIn()) {
      // Not logged in -> redirect to login
      this.router.navigate(['/auth/login']);
      return false;
    }

    // STEP 2: Check if route requires a specific role
    const requiredRoles = route.data['roles'];
    if (requiredRoles && !this.authService.hasRole(requiredRoles)) {
      // User doesn't have the required role -> redirect to login
      this.router.navigate(['/auth/login']);
      return false;
    }

    // User is logged in AND has the required role -> allow access
    return true;
  }
}