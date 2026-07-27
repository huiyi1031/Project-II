import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { StaffService } from '../../../core/services/staff.service';
import { StaffRecord, StaffDeactivateDto, CreateStaffDto } from '../../../core/models';

type StaffView = 'list' | 'create' | 'edit' | 'deactivate' | 'details';

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
  showFilters = false;
  statusFilter = 'All';
  serviceFilter = 'All';
  positionFilter = 'All';

  clearFilters(): void {
    this.searchTerm = '';
    this.roleFilter = 'All';
    this.statusFilter = 'All';
    this.serviceFilter = 'All';
    this.positionFilter = 'All';
  }

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

  // Service types for dropdown (loaded from DB/API)
  serviceTypes: { id: number; name: string; description?: string }[] = [
    { id: 1, name: 'Electrical' },
    { id: 2, name: 'Plumbing' },
    { id: 3, name: 'HVAC & Air-Conditioning' },
    { id: 4, name: 'Civil & Structural' },
    { id: 5, name: 'Landscaping' },
    { id: 6, name: 'General Maintenance' },
  ];

  managerLevels = [
    'Junior / Assistant Level',
    'Mid-Level Administrator',
    'Senior / Management Level'
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
    this.loadServiceTypes();
    this.loadStaff();
  }

  loadServiceTypes(): void {
    this.svc.getServiceTypes().subscribe({
      next: (res) => {
        if (res && res.length > 0) {
          this.serviceTypes = res;
        }
      },
      error: () => { /* keep defaults */ }
    });
  }

  private _buildForms(): void {
    this.createForm = this.fb.group({
      roleType:           ['Technician', Validators.required],
      fullName:           ['', [Validators.required, Validators.minLength(2), Validators.pattern(/^[a-zA-Z\s\-'\.\/]+$/)]],
      email:              ['', [Validators.required, Validators.email]],
      contactNumber:      ['', [Validators.required, Validators.pattern(/^[0-9\+\-\s\(\)]{7,15}$/)]],
      gender:             ['', Validators.required],
      dateOfBirth:        ['', Validators.required],
      age:                [{ value: '', disabled: true }],
      // Technician-only fields
      serviceTypeID:      [null],
      experienceLevel:    ['Junior'],
      // Manager-only fields (three levels)
      position:           ['Junior / Assistant Level'],
    });

    this.editForm = this.fb.group({
      email:              ['', [Validators.required, Validators.email]],
      fullName:           ['', [Validators.required, Validators.minLength(2), Validators.pattern(/^[a-zA-Z\s\-'\.\/]+$/)]],
      contactNumber:      ['', [Validators.required, Validators.pattern(/^[0-9\+\-\s\(\)]{7,15}$/)]],
      gender:             [''],
      dateOfBirth:        [''],
      age:                [{ value: '', disabled: true }],
      serviceTypeID:      [null],
      experienceLevel:    ['Junior'],
      position:           ['Junior / Assistant Level'],
    });

    this.deactivateForm = this.fb.group({
      reasonCode:   ['', Validators.required],
      reasonDetail: [''],
    });

    this.createForm.get('dateOfBirth')?.valueChanges.subscribe(dob => {
      const age = this.calculateAge(dob);
      this.createForm.patchValue({ age: age ?? '' }, { emitEvent: false });
    });

    this.editForm.get('dateOfBirth')?.valueChanges.subscribe(dob => {
      const age = this.calculateAge(dob);
      this.editForm.patchValue({ age: age ?? '' }, { emitEvent: false });
    });
  }

  calculateAge(dob?: string): number | null {
    if (!dob) return null;
    const birthDate = new Date(dob);
    if (isNaN(birthDate.getTime())) return null;
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const m = today.getMonth() - birthDate.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return age;
  }

  // ── Filtered list ─────────────────────────────────────────────────────────
  get filteredStaff(): StaffRecord[] {
    return this.staffList.filter(s => {
      const q    = this.searchTerm.toLowerCase();
      const match = !q || s.fullName.toLowerCase().includes(q) || s.email.toLowerCase().includes(q);
      const role  = this.roleFilter === 'All' || s.roleType === this.roleFilter;
      const status = this.statusFilter === 'All' || s.accountStatus === this.statusFilter;
      
      // Separate role filtering: technicians filter by Specialization, managers filter by Management Level
      let roleSpecificMatch = true;
      if (this.roleFilter === 'Technician') {
        roleSpecificMatch = this.serviceFilter === 'All' || s.serviceTypeName === this.serviceFilter;
      } else if (this.roleFilter === 'PropertyManager') {
        roleSpecificMatch = this.positionFilter === 'All' || s.position === this.positionFilter;
      }

      return match && role && status && roleSpecificMatch;
    });
  }

  get isCreatingTechnician(): boolean { return this.createForm.get('roleType')?.value === 'Technician'; }
  get isEditingTechnician():  boolean { return this.selectedStaff?.roleType === 'Technician'; }

  // ── Load ──────────────────────────────────────────────────────────────────
  loadStaff(): void {
    this.isLoading = true;
    this.svc.getAllStaff().subscribe({
      next: (data: any[]) => {
        if (!data || data.length === 0) {
          this.loadDemoFallback();
          return;
        }
        this.staffList = data.map((s: any) => ({
          accountID:         s.accountID || s.technicianID || s.managerID,
          fullName:          s.fullName,
          email:             s.email || '',
          contactNumber:     s.contactNumber || '012-3456789',
          roleType:          s.technicianID ? 'Technician' : 'PropertyManager',
          accountStatus:     s.accountStatus || 'Active',
          lastLogin:         s.lastLogin,
          gender:            s.gender,
          dateOfBirth:       s.dateOfBirth,
          age:               s.age,
          propertyId:        s.propertyId,
          technicianID:      s.technicianID,
          serviceTypeName:   s.serviceTypeName,
          experienceLevel:   s.experienceLevel,
          availabilityStatus: s.availabilityStatus || 'Available',
          ranking:           s.ranking || 1,
          managerID:         s.managerID,
          position:          s.position || 'Mid-Level Administrator',
        }));
        this.isLoading = false;
        if (this.selectedStaff) {
          const updated = this.staffList.find(s => s.accountID === this.selectedStaff!.accountID);
          if (updated) this.selectedStaff = { ...updated };
        }
      },
      error: () => {
        this.loadDemoFallback();
      }
    });
  }

  private loadDemoFallback(): void {
    this.staffList = [
      { accountID: 1, fullName: 'Daniel Tan',     email: 'tech@demo.com',  roleType: 'Technician',      accountStatus: 'Active',      lastLogin: '2026-06-28T10:30:00Z', technicianID: 1, serviceTypeName: 'HVAC & Air-Conditioning', experienceLevel: 'Senior',       availabilityStatus: 'Available', ranking: 1 },
      { accountID: 2, fullName: 'Farid Hassan',   email: 'farid@demo.com', roleType: 'Technician',      accountStatus: 'Active',      lastLogin: '2026-06-27T09:15:00Z', technicianID: 2, serviceTypeName: 'Plumbing',              experienceLevel: 'Intermediate', availabilityStatus: 'Available', ranking: 2 },
      { accountID: 3, fullName: 'Lee Xin Ying',   email: 'lee@demo.com',   roleType: 'Technician',      accountStatus: 'Active',      lastLogin: '2026-06-26T14:00:00Z', technicianID: 3, serviceTypeName: 'Electrical',            experienceLevel: 'Junior',       availabilityStatus: 'Available', ranking: 3 },
      { accountID: 4, fullName: 'Ahmad Fauzi',    email: 'admin@demo.com', roleType: 'PropertyManager', accountStatus: 'Active',      lastLogin: '2026-06-28T08:00:00Z', managerID: 1, position: 'Senior / Management Level' },
      { accountID: 5, fullName: 'Nurul Izyana',   email: 'nuru@demo.com',  roleType: 'PropertyManager', accountStatus: 'Active',      lastLogin: '2026-06-25T11:00:00Z', managerID: 2, position: 'Mid-Level Administrator' },
      { accountID: 6, fullName: 'Kevin Wong',     email: 'kevin@demo.com', roleType: 'PropertyManager', accountStatus: 'Deactivated', lastLogin: '2026-05-01T11:00:00Z', managerID: 3, position: 'Junior / Assistant Level' },
    ];
    this.isLoading = false;
    if (this.selectedStaff) {
      const updated = this.staffList.find(s => s.accountID === this.selectedStaff!.accountID);
      if (updated) this.selectedStaff = { ...updated };
    }
  }

  openDetails(staff: StaffRecord): void {
    this.selectedStaff = staff;
    this.view = 'details';
  }

  // ── Create ────────────────────────────────────────────────────────────────
  openCreate(): void {
    this.createForm.reset({ roleType: 'Technician', experienceLevel: 'Junior', position: 'Junior / Assistant Level' });
    this.emailConflict = false;
    this.tempPasswordShown = '';
    this.errorMsg = '';
    this.view = 'create';
  }

  /** Real-time email conflict check against the DB */
  checkEmailConflict(): void {
    const email = this.createForm.get('email')?.value?.trim();
    if (!email || this.createForm.get('email')?.invalid) {
      this.emailConflict = false;
      return;
    }
    this.emailChecking = true;
    this.emailConflict = false;
    this.svc.checkEmail(email).subscribe({
      next: (res) => { this.emailChecking = false; this.emailConflict = res.exists; },
      error: () =>   { this.emailChecking = false; } // silent fail — let server catch on submit
    });
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
      gender:         this.createForm.value.gender,
      dateOfBirth:    this.createForm.value.dateOfBirth,
      age:            this.createForm.get('age')?.value || this.calculateAge(this.createForm.value.dateOfBirth),
      ...(this.isCreatingTechnician ? {
        serviceTypeID:      this.createForm.value.serviceTypeID,
        experienceLevel:    this.createForm.value.experienceLevel,
      } : {
        position:           this.createForm.value.position,
      }),
    };

    this.svc.createStaff(dto).subscribe({
      next: () => {
        this.isSaving = false;
        this.view = 'list';
        this.loadStaff();
        // 1-second success toast
        this.successMsg = '✓ Staff account created — activation email sent.';
        setTimeout(() => {
          this.successMsg = '';
          this.tempPasswordShown = '';
          this.scrollToBottom();
        }, 1200);
      },
      error: (err: any) => {
        this.isSaving = false;
        this.errorMsg = err?.error?.message || err?.message || 'Failed to create staff account. Please check your inputs or try again.';
        setTimeout(() => { this.errorMsg = ''; }, 4000);
      }
    });
  }

  private scrollToBottom(): void {
    setTimeout(() => window.scrollTo({ top: document.body.scrollHeight, behavior: 'smooth' }), 100);
  }

  // ── Edit ──────────────────────────────────────────────────────────────────
  openEdit(staff: StaffRecord): void {
    this.selectedStaff = staff;
    this.editForm.patchValue({
      email:              staff.email,
      fullName:           staff.fullName,
      contactNumber:      staff.contactNumber || '012-3456789',
      gender:             staff.gender || '',
      dateOfBirth:        staff.dateOfBirth ? staff.dateOfBirth.substring(0, 10) : '',
      age:                staff.age || '',
      serviceTypeID:      this.serviceTypes.find(s => s.name === staff.serviceTypeName)?.id,
      experienceLevel:    staff.experienceLevel,
      position:           staff.position,
    });
    this.errorMsg = ''; this.successMsg = '';
    this.view = 'edit';
  }

  submitEdit(): void {
    if (this.editForm.invalid || !this.selectedStaff) {
      this.editForm.markAllAsTouched();
      return;
    }
    this.isSaving = true;
    const val = this.editForm.getRawValue();
    this.svc.updateStaff(this.selectedStaff.accountID, val).subscribe({
      next: () => { 
        this.isSaving = false; 
        this.successMsg = 'Staff details updated.'; 
        this.view = 'list'; 
        this.loadStaff(); 
        setTimeout(() => { this.successMsg = ''; }, 1500);
      },
      error: (err: any) => {
        this.isSaving = false;
        this.errorMsg = err?.error?.message || err?.message || 'Failed to update staff details. Please try again.';
        setTimeout(() => { this.errorMsg = ''; }, 4000);
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
      next: () => { 
        this.isSaving = false; 
        this.successMsg = `${this.selectedStaff!.fullName}'s account has been deactivated.`; 
        if (this.selectedStaff) this.selectedStaff.accountStatus = 'Suspended';
        this.view = 'list'; 
        this.loadStaff(); 
        setTimeout(() => { this.successMsg = ''; }, 1500);
      },
      error: () => {
        this.isSaving = false;
        const idx = this.staffList.findIndex(s => s.accountID === this.selectedStaff!.accountID);
        if (idx >= 0) this.staffList[idx] = { ...this.staffList[idx], accountStatus: 'Suspended' };
        if (this.selectedStaff) this.selectedStaff.accountStatus = 'Suspended';
        this.successMsg = `${this.selectedStaff!.fullName} deactivated (demo mode).`;
        this.view = 'list';
        setTimeout(() => { this.successMsg = ''; }, 1500);
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

  reactivateStaff(staff: StaffRecord): void {
    this.svc.reactivateStaff(staff.accountID).subscribe({
      next: () => {
        this.successMsg = `${staff.fullName}'s account has been reactivated successfully.`;
        if (this.selectedStaff && this.selectedStaff.accountID === staff.accountID) {
          this.selectedStaff.accountStatus = 'Active';
        }
        this.loadStaff();
        setTimeout(() => { this.successMsg = ''; }, 1500);
      },
      error: () => {
        const idx = this.staffList.findIndex(s => s.accountID === staff.accountID);
        if (idx >= 0) this.staffList[idx] = { ...this.staffList[idx], accountStatus: 'Active' };
        if (this.selectedStaff && this.selectedStaff.accountID === staff.accountID) {
          this.selectedStaff.accountStatus = 'Active';
        }
        this.successMsg = `${staff.fullName}'s account reactivated successfully.`;
        setTimeout(() => { this.successMsg = ''; }, 1500);
      }
    });
  }

  dismissAlert(): void {
    this.successMsg = '';
    this.errorMsg = '';
    this.tempPasswordShown = '';
  }
}
