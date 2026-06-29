import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';

// Routes configuration - maps URLs to what to display
const routes: Routes = [
  // When user goes to root URL (http://localhost:4200), redirect to login
  { path: '', redirectTo: 'auth/login', pathMatch: 'full' },
  
  // When user goes to /auth, load the AuthModule (lazy loading)
  // Lazy loading means the code is only loaded when needed (faster startup)
  { 
    path: 'auth', 
    loadChildren: () => import('./auth/auth.module').then(m => m.AuthModule) 
  },
  
  // Protected routes - require authentication and specific role
  {
    path: 'tenant',
    loadChildren: () => import('./features/tenant/tenant.module').then(m => m.TenantModule),
    canActivate: [AuthGuard],        // ← Only if logged in
    data: { roles: ['Occupant'] }    // ← Only if role is Occupant
  },
  {
    path: 'technician',
    loadChildren: () => import('./features/technician/technician.module').then(m => m.TechnicianModule),
    canActivate: [AuthGuard],
    data: { roles: ['Technician'] }
  },
  {
    path: 'manager',
    loadChildren: () => import('./features/manager/manager.module').then(m => m.ManagerModule),
    canActivate: [AuthGuard],
    data: { roles: ['PropertyManager'] }
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],  // ← Register routes
  exports: [RouterModule]                   // ← Make available to other modules
})
export class AppRoutingModule { }