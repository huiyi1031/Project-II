import { Component, OnInit } from '@angular/core';
import { PropertyUnit } from '../../../core/models';
import { UnitService, CreateUnitDto, UpdateUnitDto, UnitFilterOptions } from '../../../core/services/unit.service';

@Component({
  selector: 'app-units',
  templateUrl: './units.component.html',
  standalone: false,
})
export class UnitsComponent implements OnInit {
  // ── Data ────────────────────────────────────────────────────────
  units: PropertyUnit[] = [];
  filteredUnits: PropertyUnit[] = [];
  filterOptions: UnitFilterOptions = { blocks: [], floors: [] };
  properties: { id: number; name: string }[] = [];

  // ── UI State ─────────────────────────────────────────────────────
  loading   = false;
  saving    = false;
  error     = '';
  success   = '';
  showModal = false;
  showDeleteConfirm = false;
  showDetailPanel  = false;
  editMode  = false;

  selectedUnit: PropertyUnit | null = null;
  selectedUnitDetail: any = null;

  // ── Filters ───────────────────────────────────────────────────────
  searchText    = '';
  filterBlock   = '';
  filterFloor   = '';
  filterType    = '';
  filterStatus  = '';
  filterMinSqft = '';
  filterMaxSqft = '';
  selectedPropertyId: number | undefined;

  readonly unitTypes = ['Studio', '1-Bedroom', '2-Bedroom', '3-Bedroom'];
  readonly statusOptions = ['Vacant', 'Occupied', 'Under Maintenance'];

  // ── Form ──────────────────────────────────────────────────────────
  form: CreateUnitDto = this.emptyForm();

  constructor(private unitSvc: UnitService) {}

  ngOnInit(): void { this.loadUnits(); }

  // ── Load ─────────────────────────────────────────────────────────
  loadUnits(): void {
    this.loading = true;
    this.error   = '';
    this.unitSvc.getAll({
      search:     this.searchText     || undefined,
      block:      this.filterBlock    || undefined,
      floorLevel: this.filterFloor    || undefined,
      unitType:   this.filterType     || undefined,
      status:     this.filterStatus   || undefined,
      minSqft:    this.filterMinSqft  ? +this.filterMinSqft : undefined,
      maxSqft:    this.filterMaxSqft  ? +this.filterMaxSqft : undefined,
      propertyId: this.selectedPropertyId ? +this.selectedPropertyId : undefined,
    }).subscribe({
      next: units => {
        this.units = units;
        this.filteredUnits = units;
        this.loading = false;

        // Derive unique property list from unit data
        const propMap = new Map<number, string>();
        units.forEach(u => {
          if (u.propertyId && u.propertyName)
            propMap.set(u.propertyId, u.propertyName);
        });
        this.properties = Array.from(propMap, ([id, name]) => ({ id, name }));

        // Load filter options for dropdowns
        if (this.selectedPropertyId)
          this.loadFilterOptions();
      },
      error: () => { this.error = 'Failed to load units.'; this.loading = false; }
    });
  }

  loadFilterOptions(): void {
    this.unitSvc.getFilterOptions(this.selectedPropertyId).subscribe({
      next: opts => this.filterOptions = opts,
      error: () => {}
    });
  }

  applyFilters(): void { this.loadUnits(); }

  clearFilters(): void {
    this.searchText   = '';
    this.filterBlock  = '';
    this.filterFloor  = '';
    this.filterType   = '';
    this.filterStatus = '';
    this.filterMinSqft = '';
    this.filterMaxSqft = '';
    this.selectedPropertyId = undefined;
    this.loadUnits();
  }

  onPropertyChange(): void {
    this.filterBlock = '';
    this.filterFloor = '';
    this.loadUnits();
    this.loadFilterOptions();
  }

  // ── Row Selection ─────────────────────────────────────────────────
  selectUnit(unit: PropertyUnit): void {
    this.selectedUnit = unit;
    this.showDetailPanel = true;
    this.unitSvc.getById(unit.unitId).subscribe({
      next: detail => this.selectedUnitDetail = detail,
      error: () => this.selectedUnitDetail = unit
    });
  }

  closeDetailPanel(): void {
    this.showDetailPanel = false;
    this.selectedUnit    = null;
    this.selectedUnitDetail = null;
  }

  // ── Create ────────────────────────────────────────────────────────
  openCreateModal(): void {
    this.editMode = false;
    this.form = this.emptyForm();
    if (this.selectedPropertyId) this.form.propertyId = this.selectedPropertyId;
    this.showModal = true;
    this.error = '';
    this.success = '';
  }

