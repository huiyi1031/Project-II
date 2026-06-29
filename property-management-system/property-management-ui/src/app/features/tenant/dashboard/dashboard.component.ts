import { Component, OnInit } from '@angular/core';
import { MaintenanceRequest } from '../../../core/models';
import { MaintenanceService } from '../../../core/services/maintenance.service';

@Component({
  selector: 'app-tenant-dashboard',
  templateUrl: './dashboard.component.html',
  standalone: false,
})
export class TenantDashboardComponent implements OnInit {
  recentCases: MaintenanceRequest[] = [];
  stats = { openRequests: 5, inProgress: 2, completed: 9, avgResponse: '2.1h' };

  constructor(private maintenanceSvc: MaintenanceService) {}

  ngOnInit(): void {
    this.maintenanceSvc.getMyRequests().subscribe({
      next: (data) => {
        this.recentCases = data.slice(0, 5);
        this.stats.openRequests = data.filter(r => r.status === 'Pending').length;
        this.stats.inProgress   = data.filter(r => r.status === 'InProgress').length;
        this.stats.completed    = data.filter(r => r.status === 'Completed').length;
      },
      error: () => { /* use mock fallback shown in template */ }
    });
  }
}
