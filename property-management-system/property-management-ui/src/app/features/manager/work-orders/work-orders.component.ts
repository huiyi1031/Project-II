import { Component, OnInit } from '@angular/core';
import { WorkOrderService } from '../../../core/services/work-order.service';

@Component({
  selector: 'app-manager-work-orders',
  templateUrl: './work-orders.component.html',
  standalone: false,
})
export class ManagerWorkOrdersComponent implements OnInit {
  workOrders: any[] = [];
  technicians: any[] = [];
  
  selectedWo: any = null;
  selectedTechId: number | null = null;
  assigning = false;

  constructor(private svc: WorkOrderService) {}

  ngOnInit(): void { 
    this.loadWorkOrders();
  }

  loadWorkOrders(): void {
    // We updated getAllWorkOrders in the service, but since the model changed, we'll cast to any for now
    this.svc.getAllWorkOrders().subscribe({ 
      next: (d: any) => {
        this.workOrders = d;
      }, 
      error: (e) => console.error(e) 
    });
  }

  openAssignModal(wo: any): void {
    this.selectedWo = wo;
    this.selectedTechId = null;
    
    // Load technicians, pass assetId if it's a proactive maintenance WO
    this.svc.getTechnicians(wo.assetId).subscribe({
      next: (techs: any) => {
        this.technicians = techs;
      }
    });
  }

  closeAssignModal(): void {
    this.selectedWo = null;
    this.technicians = [];
  }

  assignTechnician(): void {
    if (!this.selectedWo || !this.selectedTechId) return;
    this.assigning = true;
    
    this.svc.assignTechnician(this.selectedWo.id, this.selectedTechId).subscribe({
      next: () => {
        this.assigning = false;
        this.closeAssignModal();
        this.loadWorkOrders(); // Refresh table
      },
      error: (err) => {
        alert('Failed to assign technician: ' + (err.error?.message || err.message));
        this.assigning = false;
      }
    });
  }
}