  // ── Edit ──────────────────────────────────────────────────────────
  openEditModal(unit: PropertyUnit): void {
    this.editMode    = true;
    this.selectedUnit = unit;
    this.form = {
      propertyId:   unit.propertyId,
      unitNumber:   unit.unitNumber,
      floorLevel:   unit.floorLevel,
      block:        unit.block,
      unitType:     unit.unitType || 'Studio',
      areaSqft:     unit.areaSqft,
      bedrooms:     unit.bedrooms,
      bathrooms:    unit.bathrooms,
      maxOccupants: unit.maxOccupants,
      status:       unit.status
    };
    this.showModal = true;
    this.error  = '';
    this.success = '';
  }

  // ── Save ──────────────────────────────────────────────────────────
  saveUnit(): void {
    if (!this.form.unitNumber?.trim()) { this.error = 'Unit number is required.'; return; }
    if (!this.form.propertyId)         { this.error = 'Please select a property.'; return; }

    this.saving = true;
    this.error  = '';

    if (this.editMode && this.selectedUnit) {
      const dto: UpdateUnitDto = {
        unitNumber:   this.form.unitNumber,
        floorLevel:   this.form.floorLevel,
        block:        this.form.block,
        unitType:     this.form.unitType,
        areaSqft:     this.form.areaSqft,
        bedrooms:     this.form.bedrooms,
        bathrooms:    this.form.bathrooms,
        maxOccupants: this.form.maxOccupants,
        status:       this.form.status
      };
      this.unitSvc.update(this.selectedUnit.unitId, dto).subscribe({
        next: () => { this.saving = false; this.showModal = false; this.success = 'Unit updated successfully!'; this.loadUnits(); },
        error: err => { this.saving = false; this.error = err.error?.message || 'Update failed.'; }
      });
    } else {
      this.unitSvc.create(this.form).subscribe({
        next: () => { this.saving = false; this.showModal = false; this.success = 'Unit created successfully!'; this.loadUnits(); },
        error: err => { this.saving = false; this.error = err.error?.message || 'Create failed.'; }
      });
    }
  }

  // ── Delete ────────────────────────────────────────────────────────
  confirmDelete(unit: PropertyUnit): void {
    this.selectedUnit     = unit;
    this.showDeleteConfirm = true;
  }

  deleteUnit(): void {
    if (!this.selectedUnit) return;
    this.saving = true;
    this.unitSvc.delete(this.selectedUnit.unitId).subscribe({
      next: () => {
        this.saving = false;
        this.showDeleteConfirm = false;
        this.showDetailPanel   = false;
        this.selectedUnit      = null;
        this.success = 'Unit deleted successfully!';
        this.loadUnits();
      },
      error: err => {
        this.saving = false;
        this.showDeleteConfirm = false;
        this.error = err.error?.message || 'Delete failed.';
      }
    });
  }

  closeModal():  void { this.showModal = false; this.error = ''; }
  cancelDelete(): void { this.showDeleteConfirm = false; this.selectedUnit = null; }
  dismissAlert(): void { this.error = ''; this.success = ''; }

  // ── Helpers ───────────────────────────────────────────────────────
  emptyForm(): CreateUnitDto {
    return {
      propertyId:   this.selectedPropertyId || 0,
      unitNumber:   '',
      floorLevel:   '',
      block:        'A',
      unitType:     'Studio',
      areaSqft:     520,
      bedrooms:     1,
      bathrooms:    1,
      maxOccupants: 2,
      status:       'Vacant'
    };
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'vacant':    return 'badge-vacant';
      case 'occupied':  return 'badge-occupied';
      default:          return 'badge-other';
    }
  }

  getTypeIcon(type: string): string {
    switch (type) {
      case 'Studio':    return 'S';
      case '1-Bedroom': return '1B';
      case '2-Bedroom': return '2B';
      case '3-Bedroom': return '3B';
      default:          return 'U';
    }
  }

  onUnitTypeChange(): void {
    // Auto-fill sensible defaults when unit type changes
    switch (this.form.unitType) {
      case 'Studio':    this.form.areaSqft = 520;  this.form.bedrooms = 1; this.form.bathrooms = 1; this.form.maxOccupants = 2; break;
      case '1-Bedroom': this.form.areaSqft = 720;  this.form.bedrooms = 1; this.form.bathrooms = 1; this.form.maxOccupants = 4; break;
      case '2-Bedroom': this.form.areaSqft = 980;  this.form.bedrooms = 2; this.form.bathrooms = 2; this.form.maxOccupants = 6; break;
      case '3-Bedroom': this.form.areaSqft = 1350; this.form.bedrooms = 3; this.form.bathrooms = 2; this.form.maxOccupants = 8; break;
    }
  }
}
