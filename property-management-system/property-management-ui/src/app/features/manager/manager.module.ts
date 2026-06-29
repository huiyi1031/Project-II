import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../shared/shared.module';

// Note: ChatComponent and ProfileComponent are declared in SharedModule
import { ChatComponent }    from '../tenant/chat/chat.component';
import { ProfileComponent } from '../tenant/profile/profile.component';

import { ManagerShellComponent }      from './manager-shell/manager-shell.component';
import { ManagerDashboardComponent }  from './dashboard/dashboard.component';
import { StaffComponent }             from './staff/staff.component';
import { OccupantsComponent }         from './occupants/occupants.component';
import { RequestsComponent }          from './requests/requests.component';
import { ManagerWorkOrdersComponent } from './work-orders/work-orders.component';
import { UnitsComponent }             from './units/units.component';
import { AssetsComponent }            from './assets/assets.component';
import { ProactiveComponent }         from './proactive/proactive.component';

const routes: Routes = [
  {
    path: '',
    component: ManagerShellComponent,
    children: [
      { path: '',            redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard',   component: ManagerDashboardComponent },
      { path: 'staff',       component: StaffComponent },
      { path: 'occupants',   component: OccupantsComponent },
      { path: 'requests',    component: RequestsComponent },
      { path: 'work-orders', component: ManagerWorkOrdersComponent },
      { path: 'units',       component: UnitsComponent },
      { path: 'assets',      component: AssetsComponent },
      { path: 'proactive',   component: ProactiveComponent },
      { path: 'chat',        component: ChatComponent },
      { path: 'profile',     component: ProfileComponent },
    ]
  }
];

@NgModule({
  declarations: [
    ManagerShellComponent,
    ManagerDashboardComponent,
    StaffComponent,
    OccupantsComponent,
    RequestsComponent,
    ManagerWorkOrdersComponent,
    UnitsComponent,
    AssetsComponent,
    ProactiveComponent,
    // ChatComponent and ProfileComponent are in SharedModule
  ],
  imports: [
    SharedModule,
    RouterModule.forChild(routes),
  ],
})
export class ManagerModule {}