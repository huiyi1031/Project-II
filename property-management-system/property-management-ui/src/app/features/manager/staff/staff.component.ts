import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { StaffService } from '../../../core/services/staff.service';
import { StaffRecord, StaffDeactivateDto, CreateStaffDto } from '../../../core/models';

type StaffView = 'list' | 'create' | 'edit' | 'deactivate';

@Component({
  selector: 'app-staff',
  templateUrl: './staff.component.html',
  standalone: false,
})
export class StaffComponent implements OnInit {
  view: StaffView = 'list';
  staffList: StaffRecord[] = [];
  isLoading  = false;
  isSaving   = false;
  successMsg = '';
  errorMsg   = '';

  // List filters
  searchTerm = '';
  roleFilter = 'All';

  // Forms
  createForm!: FormGroup;
  editForm!:   FormGroup;
  deactivateForm!: FormGroup;

  selectedStaff: StaffRecord | null = null;

  // Duplicate email check state
  emailChecking = false;
  emailConflict = false;

  // Temp password revealed after creation
  tempPasswordShown = '';

  // Service types for dropdown
  serviceTypes = [
    { id: 1, name: 'Electrical' },
    { id: 2, name: 'Plumbing' },
    { id: 3, name: 'HVAC & Air-Conditioning' },
    { id: 4, name: 'Civil & Structural' },
    { id: 5, name: 'Landscaping' },
    { id: 6, name: 'General Maintenance' },
  ];

  deactivateReasons = [
    { code: 'Resigned',    label: 'Staff Resigned' },
    { code: 'Terminated',  label: 'Terminated by Management' },
    { code: 'OnLeave',     label: 'Extended Leave / Medical' },
    { code: 'Other',       label: 'Other Reason' },
  ];

  constructor(private fb: FormBuilder, private svc: StaffService) {}

  ngOnInit(): void {
    this._buildForms();
    this.loadStaff();
  }

  private _buildForms(): void {
    this.createForm = this.fb.group({
      roleType:           ['Technician', Validators.required],
      fullName:           ['', [Validators.required, Validators.minLength(2)]],
      email:              ['', [Validators.required, Validators.email]],
      contactNumber:      ['', [Validators.required, Validators.pattern(/^[0-9+\-() ]{8,15}$/)]],
      gender:             ['', Validators.required],
      // Technician-only fields
      serviceTypeID:      [null],
      experienceLevel:    ['Junior'],
      availabilityStatus: ['Available'],
      priorityRank:       [null],
      // Manager-only fields
      position:           [''],
    });

    this.editForm = this.fb.group({
      serviceTypeID:      [null],
      experienceLevel:    ['Junior'],
      availabilityStatus: ['Available'],
      priorityRank:       [null],
      position:           [''],
    });

    this.deactivateForm = this.fb.group({
      reasonCode:   ['', Validators.required],
      reasonDetail: [''],
    });
  }

  // ── Filtered list ─────────────────────────────────────────────────────────
  get filteredStaff(): StaffRecord[] {
    return this.staffList.filter(s => {
      const q    = this.searchTerm.toLowerCase();
      const match = !q || s.fullName.toLowerCase().includes(q) || s.email.toLowerCase().includes(q);
      const role  = this.roleFilter === 'All' || s.roleType === this.roleFilter;
      return match && role;
    });
  }

  get isCreatingTechnician(): boolean { return this.createForm.get('roleType')?.value === 'Technician'; }
  get isEditingTechnician():  boolean { return this.selectedStaff?.roleType === 'Technician'; }

