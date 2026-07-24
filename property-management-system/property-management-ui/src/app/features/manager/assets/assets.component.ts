import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AssetService } from '../../../core/services/asset.service';
import { Asset } from '../../../core/models';

@Component({
  selector: 'app-assets',
  templateUrl: './assets.component.html',
  standalone: false
})
export class AssetsComponent implements OnInit {
  assets: Asset[] = [];
  properties: { id: number; name: string }[] = [];
  
  loading = false;
  error = '';
  success = '';
  showDeactivateConfirm = false;
  selectedAsset: Asset | null = null;
  deactivating = false;

  searchText = '';
  filterType = '';
  filterStatus = '';
  selectedPropertyId: number | undefined;

  readonly assetTypes = [
    'Elevator', 'HVAC', 'Water Pump', 'Fire System', 'Generator', 
    'Plumbing', 'Electrical Panel', 'CCTV & Security', 'Access Control', 'Other'
  ];

  constructor(
    private assetSvc: AssetService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadAssets();
  }

  loadAssets(): void {
    this.loading = true;
    this.error = '';
    this.assetSvc.getAll({
      search: this.searchText || undefined,
      assetType: this.filterType || undefined,
      status: this.filterStatus || undefined,
      propertyId: this.selectedPropertyId,
    }).subscribe({
      next: (assets) => {
        this.assets = assets;
        this.loading = false;

        const propMap = new Map<number, string>();
        assets.forEach(a => {
          if (a.propertyId && a.propertyName) propMap.set(a.propertyId, a.propertyName);
        });
        this.properties = Array.from(propMap, ([id, name]) => ({ id, name }));
      },
      error: () => {
        this.error = 'Failed to load assets.';
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.loadAssets();
  }

  clearFilters(): void {
    this.searchText = '';
    this.filterType = '';
    this.filterStatus = '';
    this.selectedPropertyId = undefined;
    this.loadAssets();
  }

  viewAsset(asset: Asset): void {
    this.router.navigate(['/manager/assets', asset.assetId]);
  }

  editAsset(asset: Asset, event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/manager/assets', asset.assetId, 'edit']);
  }

  createAsset(): void {
    this.router.navigate(['/manager/assets/create']);
  }

  confirmDeactivate(asset: Asset, event: Event): void {
    event.stopPropagation();
    this.selectedAsset = asset;
    this.showDeactivateConfirm = true;
  }

  cancelDeactivate(): void {
    this.showDeactivateConfirm = false;
    this.selectedAsset = null;
  }

  deactivateAsset(): void {
    if (!this.selectedAsset) return;
    this.deactivating = true;
    this.assetSvc.deactivate(this.selectedAsset.assetId).subscribe({
      next: () => {
        this.deactivating = false;
        this.showDeactivateConfirm = false;
        this.success = 'Asset deactivated successfully.';
        this.loadAssets();
      },
      error: (err) => {
        this.deactivating = false;
        this.showDeactivateConfirm = false;
        this.error = err.error?.message || 'Deactivation failed.';
      }
    });
  }

  dismissAlert(): void {
    this.error = '';
    this.success = '';
  }

  getStatusClass(status?: string): string {
    return status === 'Active' ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-800';
  }

  getTypeIcon(type?: string): string {
    const icons: Record<string, string> = {
      'Elevator': 'fa-arrows-alt-v',
      'HVAC': 'fa-wind',
      'Water Pump': 'fa-water',
      'Fire System': 'fa-fire-extinguisher',
      'Generator': 'fa-bolt',
      'Plumbing': 'fa-wrench',
      'Electrical Panel': 'fa-plug',
      'CCTV & Security': 'fa-video',
      'Access Control': 'fa-id-badge',
      'Other': 'fa-box'
    };
    return icons[type || ''] || 'fa-box';
  }
}
