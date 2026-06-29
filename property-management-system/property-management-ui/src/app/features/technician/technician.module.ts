import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../shared/shared.module';

// Note: ChatComponent and ProfileComponent are declared in SharedModule
import { ChatComponent }    from '../tenant/chat/chat.component';
import { ProfileComponent } from '../tenant/profile/profile.component';

import { TechnicianShellComponent } from './technician-shell/technician-shell.component';
import { TechDashboardComponent }   from './dashboard/dashboard.component';
import { TechWorkOrdersComponent }  from './work-orders/work-orders.component';
import { ExecuteWorkComponent }     from './execute-work/execute-work.component';
import { TechReportComponent }      from './report/report.component';

const routes: Routes = [
  {
    path: '',
    component: TechnicianShellComponent,
    children: [
      { path: '',              redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard',    component: TechDashboardComponent },
      { path: 'work-orders',  component: TechWorkOrdersComponent },
      { path: 'execute-work', component: ExecuteWorkComponent },
      { path: 'chat',         component: ChatComponent },
      { path: 'report',       component: TechReportComponent },
      { path: 'profile',      component: ProfileComponent },
    ]
  }
];

@NgModule({
  declarations: [
    TechnicianShellComponent,
    TechDashboardComponent,
    TechWorkOrdersComponent,
    ExecuteWorkComponent,
    TechReportComponent,
    // ChatComponent and ProfileComponent are in SharedModule
  ],
  imports: [
    SharedModule,
    RouterModule.forChild(routes),
  ],
})
export class TechnicianModule {}