import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../shared/shared.module';
import { ChatComponent }    from '../tenant/chat/chat.component';
import { ProfileComponent } from '../tenant/profile/profile.component';

import { TenantShellComponent }     from './tenant-shell/tenant-shell.component';
import { TenantDashboardComponent } from './dashboard/dashboard.component';
import { CreateRequestComponent }   from './create-request/create-request.component';
import { TrackRequestComponent }    from './track-request/track-request.component';
import { MyPropertyComponent }      from './my-property/my-property.component';
import { FamilyComponent }          from './family/family.component';
import { TenantsComponent }         from './tenants/tenants.component';

const routes: Routes = [
  {
    path: '',
    component: TenantShellComponent,
    children: [
      { path: '',               redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard',      component: TenantDashboardComponent },
      { path: 'create-request', component: CreateRequestComponent },
      { path: 'track-request',  component: TrackRequestComponent },
      { path: 'chat',           component: ChatComponent },
      { path: 'profile',        component: ProfileComponent },
      { path: 'my-property',    component: MyPropertyComponent },
      { path: 'family',         component: FamilyComponent },
      { path: 'tenants',        component: TenantsComponent },  // Owner only
    ]
  }
];

@NgModule({
  declarations: [
    TenantShellComponent,
    TenantDashboardComponent,
    CreateRequestComponent,
    TrackRequestComponent,
    MyPropertyComponent,
    FamilyComponent,
    TenantsComponent,
    // ChatComponent and ProfileComponent are declared in SharedModule
  ],
  imports: [
    SharedModule,
    RouterModule.forChild(routes),
  ],
})
export class TenantModule {}