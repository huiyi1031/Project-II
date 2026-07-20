import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { OccupantService } from '../../../core/services/occupant.service';
import { UploadService } from '../../../core/services/upload.service';
import { TenantRecord } from '../../../core/models';

type TenantView = 'list' | 'add-step1' | 'add-step2' | 'pending' | 'remove-confirm' | 'details';

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

  removeTarget: TenantRecord | null = null;
  removeForm: FormGroup;
  selectedTenant: TenantRecord | null = null;

  units: any[] = [{ unitId: 1, unitNumber: 'A-12-03' }]; // fallback

  constructor(
    private fb: FormBuilder,
    private svc: OccupantService,
    private uploadSvc: UploadService,
  ) {
    this.step1Form = fb.group({
      fullName:         ['', [Validators.required, Validators.minLength(2)]],
      email:            ['', [Validators.required, Validators.email]],
      contactNumber:    ['', [Validators.required, Validators.pattern(/^[0-9+\-() ]{8,15}$/)]],
      gender:           ['', Validators.required],
      dateOfBirth:      ['', Validators.required],
      unitID:           ['', Validators.required],
      startDate:        ['', Validators.required],
      endDate:          ['', Validators.required]
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
        this.errorMsg = 'Failed to load tenants.';
        this.tenants = [];
        this.isLoading = false;
      }
    });
  }

  submitTenant(): void {
    if (this.step1Form.invalid) { this.step1Form.markAllAsTouched(); return; }
    
    this.isSaving = true;
    this.svc.addTenant(this.step1Form.value).subscribe({
      next: (res: any) => {
        this.isSaving = false;
        this.successMsg = `Tenant added. A temporary password (${res.tempPassword}) was sent via email.`;
        this.view = 'list';
        this.loadTenants();
        this.step1Form.reset();
        setTimeout(() => this.successMsg = '', 6000);
      },
      error: (e) => {
        this.isSaving = false;
        this.errorMsg = e.error?.message || 'Failed to add tenant.';
      }
    });
  }

  confirmRemove(t: TenantRecord): void { this.removeTarget = t; this.view = 'remove-confirm'; }

  submitRemove(): void {
    if (!this.removeTarget) return;
    this.isSaving = true;
    this.svc.removeTenant(this.removeTarget.occupantID, {}).subscribe({
      next: () => { this.isSaving = false; this.successMsg = 'Tenant removal request submitted.'; this.view = 'list'; this.loadTenants(); },
      error: () => {
        this.isSaving = false;
        this.errorMsg = 'Failed to remove tenant.';
        this.view = 'list';
      }
    });
  }

  cancelAdd(): void { this.view = 'list'; this.step1Form.reset(); this.errorMsg = ''; }

  viewDetails(t: TenantRecord): void {
    this.selectedTenant = t;
    this.view = 'details';
  }

  closeDetails(): void {
    this.selectedTenant = null;
    this.view = 'list';
  }
}