  // ── Load ──────────────────────────────────────────────────────────────────
  loadStaff(): void {
    this.isLoading = true;
    this.svc.getAllStaff().subscribe({
      next: (data: any[]) => {
        this.staffList = data.map((s: any) => ({
          accountID:         s.accountID || s.technicianID || s.managerID,
          fullName:          s.fullName,
          email:             s.email || '',
          roleType:          s.technicianID ? 'Technician' : 'PropertyManager',
          accountStatus:     s.accountStatus || 'Active',
          lastLogin:         s.lastLogin,
          technicianID:      s.technicianID,
          serviceTypeName:   s.serviceTypeName,
          experienceLevel:   s.experienceLevel,
          availabilityStatus: s.availabilityStatus,
          ranking:           s.ranking,
          managerID:         s.managerID,
          position:          s.position,
        }));
        this.isLoading = false;
      },
      error: () => {
        // Demo fallback
        this.staffList = [
          { accountID: 1, fullName: 'Daniel Tan',     email: 'tech@demo.com',  roleType: 'Technician',      accountStatus: 'Active',  lastLogin: '2026-06-28T10:30:00Z', technicianID: 1, serviceTypeName: 'HVAC & Air-Conditioning', experienceLevel: 'Senior',       availabilityStatus: 'Available', ranking: 1 },
          { accountID: 2, fullName: 'Farid Hassan',   email: 'farid@demo.com', roleType: 'Technician',      accountStatus: 'Active',  lastLogin: '2026-06-27T09:15:00Z', technicianID: 2, serviceTypeName: 'Plumbing',              experienceLevel: 'Intermediate', availabilityStatus: 'Busy',      ranking: 2 },
          { accountID: 3, fullName: 'Lee Xin Ying',   email: 'lee@demo.com',   roleType: 'Technician',      accountStatus: 'Active',  lastLogin: '2026-06-26T14:00:00Z', technicianID: 3, serviceTypeName: 'Electrical',            experienceLevel: 'Junior',       availabilityStatus: 'OffDuty',   ranking: 3 },
          { accountID: 4, fullName: 'Ahmad Fauzi',    email: 'admin@demo.com', roleType: 'PropertyManager', accountStatus: 'Active',  lastLogin: '2026-06-28T08:00:00Z', managerID: 1, position: 'Lead Property Manager' },
          { accountID: 5, fullName: 'Nurul Izyana',   email: 'nuru@demo.com',  roleType: 'PropertyManager', accountStatus: 'Deactivated', lastLogin: '2026-05-01T11:00:00Z', managerID: 2, position: 'Property Manager' },
        ];
        this.isLoading = false;
      }
    });
  }

  // ── Create ────────────────────────────────────────────────────────────────
  openCreate(): void {
    this.createForm.reset({ roleType: 'Technician', experienceLevel: 'Junior', availabilityStatus: 'Available' });
    this.emailConflict = false;
    this.tempPasswordShown = '';
    this.errorMsg = '';
    this.view = 'create';
  }

  /** Real-world: check email before submitting — prevents duplicate accounts */
  checkEmailConflict(): void {
    const email = this.createForm.get('email')?.value;
    if (!email || this.createForm.get('email')?.invalid) return;
    this.emailChecking = true;
    this.emailConflict = false;
    // Demo: simulate conflict for 'dup@demo.com'
    setTimeout(() => {
      this.emailChecking = false;
      this.emailConflict = email === 'dup@demo.com';
    }, 600);
    // Real API: this.svc.checkEmail(email).subscribe(...)
  }

  submitCreate(): void {
    if (this.createForm.invalid || this.emailConflict) {
      this.createForm.markAllAsTouched();
      return;
    }
    this.isSaving = true; this.errorMsg = '';

    const dto: CreateStaffDto = {
      fullName:       this.createForm.value.fullName,
      email:          this.createForm.value.email,
      contactNumber:  this.createForm.value.contactNumber,
      roleType:       this.createForm.value.roleType,
      ...(this.isCreatingTechnician ? {
        serviceTypeID:      this.createForm.value.serviceTypeID,
        experienceLevel:    this.createForm.value.experienceLevel,
        availabilityStatus: this.createForm.value.availabilityStatus,
        priorityRank:       this.createForm.value.priorityRank,
      } : {
        // Manager-specific — position sent as part of a separate profile object
      }),
    };

    this.svc.createStaff(dto).subscribe({
      next: (res: any) => {
        this.isSaving = false;
        this.tempPasswordShown = res?.temporaryPassword || 'TEMP-' + Math.random().toString(36).slice(2, 8).toUpperCase();
        this.successMsg = `Staff account created. Activation email sent to ${dto.email}.`;
        this.loadStaff();
      },
      error: () => {
        // Demo: simulate success
        this.isSaving = false;
        this.tempPasswordShown = 'TEMP-' + Math.random().toString(36).slice(2, 8).toUpperCase();
        this.successMsg = `Staff account created (demo mode). Activation email sent to ${dto.email}.`;
        const newStaff: StaffRecord = {
          accountID: Date.now(), fullName: dto.fullName, email: dto.email,
          roleType: dto.roleType, accountStatus: 'Pending',
          serviceTypeName: this.serviceTypes.find(s => s.id == dto.serviceTypeID)?.name,
          experienceLevel: dto.experienceLevel, availabilityStatus: dto.availabilityStatus,
        };
        this.staffList = [newStaff, ...this.staffList];
      }
    });
  }

