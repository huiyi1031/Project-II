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
  requests:    MaintenanceRequest[] = [];
  upcomingAssets: Asset[] = [];

  constructor(
    private mainSvc: MaintenanceService,
    private assetSvc: AssetService,
  ) {}

  ngOnInit(): void {
    this.mainSvc.getAllRequests().subscribe({ next: d => (this.requests = d.slice(0, 5)), error: () => {} });
    // Show assets with upcoming maintenance (no more risk concept — shows all active assets)
    this.assetSvc.getAll({ status: 'Active' }).subscribe({ next: d => (this.upcomingAssets = d.slice(0, 5)), error: () => {} });
  }
}
