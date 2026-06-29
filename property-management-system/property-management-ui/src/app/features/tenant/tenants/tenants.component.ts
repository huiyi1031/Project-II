import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { OccupantService } from '../../../core/services/occupant.service';
import { UploadService } from '../../../core/services/upload.service';
import { TenantRecord } from '../../../core/models';

type TenantView = 'list' | 'add-step1' | 'add-step2' | 'pending' | 'remove-confirm';

@Component({
  selector: 'app-tenants',
  templateUrl: './tenants.component.html',
  standalone: false,
})
export class TenantsComponent implements OnInit {
  view: TenantView = 'list';
  tenants: TenantRecord[] = [];
  isLoading = false; isSaving = false;
  errorMsg = ''; successMsg = '';

  step1Form: FormGroup;
  step2Form: FormGroup;
  uploadedFileName = '';
  uploadedFileRef  = '';
  uploading = false;

  removeTarget: TenantRecord | null = null;
  removeForm: FormGroup;

  units: any[] = [{ unitID: 1, unitNumber: 'A-12-03' }]; // fallback

  constructor(
    private fb: FormBuilder,
    private svc: OccupantService,
    private uploadSvc: UploadService,
  ) {
    this.step1Form = fb.group({
      fullName:         ['', [Validators.required, Validators.minLength(2)]],
      identificationNo: ['', [Validators.required, Validators.minLength(6)]],
      email:            ['', [Validators.required, Validators.email]],
      contactNumber:    ['', [Validators.required, Validators.pattern(/^[0-9+\-() ]{8,15}$/)]],
      gender:           ['', Validators.required],
      unitID:           ['', Validators.required],
      startDate:        ['', Validators.required],
      endDate:          ['', Validators.required],
    });
    this.step2Form = fb.group({
      agreementFile: [null, Validators.required],
    });
    this.removeForm = fb.group({
      removalType: ['EarlyTermination', Validators.required],
      reason:      ['', Validators.required],
    });
  }

  ngOnInit(): void { this.loadTenants(); }

  loadTenants(): void {
    this.isLoading = true;
    this.svc.getMyTenants().subscribe({
      next: (d) => { this.tenants = d; this.isLoading = false; },
      error: () => {
        this.tenants = [
          { occupantID: 20, fullName: 'Ravi Kumar',   identificationNo: '870505-14-1234', email: 'ravi@demo.com', contactNumber: '012-9999999', contractID: 101, unitNumber: 'A-12-03', startDate: '2024-01-01', endDate: '2025-12-31', status: 'Active' },
          { occupantID: 21, fullName: 'Priya Sharma',  identificationNo: '900202-10-5678', email: 'priya@demo.com', contactNumber: '017-1111111', contractID: 102, unitNumber: 'B-05-12', startDate: '2023-06-01', endDate: '2024-05-31', status: 'Expired' },
          { occupantID: 22, fullName: 'Lee Wei Ming',  identificationNo: '950707-08-9999', email: 'lee@demo.com', contactNumber: '011-2222222', contractID: 103, unitNumber: 'A-12-03', startDate: '2025-03-01', endDate: '2026-02-28', status: 'PendingApproval' },
        ];
        this.isLoading = false;
      }
    });
  }

  // Step 1: Personal details
  goToStep2(): void {
    if (this.step1Form.invalid) { this.step1Form.markAllAsTouched(); return; }
    this.view = 'add-step2';
    this.errorMsg = '';
  }

  // Step 2: Upload tenancy agreement
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;
    const allowed = ['application/pdf', 'image/jpeg', 'image/png'];
    if (!allowed.includes(file.type)) {
      this.errorMsg = 'Invalid file format. Please upload PDF, JPG, or PNG only.';
      return;
    }
    if (file.size > 10_000_000) { this.errorMsg = 'File too large. Maximum 10 MB.'; return; }
    this.errorMsg = '';
    this.uploading = true;
    // Phase 1 upload: get temp fileRef
    this.uploadSvc.uploadTemp(file).subscribe(result => {
      this.uploading = false;
      this.uploadedFileName = result.fileName;
      this.uploadedFileRef  = result.fileRef;
      this.step2Form.patchValue({ agreementFile: result.fileRef });
    });
  }

  submitTenant(): void {
    if (!this.uploadedFileRef) { this.errorMsg = 'Please upload the tenancy agreement.'; return; }
    this.isSaving = true;
    const dto = { ...this.step1Form.value, agreementFileRef: this.uploadedFileRef };
    this.svc.addTenant(dto).subscribe({
      next: () => {
        this.isSaving = false;
        this.view = 'pending';
        this.successMsg = 'Tenant registration submitted for manager approval. An activation email will be sent upon approval.';
      },
      error: () => {
        this.isSaving = false;
        this.view = 'pending'; // Demo success
        this.successMsg = 'Approval request sent to Property Manager (demo mode).';
      }
    });
  }

  confirmRemove(t: TenantRecord): void { this.removeTarget = t; this.view = 'remove-confirm'; }

  submitRemove(): void {
    if (!this.removeTarget) return;
    this.isSaving = true;
    this.svc.removeTenant(this.removeTarget.contractID, this.removeForm.value).subscribe({
      next: () => { this.isSaving = false; this.successMsg = 'Tenant removal request submitted.'; this.view = 'list'; this.loadTenants(); },
      error: () => {
        this.isSaving = false;
        this.successMsg = 'Removal request sent (demo mode).';
        this.tenants = this.tenants.filter(t => t.contractID !== this.removeTarget!.contractID);
        this.view = 'list';
      }
    });
  }

  cancelAdd(): void { this.view = 'list'; this.step1Form.reset(); this.step2Form.reset(); this.uploadedFileName = ''; this.errorMsg = ''; }
}
