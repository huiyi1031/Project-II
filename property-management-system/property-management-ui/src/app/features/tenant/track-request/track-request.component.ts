import { Component, OnInit } from '@angular/core';
import { catchError, forkJoin, of } from 'rxjs';
import { MaintenanceRequest, MaintenanceRequestDetail } from '../../../core/models';
import { MaintenanceService } from '../../../core/services/maintenance.service';

interface RequestNotification {
  requestID: number;
  requestNumber: string;
  requestTitle: string;
  status: string;
  message: string;
  reason?: string;
  decidedAt?: string;
  detail?: MaintenanceRequestDetail;
}

@Component({
  selector: 'app-track-request',
  templateUrl: './track-request.component.html',
  standalone: false,
})
export class TrackRequestComponent implements OnInit {
  requests: MaintenanceRequest[] = [];
  filteredRequests: MaintenanceRequest[] = [];
  notifications: RequestNotification[] = [];
  statusFilter = 'All';
  dateFilter = '';
  selected: MaintenanceRequestDetail | null = null;
  loading = false;
  loadingDetail = false;
  loadingNotifications = false;
  errorMsg = '';

  statuses = ['All', 'Pending', 'Approved', 'Rejected', 'Cancelled'];

  constructor(private svc: MaintenanceService) {}

  ngOnInit(): void { this.loadRequests(); }

  loadRequests(): void {
    this.loading = true;
    this.errorMsg = '';

    this.svc.getMyRequests(this.statusFilter).subscribe({
      next: data => {
        this.requests = data;
        this.applyDateFilter();
        this.loading = false;
        this.loadDecisionNotifications();
      },
      error: err => {
        this.loading = false;
        this.requests = [];
        this.filteredRequests = [];
        this.notifications = [];
        this.errorMsg = err.error?.message || 'Failed to load maintenance requests.';
      }
    });
  }

  applyDateFilter(): void {
    this.filteredRequests = this.dateFilter
      ? this.requests.filter(request => (request.submissionDate || '').startsWith(this.dateFilter))
      : [...this.requests];
  }

  selectRequest(request: MaintenanceRequest): void {
    this.loadingDetail = true;
    this.svc.getRequestById(request.requestID).subscribe({
      next: detail => {
        this.selected = detail;
        this.loadingDetail = false;
      },
      error: err => {
        this.loadingDetail = false;
        this.errorMsg = err.error?.message || 'Failed to load request details.';
      }
    });
  }

  openNotification(notification: RequestNotification): void {
    if (notification.detail) {
      this.selected = notification.detail;
      return;
    }

    const request = this.requests.find(item => item.requestID === notification.requestID);
    if (request) this.selectRequest(request);
  }

  trackByNotification(_: number, notification: RequestNotification): number {
    return notification.requestID;
  }

  isFinalStatus(status: string): boolean {
    return status === 'Approved' || status === 'Rejected' || status === 'Cancelled';
  }

  private loadDecisionNotifications(): void {
    this.loadingNotifications = true;

    this.svc.getMyRequests().subscribe({
      next: requests => this.loadDecisionNotificationDetails(requests),
      error: () => {
        this.notifications = [];
        this.loadingNotifications = false;
      }
    });
  }

  private loadDecisionNotificationDetails(requests: MaintenanceRequest[]): void {
    const decisionRequests = requests.filter(request => request.status === 'Approved' || request.status === 'Rejected');

    if (decisionRequests.length === 0) {
      this.notifications = [];
      this.loadingNotifications = false;
      return;
    }

    forkJoin(
      decisionRequests.map(request =>
        this.svc.getRequestById(request.requestID).pipe(catchError(() => of(null)))
      )
    ).subscribe(details => {
      this.notifications = details
        .filter((detail): detail is MaintenanceRequestDetail => detail !== null)
        .map(detail => this.toNotification(detail))
        .sort((a, b) => new Date(b.decidedAt || 0).getTime() - new Date(a.decidedAt || 0).getTime());
      this.loadingNotifications = false;
    });
  }

  private toNotification(detail: MaintenanceRequestDetail): RequestNotification {
    const isRejected = detail.status === 'Rejected';
    return {
      requestID: detail.requestID,
      requestNumber: detail.requestNumber,
      requestTitle: detail.requestTitle,
      status: detail.status,
      message: isRejected ? 'Your request was rejected.' : 'Your request was accepted.',
      reason: isRejected ? detail.rejectionReason || 'No reason provided.' : undefined,
      decidedAt: isRejected ? detail.rejectedAt : detail.approvedAt,
      detail
    };
  }
}

