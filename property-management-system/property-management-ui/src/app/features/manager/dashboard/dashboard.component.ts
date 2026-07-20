import { Component, OnInit } from '@angular/core';
import { MaintenanceRequest, Asset } from '../../../core/models';
import { MaintenanceService } from '../../../core/services/maintenance.service';
import { AssetService } from '../../../core/services/asset.service';

@Component({
  selector: 'app-manager-dashboard',
  templateUrl: './dashboard.component.html',
  standalone: false,
})
export class ManagerDashboardComponent implements OnInit {
  requests:       MaintenanceRequest[] = [];
  upcomingAssets: Asset[] = [];

  /* Dynamic KPI counts */
  openCount       = 0;
  inProgressCount = 0;
  completedCount  = 0;
  assetsDueSoon   = 0;

  constructor(
    private mainSvc: MaintenanceService,
    private assetSvc: AssetService,
  ) {}

  ngOnInit(): void {
    /* Fetch all maintenance requests and compute KPI counts */
    this.mainSvc.getAllRequests().subscribe({
      next: (all: MaintenanceRequest[]) => {
        this.requests = all.slice(0, 5);

        this.openCount = all.filter(
          r => r.status === 'Pending' || r.status === 'Open'
        ).length;

        this.inProgressCount = all.filter(
          r => r.status === 'InProgress' || r.status === 'Assigned' || r.status === 'In Progress'
        ).length;

        this.completedCount = all.filter(
          r => r.status === 'Completed'
        ).length;
      },
      error: () => {}
    });

    /* Fetch active assets and count those due within 14 days */
    this.assetSvc.getAll({ status: 'Active' }).subscribe({
      next: (assets: Asset[]) => {
        this.upcomingAssets = assets
          .filter(a => a.nextMaintenanceDueDate)
          .sort((a, b) =>
            new Date(a.nextMaintenanceDueDate!).getTime() - new Date(b.nextMaintenanceDueDate!).getTime()
          )
          .slice(0, 5);

        const now = Date.now();
        const fourteenDays = 14 * 24 * 60 * 60 * 1000;
        this.assetsDueSoon = assets.filter(a => {
          if (!a.nextMaintenanceDueDate) return false;
          const diff = new Date(a.nextMaintenanceDueDate).getTime() - now;
          return diff <= fourteenDays;
        }).length;
      },
      error: () => {}
    });
  }
}
