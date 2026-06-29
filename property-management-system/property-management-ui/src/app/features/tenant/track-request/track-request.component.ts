import { Component, OnInit } from '@angular/core';
import { MaintenanceRequest } from '../../../core/models';
import { MaintenanceService } from '../../../core/services/maintenance.service';

@Component({
  selector: 'app-track-request',
  templateUrl: './track-request.component.html',
  standalone: false,
})
export class TrackRequestComponent implements OnInit {
  requests:     MaintenanceRequest[] = [];
  statusFilter  = 'All';
  dateFilter    = '';
  selected:     MaintenanceRequest | null = null;
  showProgress  = false;
  selectedId    = '';
  selectedTitle = '';
  progressWidth = '0%';

  steps = [
    { label: 'Submitted',  done: true  },
    { label: 'Assigned',   done: true  },
    { label: 'Scheduled',  done: false },
    { label: 'In Progress',done: false },
    { label: 'Completed',  done: false },
  ];

  constructor(private svc: MaintenanceService) {}

  ngOnInit(): void { this.loadRequests(); }

  loadRequests(): void {
    this.svc.getMyRequests(this.statusFilter).subscribe({
      next: (data) => (this.requests = data),
      error: () => {}
    });
  }

  selectRequest(r: MaintenanceRequest): void {
    this.selected     = r;
    this.selectedId   = `REQ-${String(r.requestID).padStart(4, '0')}`;
    this.selectedTitle = r.requestTitle;
    this.showProgress = true;
    this.computeProgress(r.status);
  }

  selectMock(): void {
    this.showProgress = true;
    this.selectedId   = 'REQ-2026-0012';
    this.selectedTitle = 'Aircond leaking';
    this.computeProgress('Assigned');
  }

  private computeProgress(status: string): void {
    const statusMap: Record<string, { width: string; done: boolean[] }> = {
      Pending:    { width: '10%',  done: [true, false, false, false, false] },
      Assigned:   { width: '35%',  done: [true, true,  false, false, false] },
      InProgress: { width: '65%',  done: [true, true,  true,  true,  false] },
      Completed:  { width: '100%', done: [true, true,  true,  true,  true ] },
      Cancelled:  { width: '0%',   done: [false,false, false, false, false] },
    };
    const cfg = statusMap[status] ?? { width: '10%', done: [true, false, false, false, false] };
    this.progressWidth = cfg.width;
    this.steps.forEach((s, i) => (s.done = cfg.done[i]));
  }
}
