import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import {
  MaintenanceRequest,
  MaintenanceRequestDetail,
  MaintenanceRequestFilter,
  MaintenanceRequester,
} from '../../../core/models';
import { MaintenanceService } from '../../../core/services/maintenance.service';

@Component({
  selector: 'app-requests',
  templateUrl: './requests.component.html',
  styleUrls: ['./requests.component.css'],
  standalone: false,
})
export class RequestsComponent implements OnInit {
  requests: MaintenanceRequest[] = [];
  requesters: MaintenanceRequester[] = [];
  selected: MaintenanceRequestDetail | null = null;
  @ViewChild('requestDetailSection') private requestDetailSection?: ElementRef<HTMLElement>;
  @ViewChild('requestFormSection') private requestFormSection?: ElementRef<HTMLElement>;
  @ViewChild('requestActionSection') private requestActionSection?: ElementRef<HTMLElement>;
  form!: FormGroup;

  statuses = ['All', 'Pending', 'Approved', 'Rejected', 'Cancelled'];
  priorities = ['All', 'Low', 'Medium', 'High'];
  issueTypes = ['HVAC', 'Plumbing', 'Electrical', 'Cleaning', 'Structural', 'General Maintenance', 'Other'];
  pageSizeOptions = [5, 10, 20];

  filters: MaintenanceRequestFilter = { pageNumber: 1, pageSize: 10 };
  totalCount = 0;
  totalPages = 1;
  loading = false;
  saving = false;
  loadingDetail = false;
  errorMsg = '';
  formError = '';
  actionError = '';
  showForm = false;
  editingId: number | null = null;
  actionMode: 'reject' | 'cancel' | null = null;
  actionTarget: MaintenanceRequest | null = null;
  actionReason = '';
  showSuccessPopup = false;
  successPopupMessage = '';

  private readonly unitNumberPattern = /^[A-C]-(0[1-9]|1[0-9]|20)-0[1-9]$/i;

