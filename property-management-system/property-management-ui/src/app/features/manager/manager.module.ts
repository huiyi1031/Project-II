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

import { UnitFormComponent }          from './units/unit-form/unit-form.component';
import { UnitDetailComponent }        from './units/unit-detail/unit-detail.component';

import { AssetFormComponent }         from './assets/asset-form/asset-form.component';
import { AssetDetailComponent }       from './assets/asset-detail/asset-detail.component';

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
      { path: 'units/create',component: UnitFormComponent },
      { path: 'units/:id/edit', component: UnitFormComponent },
      { path: 'units/:id',   component: UnitDetailComponent },
      { path: 'assets',      component: AssetsComponent },
      { path: 'assets/create',component: AssetFormComponent },
      { path: 'assets/:id/edit', component: AssetFormComponent },
      { path: 'assets/:id',   component: AssetDetailComponent },
      { path: 'proactive',   component: ProactiveComponent },
      { path: 'chat',        component: ChatComponent },
      { path: 'profile',     component: ProfileComponent },
    ]
  }
];

import { QRCodeModule } from 'angularx-qrcode';

@NgModule({
  declarations: [
    ManagerShellComponent,
    ManagerDashboardComponent,
    StaffComponent,
    OccupantsComponent,
    RequestsComponent,
    ManagerWorkOrdersComponent,
    UnitsComponent,
    UnitFormComponent,
    UnitDetailComponent,
    AssetsComponent,
    AssetFormComponent,
    AssetDetailComponent,
    ProactiveComponent,
    // ChatComponent and ProfileComponent are in SharedModule
  ],
  imports: [
    SharedModule,
    QRCodeModule,
    RouterModule.forChild(routes),
  ],
})
export class ManagerModule {}