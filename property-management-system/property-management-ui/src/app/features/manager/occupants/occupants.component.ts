import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Occupant } from '../../../core/models';
import { OccupantService } from '../../../core/services/occupant.service';

@Component({
  selector: 'app-occupants',
  templateUrl: './occupants.component.html',
  styleUrls: ['./occupants.component.css'],
  standalone: false,
})
export class OccupantsComponent implements OnInit {
  occupants: Occupant[] = [];
  search = '';
  filter = 'All';
  editMode = false;
  selectedOccupantId: number | null = null;
  form!: FormGroup;
  loading = false;
  saving = false;
  deleting = false;
  errorMsg = '';
  formError = '';
  showSuccessPopup = false;
  successPopupMessage = '';

  currentPage = 1;
  pageSize = 5;
  totalItems = 0;
  totalPages = 1;
  pageSizeOptions = [5, 10, 20, 50];
  private searchTimer?: ReturnType<typeof setTimeout>;

  private readonly namePattern = /^[A-Za-z ]+$/;
  private readonly identificationPattern = /^\d{6}-\d{2}-\d{4}$/;
  private readonly contactPattern = /^01\d-\d{7}$/;
  private readonly emailPattern = /^[^@\s]+@[^@\s]+\.com$/;
  private readonly unitNumberPattern = /^[A-C]-(0[1-9]|1[0-9]|20)-0[1-9]$/i;

  constructor(private fb: FormBuilder, private svc: OccupantService) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(100), this.patternValidator(this.namePattern, 'nameFormat')]],
      identificationNo: ['', [Validators.required, this.patternValidator(this.identificationPattern, 'identificationFormat')]],
      contactNumber: ['', [Validators.required, this.patternValidator(this.contactPattern, 'contactFormat')]],
      email: ['', [this.optionalPatternValidator(this.emailPattern, 'emailFormat')]],
      unitNumber: ['', [Validators.required, this.patternValidator(this.unitNumberPattern, 'unitFormat')]],
      occupantType: ['Tenant', Validators.required],
    });

    this.loadOccupants();
  }

  get f() { return this.form.controls; }

  get filteredOccupants(): Occupant[] {
    return this.occupants;
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

  loadOccupants(page = this.currentPage): void {
    this.currentPage = Math.max(1, page);
    this.loading = true;
    this.errorMsg = '';

    this.svc.getAllOccupants(this.filter, this.currentPage, this.pageSize, this.search).subscribe({
      next: result => {
        this.occupants = result.items;
        this.currentPage = result.page;
        this.pageSize = result.pageSize;
        this.totalItems = result.totalItems;
        this.totalPages = result.totalPages || 1;
        this.loading = false;
      },
      error: err => {
        this.occupants = [];
        this.totalItems = 0;
        this.totalPages = 1;
        this.loading = false;
        this.errorMsg = this.extractError(err, 'Failed to load occupant records. Please log in again if your session expired.');
      }
    });
  }

  onSearchChanged(): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.loadOccupants(1), 300);
  }

  onFilterChanged(): void {
    this.loadOccupants(1);
  }

  onPageSizeChanged(): void {
    this.loadOccupants(1);
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage || this.loading) return;
    this.loadOccupants(page);
  }

  editOccupant(occupant: Occupant): void {
    this.editMode = true;
    this.selectedOccupantId = occupant.occupantID;
    this.formError = '';
    this.form.reset({
      fullName: occupant.fullName ?? '',
      identificationNo: occupant.identificationNo ?? '',
      contactNumber: occupant.contactNumber ?? '',
      email: occupant.email ?? '',
      unitNumber: (occupant as any).unitNumber ?? '',
      occupantType: occupant.occupantType ?? 'Tenant',
    });
  }

  toggleStatus(occupant: Occupant): void {
    const action = occupant.occupantStatus === 'Active'
      ? this.svc.deactivateOccupant(occupant.occupantID)
      : this.svc.activateOccupant(occupant.occupantID);

    action.subscribe({
      next: () => this.loadOccupants(this.currentPage),
      error: err => (this.errorMsg = this.extractError(err, 'Failed to update occupant status.')),
    });
  }

  saveOccupant(): void {
    this.form.patchValue({ unitNumber: this.form.value.unitNumber?.toUpperCase() });
    if (this.form.invalid || this.saving) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.formError = '';
    const payload = this.form.value;
    const returnPage = this.editMode ? this.currentPage : 1;
    const request$ = this.editMode && this.selectedOccupantId
      ? this.svc.updateOccupant(this.selectedOccupantId, payload)
      : this.svc.createOccupant(payload);

    request$.subscribe({
      next: saved => {
        this.saving = false;
        this.successPopupMessage = `${saved.fullName || payload.fullName} saved successfully.`;
        this.showSuccessPopup = true;
        this.resetForm();
        this.loadOccupants(returnPage);
      },
      error: err => {
        this.saving = false;
        this.formError = this.extractError(err, 'Failed to save occupant record.');
      }
    });
  }

  resetForm(): void {
    this.editMode = false;
    this.selectedOccupantId = null;
    this.formError = '';
    this.form.reset({ occupantType: 'Tenant' });
  }

  deleteSelectedOccupant(): void {
    if (!this.editMode || !this.selectedOccupantId || this.deleting) return;

    const fullName = this.form.value.fullName || 'this occupant';
    const confirmed = window.confirm(`Delete ${fullName}? This record will be removed from the active occupant list.`);
    if (!confirmed) return;

    this.deleting = true;
    this.formError = '';

    this.svc.deleteOccupant(this.selectedOccupantId).subscribe({
      next: response => {
        this.deleting = false;
        this.successPopupMessage = response.message || `${fullName} deleted successfully.`;
        this.showSuccessPopup = true;
        this.resetForm();
        this.loadOccupants(this.currentPage);
      },
      error: err => {
        this.deleting = false;
        this.formError = this.extractError(err, 'Failed to delete occupant record.');
      }
    });
  }

  closeSuccessPopup(): void {
    this.showSuccessPopup = false;
    this.successPopupMessage = '';
  }

  private patternValidator(pattern: RegExp, errorKey: string): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) return null;
      return pattern.test(String(control.value).trim()) ? null : { [errorKey]: true };
    };
  }

  private optionalPatternValidator(pattern: RegExp, errorKey: string): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value || String(control.value).trim() === '') return null;
      return pattern.test(String(control.value).trim()) ? null : { [errorKey]: true };
    };
  }

  private extractError(err: any, fallback: string): string {
    if (err.error?.errors) {
      const first = Object.values(err.error.errors)[0] as string[] | undefined;
      if (first?.length) return first[0];
    }
    return err.error?.message || fallback;
  }
}