  constructor(private fb: FormBuilder, private svc: MaintenanceService) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      requesterName: ['', [Validators.required, Validators.maxLength(100), this.requesterNameValidator()]],
      unitNumber: ['', [Validators.required, this.unitNumberValidator()]],
      requestTitle: [''],
      issueCategory: ['', [Validators.required, Validators.maxLength(100)]],
      customIssueCategory: ['', [Validators.maxLength(100), this.notBlankWhenOther()]],
      priorityLevel: ['Medium', Validators.required],
      description: ['', [Validators.maxLength(2000)]],
    });

    this.form.get('issueCategory')?.valueChanges.subscribe(() => {
      this.form.get('customIssueCategory')?.updateValueAndValidity();
    });

    this.loadRequesters();
    this.loadRequests();
  }

  get f() { return this.form.controls; }

  get currentPage(): number {
    return this.filters.pageNumber || 1;
  }

  get pageSize(): number {
    return this.filters.pageSize || 10;
  }

  set pageSize(value: number) {
    this.filters.pageSize = Number(value) || 10;
  }

  get totalItems(): number {
    return this.totalCount;
  }

  get startItem(): number {
    return this.totalItems === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
  }

  get endItem(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalItems);
  }

  get pageNumbers(): number[] {
    const maxVisible = 5;
    const half = Math.floor(maxVisible / 2);
    let start = Math.max(1, this.currentPage - half);
    const end = Math.min(this.totalPages, start + maxVisible - 1);
    start = Math.max(1, end - maxVisible + 1);

    return Array.from({ length: end - start + 1 }, (_, index) => start + index);
  }

  loadRequests(): void {
    this.loading = true;
    this.errorMsg = '';

    this.svc.getRequestPage({ ...this.filters }).subscribe({
      next: response => {
        this.requests = response.items ?? [];
        this.totalCount = response.totalCount;
        this.totalPages = response.totalPages || 1;
        this.filters.pageNumber = response.pageNumber || 1;
        this.loading = false;
      },
      error: err => {
        this.requests = [];
        this.loading = false;
        this.errorMsg = err.error?.message || 'Failed to load maintenance requests.';
      }
    });
  }

  loadRequesters(): void {
    this.svc.getRequesters().subscribe({
      next: data => (this.requesters = data),
      error: () => (this.requesters = []),
    });
  }

  applyFilters(): void {
    this.filters.pageNumber = 1;
    this.loadRequests();
  }

  applyDateFilter(): void {
    this.filters.createdTo = this.filters.createdFrom || undefined;
    this.applyFilters();
  }

  clearFilters(): void {
    this.filters = { pageNumber: 1, pageSize: 10 };
    this.loadRequests();
  }

  onPageSizeChanged(): void {
    this.filters.pageNumber = 1;
    this.loadRequests();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage || this.loading) return;
    this.filters.pageNumber = page;
    this.loadRequests();
  }

  changePage(offset: number): void {
    const next = Math.min(Math.max((this.filters.pageNumber || 1) + offset, 1), this.totalPages || 1);
    if (next === this.filters.pageNumber) return;
    this.filters.pageNumber = next;
    this.loadRequests();
  }

  openCreate(): void {
    this.showForm = true;
    this.editingId = null;
    this.formError = '';
    this.form.reset({ requesterName: '', priorityLevel: 'Medium' });
    this.scrollToRequestForm();
  }

  editRequest(request: MaintenanceRequest): void {
    if (!this.canEdit(request)) return;
    this.loadingDetail = true;
    this.svc.getRequestById(request.requestID).subscribe({
      next: detail => {
        const issue = this.resolveIssueForForm(detail.issueCategory);
        this.selected = detail;
        this.showForm = true;
        this.editingId = detail.requestID;
        this.formError = '';
        this.form.reset({
          requesterName: detail.occupantName,
          unitNumber: detail.unitNumber,
          requestTitle: detail.requestTitle,
          issueCategory: issue.issueCategory,
          customIssueCategory: issue.customIssueCategory,
          priorityLevel: detail.priorityLevel,
          description: detail.description,
        });
        this.loadingDetail = false;
        this.scrollToRequestForm();
      },
      error: err => {
        this.loadingDetail = false;
        this.errorMsg = err.error?.message || 'Failed to open request.';
      }
    });
  }

  viewRequest(request: MaintenanceRequest): void {
    this.loadingDetail = true;
    this.svc.getRequestById(request.requestID).subscribe({
      next: detail => {
        this.selected = detail;
        this.loadingDetail = false;
        this.scrollToRequestDetail();
      },
      error: err => {
        this.loadingDetail = false;
        this.errorMsg = err.error?.message || 'Failed to open request details.';
      }
    });
  }

  submitForm(): void {
    this.form.patchValue({ unitNumber: this.form.value.unitNumber?.toUpperCase() });
    if (this.form.invalid || this.saving) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.formError = '';
    const raw = this.form.getRawValue();
    const request$ = this.editingId
      ? this.svc.updateRequest(this.editingId, raw)
      : this.svc.createRequest(raw);

    request$.subscribe({
      next: detail => {
        this.saving = false;
        this.showForm = false;
        this.selected = detail;
        this.loadRequests();
      },
      error: err => {
        this.saving = false;
        this.formError = this.extractError(err, 'Failed to save maintenance request.');
      }
    });
  }

  approve(request: MaintenanceRequest): void {
    if (!this.canApprove(request)) return;
    this.svc.approveRequest(request.requestID).subscribe({
      next: () => this.afterAction(request),
      error: err => (this.errorMsg = this.extractError(err, 'Failed to approve request.')),
    });
  }

  beginAction(mode: 'reject' | 'cancel', request: MaintenanceRequest): void {
    this.actionMode = mode;
    this.actionTarget = request;
    this.actionReason = '';
    this.actionError = '';
    this.scrollToRequestAction();
  }

  submitAction(): void {
    const reason = this.actionReason.trim();
    if (!this.actionTarget || !this.actionMode) return;
    if (!reason || reason.length > 500) {
      this.actionError = 'Reason is required and must be 500 characters or less.';
      return;
    }

    const request = this.actionTarget;
    const action$ = this.actionMode === 'reject'
      ? this.svc.rejectRequest(request.requestID, reason)
      : this.svc.cancelRequest(request.requestID, reason);

    action$.subscribe({
      next: () => {
        this.actionMode = null;
        this.actionTarget = null;
        this.actionReason = '';
        this.afterAction(request);
      },
      error: err => (this.actionError = this.extractError(err, 'Failed to update request.')),
    });
  }

  canEdit(request: MaintenanceRequest): boolean { return request.status === 'Pending'; }
  canApprove(request: MaintenanceRequest): boolean { return request.status === 'Pending'; }
  canReject(request: MaintenanceRequest): boolean { return request.status === 'Pending'; }
  canCancel(request: MaintenanceRequest): boolean { return request.status === 'Pending' || request.status === 'Approved'; }

  closeSuccessPopup(): void {
    this.showSuccessPopup = false;
    this.successPopupMessage = '';
  }

  closeForm(): void {
    this.showForm = false;
    this.editingId = null;
    this.formError = '';
  }

  trackByRequest(_: number, request: MaintenanceRequest): number {
    return request.requestID;
  }

  private scrollToRequestDetail(): void {
    setTimeout(() => {
      this.requestDetailSection?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  }

  private scrollToRequestForm(): void {
    setTimeout(() => {
      this.requestFormSection?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  }

  private scrollToRequestAction(): void {
    setTimeout(() => {
      this.requestActionSection?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  }

  private afterAction(request: MaintenanceRequest): void {
    this.loadRequests();
    this.viewRequest(request);
  }

  private resolveIssueForForm(issueCategory: string): { issueCategory: string; customIssueCategory: string } {
    return this.issueTypes.includes(issueCategory)
      ? { issueCategory, customIssueCategory: '' }
      : { issueCategory: 'Other', customIssueCategory: issueCategory };
  }

  private extractError(err: any, fallback: string): string {
    if (err.error?.errors) {
      const first = Object.values(err.error.errors)[0] as string[] | undefined;
      if (first?.length) return first[0];
    }
    return err.error?.message || fallback;
  }
  private requesterNameValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) return null;
      return /^[A-Za-z ]+$/.test(String(control.value).trim()) ? null : { requesterNameFormat: true };
    };
  }

  private unitNumberValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) return null;
      return this.unitNumberPattern.test(String(control.value).trim()) ? null : { unitFormat: true };
    };
  }

  private notBlank(control: AbstractControl): ValidationErrors | null {
    return typeof control.value === 'string' && control.value.trim().length === 0 ? { blank: true } : null;
  }

  private notBlankWhenOther(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const issueCategory = control.parent?.get('issueCategory')?.value;
      if (issueCategory !== 'Other') return null;
      return typeof control.value === 'string' && control.value.trim().length > 0 ? null : { requiredWhenOther: true };
    };
  }
}
















