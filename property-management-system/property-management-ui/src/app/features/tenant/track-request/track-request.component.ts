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

  // Progress Bar state
  showProgress = false;
  progressWidth = '0%';
  isRejected = false;
  isCancelled = false;
  rejectionReason = '';
  steps = [
    { label: 'Pending',    done: true  },
    { label: 'Approved',   done: false },
    { label: 'Scheduling', done: false },
    { label: 'Scheduled',  done: false },
    { label: 'In Progress',done: false },
    { label: 'Completed',  done: false },
    { label: 'Payment',    done: false },
  ];

  // Cancel modal state
  showCancelModal = false;
  requestToCancel: MaintenanceRequest | null = null;
  isCancelling = false;
  cancelReason = '';

  // Invoice modal state
  showInvoiceModal = false;
  selectedInvoiceRequest: MaintenanceRequest | null = null;

  statuses = ['All', 'Pending', 'Approved', 'Rejected', 'Cancelled', 'Scheduling', 'Scheduled', 'In Progress', 'Completed'];

  constructor(private svc: MaintenanceService) {}

  ngOnInit(): void { this.loadRequests(); }

  loadRequests(): void {
    this.loading = true;
    this.errorMsg = '';

    this.svc.getMyRequests(this.statusFilter).subscribe({
      next: data => {
        this.requests = data.items.map(r => {
          if (r.status === 'Assigned') r.status = 'Approved';
          return r;
        });
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
    if (!this.dateFilter) {
      this.filteredRequests = [...this.requests];
      return;
    }

    const filterDate = new Date(this.dateFilter).toDateString();
    
    this.filteredRequests = this.requests.filter(r => {
      const sDate = new Date(r.submissionDate).toDateString();
      return sDate === filterDate;
    });
  }

  clearFilters(): void {
    this.statusFilter = 'All';
    this.dateFilter = '';
    this.loadRequests();
  }

  selectRequest(request: MaintenanceRequest): void {
    this.loadingDetail = true;
    this.showProgress = true;
    this.isRejected = request.status === 'Rejected';
    this.isCancelled = request.status === 'Cancelled';
    this.rejectionReason = request.rejectionReason || request.cancellationReason || 'No reason provided.';
    this.computeProgress(request.status);

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
      next: data => this.loadDecisionNotificationDetails(data.items),
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
        .map(detail => {
          if (detail.status === 'Assigned') detail.status = 'Approved';
          return this.toNotification(detail);
        })
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

  cancelRequestModal(r: MaintenanceRequest): void {
    this.requestToCancel = r;
    this.showCancelModal = true;
  }

  confirmCancel(): void {
    if (!this.requestToCancel || !this.cancelReason.trim()) return;
    this.isCancelling = true;
    
    const reqId = this.requestToCancel.requestID;
    this.svc.cancelRequest(reqId, this.cancelReason).subscribe({
      next: () => {
        this.loadRequests();
        if (this.selected?.requestID === reqId) {
          this.showProgress = false;
          this.selected = null;
        }
        this.closeCancelModal();
      },
      error: (err) => {
        alert('Failed to cancel the request. It might be already in progress or you lack permission.');
        this.closeCancelModal();
      }
    });
  }

  closeCancelModal(): void {
    this.showCancelModal = false;
    this.requestToCancel = null;
    this.isCancelling = false;
    this.cancelReason = '';
  }

  openInvoiceModal(request: MaintenanceRequest): void {
    this.selectedInvoiceRequest = request;
    this.showInvoiceModal = true;
  }

  closeInvoiceModal(): void {
    this.showInvoiceModal = false;
    this.selectedInvoiceRequest = null;
  }

  getInvoiceDueDate(dateString?: string): Date | null {
    if (!dateString) return null;
    const date = new Date(dateString);
    date.setDate(date.getDate() + 7);
    return date;
  }

  private computeProgress(status: string): void {
    if (status === 'Rejected') {
      this.steps[1].label = 'Rejected';
    } else if (status === 'Cancelled') {
      this.steps[1].label = 'Cancelled';
    } else {
      this.steps[1].label = 'Approved';
    }

    const statusMap: Record<string, { width: string; done: boolean[] }> = {
      Pending:    { width: '10%',  done: [true,  false, false, false, false, false, false] },
      Approved:   { width: '25%',  done: [true,  true,  false, false, false, false, false] },
      Scheduling: { width: '45%',  done: [true,  true,  true,  false, false, false, false] },
      Scheduled:  { width: '65%',  done: [true,  true,  true,  true,  false, false, false] },
      InProgress: { width: '85%',  done: [true,  true,  true,  true,  true,  false, false] },
      Completed:  { width: '100%', done: [true,  true,  true,  true,  true,  true,  false] },
      Rejected:   { width: '25%',  done: [true,  true,  false, false, false, false, false] },
      Cancelled:  { width: '25%',  done: [true,  true,  false, false, false, false, false] },
      Assigned:   { width: '25%',  done: [true,  true,  false, false, false, false, false] }, 
    };
    const cfg = statusMap[status] ?? { width: '10%', done: [true, false, false, false, false, false, false] };
    this.progressWidth = cfg.width;
    this.steps.forEach((s, i) => (s.done = cfg.done[i]));
  }
}

