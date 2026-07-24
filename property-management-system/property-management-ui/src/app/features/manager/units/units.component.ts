import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { PropertyUnit } from '../../../core/models';
import { UnitService, UnitFilterOptions } from '../../../core/services/unit.service';

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

  // ── UI State ─────────────────────────────────────────────────────
  loading   = false;
  error     = '';
  success   = '';
  
  showFilters = true;

  // ── Filters & Sorting ───────────────────────────────────────────────
  searchText    = '';
  sortBy        = 'floor_asc';
  filterBlock   = '';
  filterFloor   = '';
  filterType    = '';
  filterStatus  = '';
  filterMinSqft = '';
  filterMaxSqft = '';

  readonly unitTypes = ['Studio', '1-Bedroom', '2-Bedroom', '3-Bedroom'];
  readonly statusOptions = ['Vacant', 'Occupied', 'Under Maintenance'];

  constructor(private unitSvc: UnitService, private router: Router) {}

  ngOnInit(): void { 
    this.loadUnits(); 
    this.loadFilterOptions();
  }

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
      maxSqft:    this.filterMaxSqft  ? +this.filterMaxSqft : undefined
    }).subscribe({
      next: units => {
        this.units = units;
        this.filteredUnits = [...units];
        this.applySort();
        this.loading = false;
      },
      error: () => { this.error = 'Failed to load units.'; this.loading = false; }
    });
  }

  loadFilterOptions(): void {
    this.unitSvc.getFilterOptions(undefined).subscribe({
      next: opts => this.filterOptions = opts,
      error: () => {}
    });
  }

  applyFilters(): void { this.loadUnits(); }

  applySort(): void {
    if (!this.filteredUnits) return;
    this.filteredUnits.sort((a, b) => {
      if (this.sortBy === 'unit_asc') {
        return (a.unitNumber || '').localeCompare(b.unitNumber || '');
      } else if (this.sortBy === 'unit_desc') {
        return (b.unitNumber || '').localeCompare(a.unitNumber || '');
      } else if (this.sortBy === 'floor_asc') {
        const floorA = parseInt(a.floorLevel || '0', 10) || 0;
        const floorB = parseInt(b.floorLevel || '0', 10) || 0;
        return floorA - floorB;
      } else if (this.sortBy === 'floor_desc') {
        const floorA = parseInt(a.floorLevel || '0', 10) || 0;
        const floorB = parseInt(b.floorLevel || '0', 10) || 0;
        return floorB - floorA;
      }
      return 0;
    });
  }

  clearFilters(): void {
    this.searchText   = '';
    this.filterBlock  = '';
    this.filterFloor  = '';
    this.filterType   = '';
    this.filterStatus = '';
    this.filterMinSqft = '';
    this.filterMaxSqft = '';
    this.loadUnits();
  }
  
  toggleFilters(): void {
    this.showFilters = !this.showFilters;
  }
  
  exportCsv(): void {
    if (this.filteredUnits.length === 0) return;
    const header = ['Unit No', 'Block', 'Floor', 'Type', 'Size(sqft)', 'Bedrooms', 'Bathrooms', 'Status'].join(',');
    const rows = this.filteredUnits.map(u => 
      [u.unitNumber, u.block || '', u.floorLevel || '', u.unitType || '', u.areaSqft || '', u.bedrooms || '', u.bathrooms || '', u.status].join(',')
    );
    const csvContent = [header, ...rows].join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', 'Property_Units_Export.csv');
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  // ── Actions ───────────────────────────────────────────────────────
  selectUnit(unit: PropertyUnit): void {
    this.router.navigate(['/manager/units', unit.unitId]);
  }

  openCreateModal(): void {
    this.router.navigate(['/manager/units/create']);
  }

  openEditModal(unit: PropertyUnit): void {
    this.router.navigate(['/manager/units', unit.unitId, 'edit']);
  }

  dismissAlert(): void { this.error = ''; this.success = ''; }

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
}
