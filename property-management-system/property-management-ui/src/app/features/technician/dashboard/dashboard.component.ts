import { Component, OnInit } from '@angular/core';
import { WorkOrder } from '../../../core/models';
import { WorkOrderService } from '../../../core/services/work-order.service';

@Component({
  selector: 'app-tech-dashboard',
  templateUrl: './dashboard.component.html',
  standalone: false,
})
export class TechDashboardComponent implements OnInit {
  workOrders: WorkOrder[] = [];
  perfStats = [
    { label: 'Jobs Completed',  value: '18' },
    { label: 'Avg Completion',  value: '3.4h' },
    { label: 'Rating',          value: '⭐ 4.7' },
    { label: 'On-time Rate',    value: '88%' },
  ];

  constructor(private woSvc: WorkOrderService) {}

  ngOnInit(): void {
    this.woSvc.getMyWorkOrders().subscribe({
      next: (data) => (this.workOrders = data),
      error: () => {}
    });
  }
}
