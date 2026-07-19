import { Component, OnInit } from '@angular/core';
import { Asset, AssetMaintenanceHistory } from '../../../core/models';
import { AssetService, CreateAssetDto } from '../../../core/services/asset.service';

@Component({
  selector: 'app-assets',
  templateUrl: './assets.component.html',
  standalone: false,
})
export class AssetsComponent implements OnInit {
  // â”€â”€ Data â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  assets:  Asset[]  = [];
  history: AssetMaintenanceHistory[] = [];
  properties: { id: number; name: string }[] = [];

  // â”€â”€ UI State â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  loading    = false;
  saving     = false;
  error      = '';
  success    = '';
  showModal  = false;
  showHistoryModal = false;
  showDeactivateConfirm = false;
  editMode   = false;
  historyLoading = false;

  selectedAsset: Asset | null = null;
  selectedAssetDetail: any = null;

  // â”€â”€ Filters â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  searchText         = '';
  filterType         = '';
  filterStatus       = '';
  selectedPropertyId: number | undefined;

  readonly assetTypes  = ['Elevator', 'HVAC', 'Water Pump', 'Fire System', 'Generator', 'Plumbing', 'Electrical', 'Security', 'Other'];
  readonly statusOpts  = ['Active', 'Inactive'];
  readonly maintenanceTypes = [
    { value: 1, label: 'Preventive' },
    { value: 0, label: 'Corrective' },
    { value: 2, label: 'Inspection' }
  ];

  // â”€â”€ Forms â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  form: CreateAssetDto = this.emptyAssetForm();

  historyForm = {
    maintenanceType: 1,
    description:     '',
    cost:            0,
    maintenanceDate: new Date().toISOString().split('T')[0],
    resultStatus:    'Completed',
    performedBy:     ''
  };

  constructor(private assetSvc: AssetService) {}

  ngOnInit(): void { this.loadAssets(); }

