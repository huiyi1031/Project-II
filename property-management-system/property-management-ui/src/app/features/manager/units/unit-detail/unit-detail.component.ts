import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { UnitService } from '../../../../core/services/unit.service';

@Component({
  selector: 'app-unit-detail',
  templateUrl: './unit-detail.component.html',
  standalone: false
})
export class UnitDetailComponent implements OnInit {
  unitId!: number;
  unitDetail: any = null;
  loading = false;
  error = '';

  showDeleteConfirm = false;
  showCannotDeleteModal = false;
  deleting = false;

  constructor(
    private unitSvc: UnitService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.unitId = +idParam;
      this.loadUnitDetails();
    } else {
      this.error = 'Invalid Unit ID.';
    }
  }

  loadUnitDetails(): void {
    this.loading = true;
    this.unitSvc.getById(this.unitId).subscribe({
      next: detail => {
        this.unitDetail = detail;
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load unit details.';
        this.loading = false;
      }
    });
  }

  get owners() {
    return this.unitDetail?.activeContracts?.filter((c: any) => c.occupantType === 'Owner') || [];
  }

  get tenants() {
    return this.unitDetail?.activeContracts?.filter((c: any) => c.occupantType === 'Tenant') || [];
  }

  get residents() {
    return this.unitDetail?.activeContracts?.filter((c: any) => c.occupantType === 'Resident') || [];
  }

  getTypeIcon(type: string): string {
    if (type.includes('Studio')) return '🏢';
    if (type.includes('1-Bed'))  return '🛏️';
    if (type.includes('2-Bed'))  return '🛋️';
    if (type.includes('3-Bed'))  return '🏠';
    return '🏢';
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Vacant': return 'badge-vacant';
      case 'Occupied': return 'badge-occupied';
      default: return 'badge-other';
    }
  }

  openEdit(): void {
    this.router.navigate(['/manager/units', this.unitId, 'edit']);
  }

  back(): void {
    this.router.navigate(['/manager/units']);
  }

  // ── Delete ────────────────────────────────────────────────────────
  confirmDelete(): void {
    if (this.unitDetail?.status?.toLowerCase() !== 'vacant') {
      this.showCannotDeleteModal = true;
      return;
    }
    this.showDeleteConfirm = true;
  }

  closeCannotDeleteModal(): void {
    this.showCannotDeleteModal = false;
  }

  cancelDelete(): void {
    this.showDeleteConfirm = false;
  }

  deleteUnit(): void {
    this.deleting = true;
    this.unitSvc.delete(this.unitId).subscribe({
      next: () => {
        this.deleting = false;
        this.showDeleteConfirm = false;
        this.router.navigate(['/manager/units']);
      },
      error: err => {
        this.deleting = false;
        this.error = err.error?.message || 'Delete failed.';
        this.showDeleteConfirm = false;
      }
    });
  }
}