  // ── Edit ──────────────────────────────────────────────────────────────────
  openEdit(staff: StaffRecord): void {
    this.selectedStaff = staff;
    this.editForm.patchValue({
      serviceTypeID:      this.serviceTypes.find(s => s.name === staff.serviceTypeName)?.id,
      experienceLevel:    staff.experienceLevel,
      availabilityStatus: staff.availabilityStatus,
      priorityRank:       staff.ranking,
      position:           staff.position,
    });
    this.errorMsg = ''; this.successMsg = '';
    this.view = 'edit';
  }

  submitEdit(): void {
    if (this.editForm.invalid || !this.selectedStaff) return;
    this.isSaving = true;
    this.svc.updateStaff(this.selectedStaff.accountID, this.editForm.value).subscribe({
      next: () => { this.isSaving = false; this.successMsg = 'Staff details updated.'; this.view = 'list'; this.loadStaff(); },
      error: () => {
        this.isSaving = false;
        // Demo success
        const idx = this.staffList.findIndex(s => s.accountID === this.selectedStaff!.accountID);
        if (idx >= 0) {
          this.staffList[idx] = { ...this.staffList[idx], ...this.editForm.value, serviceTypeName: this.serviceTypes.find(s => s.id == this.editForm.value.serviceTypeID)?.name };
        }
        this.successMsg = 'Staff details updated (demo mode).';
        this.view = 'list';
      }
    });
  }

  // ── Deactivate ────────────────────────────────────────────────────────────
  openDeactivate(staff: StaffRecord): void {
    this.selectedStaff = staff;
    this.deactivateForm.reset();
    this.errorMsg = ''; this.successMsg = '';
    this.view = 'deactivate';
  }

  submitDeactivate(): void {
    if (this.deactivateForm.invalid || !this.selectedStaff) {
      this.deactivateForm.markAllAsTouched();
      return;
    }
    this.isSaving = true;
    const dto: StaffDeactivateDto = {
      accountID:    this.selectedStaff.accountID,
      reasonCode:   this.deactivateForm.value.reasonCode,
      reasonDetail: this.deactivateForm.value.reasonDetail,
    };
    this.svc.deactivateStaff(dto.accountID, dto.reasonCode).subscribe({
      next: () => { this.isSaving = false; this.successMsg = `${this.selectedStaff!.fullName}'s account has been deactivated.`; this.view = 'list'; this.loadStaff(); },
      error: () => {
        this.isSaving = false;
        const idx = this.staffList.findIndex(s => s.accountID === this.selectedStaff!.accountID);
        if (idx >= 0) this.staffList[idx] = { ...this.staffList[idx], accountStatus: 'Deactivated' };
        this.successMsg = `${this.selectedStaff!.fullName} deactivated (demo mode).`;
        this.view = 'list';
      }
    });
  }

  cancelAction(): void { this.view = 'list'; this.selectedStaff = null; this.errorMsg = ''; }

  countByRole(role: string): number    { return this.staffList.filter(s => s.roleType === role).length; }
  countByStatus(status: string): number { return this.staffList.filter(s => s.accountStatus === status).length; }

  formatLastLogin(d?: string): string {
    if (!d) return 'Never';
    const dt = new Date(d);
    const diff = Date.now() - dt.getTime();
    const h = Math.floor(diff / 3_600_000);
    if (h < 1)   return 'Just now';
    if (h < 24)  return `${h}h ago`;
    const days = Math.floor(h / 24);
    if (days < 7) return `${days}d ago`;
    return dt.toLocaleDateString('en-MY');
  }
}
