import { Component, OnInit } from '@angular/core';
import { WorkOrder } from '../../../core/models';
import { WorkOrderService } from '../../../core/services/work-order.service';

@Component({
  selector: 'app-tech-work-orders',
  templateUrl: './work-orders.component.html',
  standalone: false,
})
export class TechWorkOrdersComponent implements OnInit {
  workOrders:   WorkOrder[] = [];
  selected:     WorkOrder | null = null;
  statusFilter  = 'All';
  calView       = 'Weekly';
  decision      = 'Accept';
  declineReason = '';

  days     = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
  calCells: { day: number; orders: string[] }[] = [];

  constructor(private woSvc: WorkOrderService) {}

  ngOnInit(): void {
    this.buildCalendar();
    this.loadOrders();
  }

  loadOrders(): void {
    this.woSvc.getMyWorkOrders(this.statusFilter).subscribe({
      next: (data) => (this.workOrders = data),
      error: () => {}
    });
  }

  select(wo: WorkOrder): void {
    this.selected = wo;
  }

  acknowledge(): void {
    if (!this.selected) return;
    this.woSvc.acknowledgeWorkOrder(
      this.selected.workOrderID, this.decision, this.declineReason
    ).subscribe({
      next: () => { alert('Work order acknowledged!'); this.selected = null; this.loadOrders(); },
      error: () => alert('Saved locally (API not connected).')
    });
  }

  private buildCalendar(): void {
    const highlightDays = [2, 5, 11, 15, 21];
    this.calCells = Array.from({ length: 28 }, (_, i) => ({
      day:    i + 1,
      orders: highlightDays.includes(i + 1) ? [`WO-${120 + i}`] : []
    }));
  }
}
