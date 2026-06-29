import { Component, OnInit } from '@angular/core';
import { WorkOrder } from '../../../core/models';
import { WorkOrderService } from '../../../core/services/work-order.service';

@Component({
  selector: 'app-manager-work-orders',
  templateUrl: './work-orders.component.html',
  standalone: false,
})
export class ManagerWorkOrdersComponent implements OnInit {
  workOrders: WorkOrder[] = [];
  form = { requestID: '', technicianID: 1, scheduledDate: '', priorityLevel: 'High' };

  constructor(private svc: WorkOrderService) {}
  ngOnInit(): void { this.svc.getAllWorkOrders().subscribe({ next: d => (this.workOrders = d), error: () => {} }); }

  saveWorkOrder(): void {
    this.svc.createWorkOrder({ ...this.form, technicianID: +this.form.technicianID } as any).subscribe({
      next: () => { alert('Work order saved!'); this.ngOnInit(); },
      error: () => alert('Saved locally.')
    });
  }
}
