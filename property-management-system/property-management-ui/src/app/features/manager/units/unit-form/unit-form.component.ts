import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { UnitService, CreateUnitDto, UpdateUnitDto } from '../../../../core/services/unit.service';

@Component({
  selector: 'app-unit-form',
  templateUrl: './unit-form.component.html',
  standalone: false
})
export class UnitFormComponent implements OnInit {
  editMode = false;
  unitId!: number;
  
  form: CreateUnitDto = {
    propertyId: 0,
    unitNumber: '',
    floorLevel: '',
    block: '',
    unitType: 'Studio',
    areaSqft: undefined,
    bedrooms: undefined,
    bathrooms: undefined,
    status: 'Vacant'
  };

  loading = false;
  saving = false;
  error = '';
  
  readonly unitTypes = ['Studio', '1-Bedroom', '2-Bedroom', '3-Bedroom'];
  readonly statusOptions = ['Vacant', 'Occupied', 'Under Maintenance'];

  constructor(
    private unitSvc: UnitService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.editMode = true;
      this.unitId = +idParam;
      this.loadUnitForEdit();
    } else {
      // Create mode
      this.editMode = false;
      const qPropId = this.route.snapshot.queryParamMap.get('propertyId');
      if (qPropId) {
        this.form.propertyId = +qPropId;
      } else {
        // Fallback: fetch a unit to determine the property ID
        this.loading = true;
        this.unitSvc.getAll({}).subscribe({
          next: units => {
            if (units.length > 0) this.form.propertyId = units[0].propertyId || 0;
            this.loading = false;
          },
          error: () => this.loading = false
        });
      }
    }
  }

  loadUnitForEdit(): void {
    this.loading = true;
    this.unitSvc.getById(this.unitId).subscribe({
      next: (unit: any) => {
        this.form = {
          propertyId: unit.propertyId,
          unitNumber: unit.unitNumber,
          floorLevel: unit.floorLevel,
          block: unit.block,
          unitType: unit.unitType || 'Studio',
          areaSqft: unit.areaSqft,
          bedrooms: unit.bedrooms,
          bathrooms: unit.bathrooms,
          status: unit.status
        };
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load unit details.';
        this.loading = false;
      }
    });
  }

  onUnitTypeChange(): void {
    const t = this.form.unitType;
    if (t === 'Studio') { this.form.bedrooms = 1; this.form.bathrooms = 1; }
    else if (t === '1-Bedroom') { this.form.bedrooms = 1; this.form.bathrooms = 1; }
    else if (t === '2-Bedroom') { this.form.bedrooms = 2; this.form.bathrooms = 2; }
    else if (t === '3-Bedroom') { this.form.bedrooms = 3; this.form.bathrooms = 2; }
  }

  saveUnit(): void {
    if (!this.form.unitNumber?.trim()) { this.error = 'Unit number is required.'; return; }
    
    this.saving = true;
    this.error = '';

    if (this.editMode) {
      const dto: UpdateUnitDto = {
        unitNumber: this.form.unitNumber,
        floorLevel: this.form.floorLevel,
        block: this.form.block,
        unitType: this.form.unitType,
        areaSqft: this.form.areaSqft,
        bedrooms: this.form.bedrooms,
        bathrooms: this.form.bathrooms,
        status: this.form.status
      };
      this.unitSvc.update(this.unitId, dto).subscribe({
        next: () => this.router.navigate(['/manager/units']),
        error: err => { this.error = err.error?.message || 'Update failed.'; this.saving = false; }
      });
    } else {
      this.unitSvc.create(this.form).subscribe({
        next: () => this.router.navigate(['/manager/units']),
        error: err => { this.error = err.error?.message || 'Create failed.'; this.saving = false; }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/manager/units']);
  }
}
