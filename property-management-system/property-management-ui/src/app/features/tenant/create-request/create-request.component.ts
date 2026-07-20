import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MaintenanceService } from '../../../core/services/maintenance.service';
import { OccupantService } from '../../../core/services/occupant.service';
import { PropertyUnit } from '../../../core/models';

@Component({
  selector: 'app-create-request',
  templateUrl: './create-request.component.html',
  standalone: false,
})
export class CreateRequestComponent implements OnInit {
  form!: FormGroup;
  units: PropertyUnit[] = [];
  fileName = '';
  isLoading = false;
  successMsg = '';
  errorMsg = '';

  constructor(
    private fb: FormBuilder,
    private maintenanceSvc: MaintenanceService,
    private occupantSvc: OccupantService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.form = this.fb.group({
      requestTitle: ['', Validators.required],
      issueCategory: ['', Validators.required],
      description: ['', Validators.required],
      unitId: ['', Validators.required],
    });

    // Load units for dropdown and pre-select the first one
    this.occupantSvc.getAllUnits().subscribe({
      next: (data) => {
        this.units = data;
        if (data.length > 0) {
          this.form.patchValue({ unitId:  data[0].unitID });
        }
      },
      error: () => { }
    });
  }

  get f() { return this.form.controls; }

  fileToUpload: File | null = null;

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.fileToUpload = input.files[0];
      this.fileName = this.fileToUpload.name;
    } else {
      this.fileToUpload = null;
      this.fileName = '';
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.isLoading = true;
    this.successMsg = '';
    this.errorMsg = '';

    const formData = new FormData();
    formData.append('Title', this.form.value.requestTitle);
    formData.append('IssueCategory', this.form.value.issueCategory);
    formData.append('Description', this.form.value.description);
    formData.append('UnitId', String(this.form.value.unitId));

    if (this.fileToUpload) {
      formData.append('Image', this.fileToUpload);
    }

    this.maintenanceSvc.createRequest(formData).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMsg = 'Maintenance request submitted successfully! Redirecting...';
        setTimeout(() => this.router.navigate(['/tenant/track-request']), 2000);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMsg = err.error?.message || 'Failed to submit request. Please try again.';
      }
    });
  }

  resetForm(): void {
    const firstUnitId = this.units.length > 0 ?  this.units[0].unitID : '';
    this.form.reset({ unitID: firstUnitId });
    this.fileName = '';
    this.fileToUpload = null;
    this.successMsg = '';
    this.errorMsg = '';
  }
}
