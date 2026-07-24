import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AssetService } from '../../../../core/services/asset.service';
import { Asset } from '../../../../core/models';

@Component({
  selector: 'app-asset-detail',
  templateUrl: './asset-detail.component.html',
  standalone: false
})
export class AssetDetailComponent implements OnInit {
  assetId!: number;
  asset: any;
  loading = true;
  error = '';
  qrCodeUrl = '';

  constructor(
    private assetSvc: AssetService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.assetId = +id;
      this.loadAsset();
    }
  }

  loadAsset(): void {
    this.loading = true;
    this.assetSvc.getById(this.assetId).subscribe({
      next: (data) => {
        this.asset = data;
        // Generate the URL that the QR code will point to
        this.qrCodeUrl = window.location.origin + '/manager/assets/' + this.assetId;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load asset details.';
        this.loading = false;
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/manager/assets']);
  }

  editAsset(): void {
    this.router.navigate(['/manager/assets', this.assetId, 'edit']);
  }

  addMaintenance(): void {
    // Navigate to proactive maintenance and pass the asset ID in query params
    this.router.navigate(['/manager/proactive'], { queryParams: { assetId: this.assetId, createMode: true } });
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
