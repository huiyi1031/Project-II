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
  errorMsg   = '';

  constructor(
    private fb: FormBuilder,
    private maintenanceSvc: MaintenanceService,
    private occupantSvc: OccupantService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      requestTitle:          ['', Validators.required],
      issueCategory:         ['', Validators.required],
      description:           ['', Validators.required],
      priorityLevel:         ['Medium', Validators.required],
      unitID:                [1, Validators.required],
      preferredScheduleDate: [''],
    });

    // Load units for dropdown
    this.occupantSvc.getAllUnits().subscribe({
      next: (data) => (this.units = data),
      error: () => {}
    });
  }

  get f() { return this.form.controls; }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.fileName = input.files[0].name;
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.isLoading  = true;
    this.successMsg = '';
    this.errorMsg   = '';

    this.maintenanceSvc.createRequest(this.form.value).subscribe({
      next: () => {
        this.isLoading  = false;
        this.successMsg = 'Maintenance request submitted successfully! Redirecting...';
        setTimeout(() => this.router.navigate(['/tenant/track-request']), 2000);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMsg  = err.error?.message || 'Failed to submit request. Please try again.';
      }
    });
  }

  resetForm(): void {
    this.form.reset({ priorityLevel: 'Medium', unitID: 1 });
    this.fileName = '';
    this.successMsg = '';
    this.errorMsg   = '';
  }
}
