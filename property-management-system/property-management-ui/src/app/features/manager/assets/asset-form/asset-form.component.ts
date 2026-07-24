import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AssetService, CreateAssetDto } from '../../../../core/services/asset.service';

@Component({
  selector: 'app-asset-form',
  templateUrl: './asset-form.component.html',
  standalone: false
})
export class AssetFormComponent implements OnInit {
  isEditMode = false;
  assetId!: number;
  loading = false;
  saving = false;
  error = '';

  form: CreateAssetDto = {
    propertyId: 0,
    assetName: '',
    assetType: 'Elevator',
    location: '',
    installationDate: new Date().toISOString().split('T')[0],
    manufacturer: '',
    modelNumber: '',
    expLifespanYears: 15,
    maintenanceIntervalDays: 30,
    supplierName: '',
    warrantyExpiryDate: ''
  };

  readonly assetTypes = [
    'Elevator', 'HVAC', 'Water Pump', 'Fire System', 'Generator', 
    'Plumbing', 'Electrical Panel', 'CCTV & Security', 'Access Control', 'Other'
  ];

  constructor(
    private assetSvc: AssetService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEditMode = true;
      this.assetId = +idParam;
      this.loadAsset();
    }
  }

  loadAsset(): void {
    this.loading = true;
    this.assetSvc.getById(this.assetId).subscribe({
      next: (asset) => {
        this.form = {
          propertyId: asset.propertyId,
          assetName: asset.assetName,
          assetType: asset.assetType,
          location: asset.location,
          installationDate: asset.installationDate?.split('T')[0] || '',
          manufacturer: asset.manufacturer,
          modelNumber: asset.modelNumber,
          expLifespanYears: asset.expLifespanYears,
          maintenanceIntervalDays: asset.maintenanceIntervalDays,
          supplierName: asset.supplierName,
          warrantyExpiryDate: asset.warrantyExpiryDate?.split('T')[0] || ''
        };
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load asset details.';
        this.loading = false;
      }
    });
  }

  save(): void {
    if (!this.form.assetName?.trim()) { this.error = 'Asset Name is required.'; return; }
    if (!this.form.installationDate) { this.error = 'Installation Date is required.'; return; }

    this.saving = true;
    this.error = '';

    if (this.isEditMode) {
      this.assetSvc.update(this.assetId, this.form as any).subscribe({
        next: () => {
          this.router.navigate(['/manager/assets']);
        },
        error: (err) => {
          this.error = err.error?.message || 'Update failed.';
          this.saving = false;
        }
      });
    } else {
      this.assetSvc.create(this.form).subscribe({
        next: () => {
          this.router.navigate(['/manager/assets']);
        },
        error: (err) => {
          this.error = err.error?.message || 'Create failed.';
          this.saving = false;
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/manager/assets']);
  }
}
