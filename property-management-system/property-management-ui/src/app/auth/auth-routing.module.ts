import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '../core/guards/auth.guard';  // ← Import function (corrected path)

const routes: Routes = [
  { path: '', redirectTo: '/auth/login', pathMatch: 'full' },
  { 
    path: 'auth', 
    loadChildren: () => import('./auth.module').then(m => m.AuthModule)
  },
  {
    path: 'tenant',
    loadChildren: () => import('../features/tenant/tenant.module').then(m => m.TenantModule),
    canActivate: [AuthGuard],  // ← Use function directly
    data: { roles: ['Occupant'] }
  },
  {
    path: 'technician',
    loadChildren: () => import('../features/technician/technician.module').then(m => m.TechnicianModule),
    canActivate: [AuthGuard],
    data: { roles: ['Technician'] }
  },
  {
    path: 'manager',
    loadChildren: () => import('../features/manager/manager.module').then(m => m.ManagerModule),
    canActivate: [AuthGuard],
    data: { roles: ['PropertyManager'] }
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }