import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MaintenanceService } from '../../../core/services/maintenance.service';
import { OccupantService } from '../../../core/services/occupant.service';
import { PropertyUnit, MaintenanceRequest } from '../../../core/models';

@Component({
  selector: 'app-create-request',
  templateUrl: './create-request.component.html',
  standalone: false,
})
export class CreateRequestComponent implements OnInit {
  form!: FormGroup;
  units: PropertyUnit[] = [];
  categories: string[] = [];
  fileName = '';
  isLoading = false;
  successMsg = '';
  errorMsg = '';
  editId: number | null = null;
  isEditing = false;

  constructor(
    private fb: FormBuilder,
    private maintenanceSvc: MaintenanceService,
    private occupantSvc: OccupantService,
    private router: Router,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    this.form = this.fb.group({
      requestTitle: ['', Validators.required],
      issueCategory: ['', Validators.required],
      description: ['', Validators.required],
      unitId: ['', Validators.required],
    });

    // Load dynamic categories
    this.maintenanceSvc.getCategories().subscribe({
      next: (data) => {
        this.categories = data;
      },
      error: () => { }
    });

    // Load units for dropdown and pre-select the first one
    this.occupantSvc.getMyUnits().subscribe({
      next: (data) => {
        this.units = data;
        if (data.length > 0 && !this.isEditing) {
          this.form.patchValue({ unitId:  data[0].unitID });
        }
      },
      error: () => { }
    });

    // Check if editing
    this.route.queryParams.subscribe(params => {
      if (params['edit']) {
        this.editId = +params['edit'];
        this.isEditing = true;
        this.loadEditData(this.editId);
      }
    });
  }

  loadEditData(id: number): void {
    this.maintenanceSvc.getRequestById(id).subscribe({
      next: (req) => {
        this.form.patchValue({
          requestTitle: req.requestTitle,
          issueCategory: req.issueCategory,
          description: req.description,
          unitId: req.unitID
        });
      },
      error: () => {
        this.errorMsg = 'Failed to load request for editing.';
      }
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

    if (this.isEditing && this.editId) {
      this.maintenanceSvc.updateRequest(this.editId, formData).subscribe({
        next: () => {
          this.isLoading = false;
          this.successMsg = 'Maintenance request updated successfully! Redirecting...';
          setTimeout(() => this.router.navigate(['/tenant/track-request']), 2000);
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMsg = err.error?.message || 'Failed to update request. Please try again.';
        }
      });
    } else {
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
  }
}
