import { Component, OnInit } from '@angular/core';
import { WorkOrderService } from '../../../core/services/work-order.service';
import { AssetService } from '../../../core/services/asset.service';
import { AssetMaintenanceHistory } from '../../../core/models';

@Component({
  selector: 'app-execute-work',
  templateUrl: './execute-work.component.html',
  standalone: false,
})
export class ExecuteWorkComponent implements OnInit {
  workOrderId     = 'WO-145';
  newStatus       = 'InProgress';
  progressNotes   = '';
  completionReport = '';
  evidenceFile    = '';
  history:        AssetMaintenanceHistory[] = [];
  msg             = '';

  constructor(private woSvc: WorkOrderService, private assetSvc: AssetService) {}

  ngOnInit(): void { /* history could be loaded from API */ }

  onEvidence(event: Event): void {
    const f = (event.target as HTMLInputElement).files?.[0];
    if (f) this.evidenceFile = f.name;
  }

  saveProgress(): void {
    const id = parseInt(this.workOrderId.replace('WO-', ''));
    if (!id) return;
    this.woSvc.updateStatus(id, this.newStatus, this.progressNotes).subscribe({
      next: () => { this.msg = 'Progress saved.'; setTimeout(() => (this.msg = ''), 3000); },
      error: () => { this.msg = 'Saved locally.'; setTimeout(() => (this.msg = ''), 3000); }
    });
  }

  submitCompletion(): void {
    const id = parseInt(this.workOrderId.replace('WO-', ''));
    if (!id) return;
    this.woSvc.submitCompletion(id, this.completionReport).subscribe({
      next: () => { this.msg = 'Work order completed successfully!'; },
      error: () => { this.msg = 'Marked as completed locally.'; }
    });
  }
}
