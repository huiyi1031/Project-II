import { Component, OnInit } from '@angular/core';
import { Asset } from '../../../core/models';
import { AssetService } from '../../../core/services/asset.service';

@Component({
  selector: 'app-proactive',
  templateUrl: './proactive.component.html',
  standalone: false,
})
export class ProactiveComponent implements OnInit {
  riskAssets: Asset[] = [];
  dateFrom = ''; dateTo = '';
  days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
  calCells: { day: number; tags: string[] }[] = [];

  constructor(private svc: AssetService) {}

  ngOnInit(): void {
    this.buildCalendar();
    // Load active assets — show those with upcoming maintenance (risk concept removed)
    this.svc.getAll({ status: 'Active' }).subscribe({ next: d => (this.riskAssets = d), error: () => {} });
  }

  private buildCalendar(): void {
    const pmDays = [4, 9, 13];
    this.calCells = Array.from({ length: 14 }, (_, i) => ({
      day:  i + 1,
      tags: pmDays.includes(i + 1) ? ['PM Plan'] : []
    }));
  }

  exportReport(): void { alert('Report export feature will be implemented with the backend.'); }
}