  // â”€â”€ Load â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  loadAssets(): void {
    this.loading = true;
    this.error   = '';
    this.assetSvc.getAll({
      search:     this.searchText || undefined,
      assetType:  this.filterType || undefined,
      status:     this.filterStatus || undefined,
      propertyId: this.selectedPropertyId,
    }).subscribe({
      next: assets => {
        this.assets  = assets;
        this.loading = false;

        // Derive properties list
        const propMap = new Map<number, string>();
        assets.forEach(a => {
          if (a.propertyId && a.propertyName)
            propMap.set(a.propertyId, a.propertyName);
        });
        this.properties = Array.from(propMap, ([id, name]) => ({ id, name }));
      },
      error: () => { this.error = 'Failed to load assets.'; this.loading = false; }
    });
  }

  applyFilters(): void { this.loadAssets(); }
  clearFilters(): void {
    this.searchText = ''; this.filterType = ''; this.filterStatus = '';
    this.selectedPropertyId = undefined;
    this.loadAssets();
  }

  // â”€â”€ Select / View Detail â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  selectAsset(asset: Asset): void {
    this.selectedAsset = asset;
    this.assetSvc.getById(asset.assetId).subscribe({
      next: detail => { this.selectedAssetDetail = detail; },
      error: () => { this.selectedAssetDetail = asset; }
    });
  }

  closeDetail(): void { this.selectedAsset = null; this.selectedAssetDetail = null; }

  // â”€â”€ Create â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  openCreateModal(): void {
    this.editMode = false;
    this.form = this.emptyAssetForm();
    if (this.selectedPropertyId) this.form.propertyId = this.selectedPropertyId;
    this.showModal = true; this.error = ''; this.success = '';
  }

  // â”€â”€ Edit â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  openEditModal(asset: Asset): void {
    this.editMode     = true;
    this.selectedAsset = asset;
    this.form = {
      propertyId:              asset.propertyId,
      assetName:               asset.assetName,
      assetType:               asset.assetType,
      location:                asset.location,
      installationDate:        asset.installationDate?.split('T')[0] || '',
      manufacturer:            asset.manufacturer,
      modelNumber:             asset.modelNumber,
      expLifespanYears:        asset.expLifespanYears,
      maintenanceIntervalDays: asset.maintenanceIntervalDays,
      supplierName:            asset.supplierName,
      warrantyExpiryDate:      asset.warrantyExpiryDate?.split('T')[0] || ''
    };
    this.showModal = true; this.error = ''; this.success = '';
  }

  // â”€â”€ Save Asset â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  saveAsset(): void {
    if (!this.form.assetName?.trim()) { this.error = 'Asset name is required.'; return; }
    if (!this.form.propertyId)        { this.error = 'Please select a property.'; return; }
    if (!this.form.installationDate)  { this.error = 'Installation date is required.'; return; }

    this.saving = true; this.error = '';

    if (this.editMode && this.selectedAsset) {
      const dto = { ...this.form, status: this.selectedAsset.status };
      this.assetSvc.update(this.selectedAsset.assetId, dto as any).subscribe({
        next: () => { this.saving = false; this.showModal = false; this.success = 'Asset updated!'; this.loadAssets(); },
        error: err => { this.saving = false; this.error = err.error?.message || 'Update failed.'; }
      });
    } else {
      this.assetSvc.create(this.form).subscribe({
        next: (res) => {
          this.saving = false; this.showModal = false;
          this.success = `Asset '${this.form.assetName}' registered! QR: ${res.qrCode}`;
          this.loadAssets();
        },
        error: err => { this.saving = false; this.error = err.error?.message || 'Create failed.'; }
      });
    }
  }

  // â”€â”€ Deactivate â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  confirmDeactivate(asset: Asset): void {
    this.selectedAsset = asset;
    this.showDeactivateConfirm = true;
  }

  deactivateAsset(): void {
    if (!this.selectedAsset) return;
    this.saving = true;
    this.assetSvc.deactivate(this.selectedAsset.assetId).subscribe({
      next: () => {
        this.saving = false; this.showDeactivateConfirm = false;
        this.success = 'Asset deactivated. Historical records preserved.';
        this.closeDetail(); this.loadAssets();
      },
      error: err => { this.saving = false; this.showDeactivateConfirm = false; this.error = err.error?.message || 'Deactivation failed.'; }
    });
  }

  cancelDeactivate(): void { this.showDeactivateConfirm = false; }

  // â”€â”€ Maintenance History Modal â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  openHistoryModal(asset: Asset): void {
    this.selectedAsset = asset;
    this.historyLoading = true;
    this.historyForm = {
      maintenanceType: 1, description: '', cost: 0,
      maintenanceDate: new Date().toISOString().split('T')[0],
      resultStatus: 'Completed', performedBy: ''
    };
    this.assetSvc.getHistory(asset.assetId).subscribe({
      next: h => { this.history = h; this.historyLoading = false; },
      error: () => { this.history = []; this.historyLoading = false; }
    });
    this.showHistoryModal = true;
  }

  addHistoryRecord(): void {
    if (!this.selectedAsset) return;
    this.saving = true;
    this.assetSvc.addHistory(this.selectedAsset.assetId, {
      ...this.historyForm,
      cost: this.historyForm.cost || 0
    }).subscribe({
      next: (res) => {
        this.saving  = false;
        this.success = `Maintenance record added. Next due: ${res.nextMaintenanceDueDate ? new Date(res.nextMaintenanceDueDate).toLocaleDateString() : 'N/A'}`;
        this.showHistoryModal = false;
        this.loadAssets();
      },
      error: err => { this.saving = false; this.error = err.error?.message || 'Failed to add record.'; }
    });
  }

  closeModal():       void { this.showModal = false; this.error = ''; }
  closeHistoryModal(): void { this.showHistoryModal = false; this.error = ''; }
  dismissAlert():     void { this.error = ''; this.success = ''; }

  // â”€â”€ QR â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  getQrUrl(qrCode: string | undefined): string {
    if (!qrCode) return '';
    return this.assetSvc.getQrImageUrl(qrCode, 120);
  }

  // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  emptyAssetForm(): CreateAssetDto {
    return {
      propertyId: this.selectedPropertyId || 0,
      assetName: '', assetType: 'Elevator', location: '',
      installationDate: new Date().toISOString().split('T')[0],
      manufacturer: '', modelNumber: '', expLifespanYears: 15,
      maintenanceIntervalDays: 30, supplierName: '', warrantyExpiryDate: ''
    };
  }

  getStatusClass(status?: string): string {
    return status === 'Active' ? 'badge-active' : 'badge-inactive';
  }

  getTypeIcon(type?: string): string {
    const icons: Record<string, string> = {
      'Elevator': 'ðŸ›—', 'HVAC': 'â„ï¸', 'Water Pump': 'ðŸ’§',
      'Fire System': 'ðŸš’', 'Generator': 'âš¡', 'Plumbing': 'ðŸ”§',
      'Electrical': 'ðŸ’¡', 'Security': 'ðŸ”’', 'Other': 'âš™ï¸'
    };
    return icons[type || ''] || 'âš™ï¸';
  }

  getDaysUntilDue(nextDue?: string): number | null {
    if (!nextDue) return null;
    const diff = new Date(nextDue).getTime() - Date.now();
    return Math.ceil(diff / (1000 * 60 * 60 * 24));
  }

  getDueClass(nextDue?: string): string {
    const days = this.getDaysUntilDue(nextDue);
    if (days === null) return '';
    if (days <= 0)  return 'due-overdue';
    if (days <= 14) return 'due-soon';
    return 'due-ok';
  }
}

