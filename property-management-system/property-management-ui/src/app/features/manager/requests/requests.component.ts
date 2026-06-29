import { Component, OnInit } from '@angular/core';
import { MaintenanceRequest } from '../../../core/models';
import { MaintenanceService } from '../../../core/services/maintenance.service';

@Component({
  selector: 'app-requests',
  templateUrl: './requests.component.html',
  standalone: false,
})
export class RequestsComponent implements OnInit {
  requests:     MaintenanceRequest[] = [];
  statusFilter  = 'All';

  constructor(private svc: MaintenanceService) {}
  ngOnInit(): void { this.svc.getAllRequests().subscribe({ next: d => (this.requests = d), error: () => {} }); }
  approve(id: number): void { this.svc.approveRequest(id).subscribe({ next: () => this.ngOnInit(), error: () => {} }); }
  reject(id: number): void { this.svc.rejectRequest(id, 'Rejected by manager').subscribe({ next: () => this.ngOnInit(), error: () => {} }); }
}
