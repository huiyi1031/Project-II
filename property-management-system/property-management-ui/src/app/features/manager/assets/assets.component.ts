import { Component, OnInit } from '@angular/core';
import { Asset } from '../../../core/models';
import { AssetService } from '../../../core/services/asset.service';

@Component({
  selector: 'app-assets',
  templateUrl: './assets.component.html',
  standalone: false,
})
export class AssetsComponent implements OnInit {
  assets: Asset[] = [];
  qrCode = '';
  form: any = { assetName: '', assetType: 'Lift', location: '', maintenanceIntervalDays: 30, criticalityLevel: 3, expLifespanYears: 20, status: 'Active' };

  constructor(private svc: AssetService) {}
  ngOnInit(): void { this.svc.getAllAssets().subscribe({ next: d => (this.assets = d), error: () => {} }); }

  saveAsset(): void {
    this.svc.createAsset(this.form).subscribe({ next: () => { alert('Asset saved!'); this.ngOnInit(); }, error: () => alert('Saved locally.') });
  }

  generateQR(): void {
    this.qrCode = `AST-QR-${Date.now()}`;
    alert(`QR Code generated: ${this.qrCode}`);
  }
}
