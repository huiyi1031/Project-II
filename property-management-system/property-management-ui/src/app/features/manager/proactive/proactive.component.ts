import { Component, OnInit } from '@angular/core';
import { Asset, WorkOrder } from '../../../core/models';
import { AssetService } from '../../../core/services/asset.service';
import { WorkOrderService } from '../../../core/services/work-order.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-proactive',
  templateUrl: './proactive.component.html',
  standalone: false,
})
export class ProactiveComponent implements OnInit {
  allAssets: Asset[] = [];
  allHistories: any[] = [];
  pendingWorkOrders: any[] = [];
  
  // Lists
  unscheduledOverdue: Asset[] = [];
  
  // Calendar State
  viewDate: Date = new Date();
  viewMode: 'month' | 'week' | 'year' = 'month';
  calendarDays: { date: Date; isCurrentMonth: boolean; isToday: boolean; upcomingEvents: Asset[]; historyEvents: any[] }[] = [];
  calendarMonths: { monthIndex: number; name: string; upcomingEvents: Asset[]; historyEvents: any[] }[] = [];
  weekDays: string[] = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  // Modal State
  selectedAsset: Asset | null = null;
  showConfirmModal = false;
  submitting = false;

  selectedHistory: any | null = null;
  showHistoryModal = false;

  constructor(
    private assetSvc: AssetService,
    private woSvc: WorkOrderService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.viewDate.setHours(0,0,0,0);
    this.loadData();
  }

  loadData(): void {
    // Load active assets
    this.assetSvc.getAll({ status: 'Active' }).subscribe({
      next: (assets) => {
        this.allAssets = assets;
        
        // Load Work Orders to find pending ones
        this.woSvc.getAllWorkOrders().subscribe({
          next: (wos: any[]) => {
            this.pendingWorkOrders = wos.filter(w => w.status === 'Pending' || w.status === 'Assigned');
            
            // Load all maintenance histories
            this.assetSvc.getAllHistories().subscribe({
              next: (histories) => {
                this.allHistories = histories;
                this.processData();
                this.buildCalendar();
              }
            });
          }
        });
      }
    });
  }

  processData(): void {
    const now = new Date();
    now.setHours(0,0,0,0);
    
    // An asset is unscheduled if it has no pending work order
    const pendingAssetIds = this.pendingWorkOrders.map(w => w.assetId).filter(id => id != null);

    this.unscheduledOverdue = this.allAssets.filter(a => {
      if (!a.nextMaintenanceDueDate) return false;
      const due = new Date(a.nextMaintenanceDueDate);
      due.setHours(0,0,0,0);
      const isOverdue = due < now;
      const hasPendingWO = pendingAssetIds.includes(a.assetId);
      return isOverdue && !hasPendingWO;
    });

    this.unscheduledOverdue.sort((a, b) => new Date(a.nextMaintenanceDueDate!).getTime() - new Date(b.nextMaintenanceDueDate!).getTime());
  }

  // --- Calendar Logic ---

  changeView(mode: 'month' | 'week' | 'year'): void {
    this.viewMode = mode;
    this.buildCalendar();
  }

  prev(): void {
    const d = new Date(this.viewDate);
    if (this.viewMode === 'month') {
      d.setMonth(d.getMonth() - 1);
    } else if (this.viewMode === 'week') {
      d.setDate(d.getDate() - 7);
    } else {
      d.setFullYear(d.getFullYear() - 1);
    }
    this.viewDate = d;
    this.buildCalendar();
  }

  next(): void {
    const d = new Date(this.viewDate);
    if (this.viewMode === 'month') {
      d.setMonth(d.getMonth() + 1);
    } else if (this.viewMode === 'week') {
      d.setDate(d.getDate() + 7);
    } else {
      d.setFullYear(d.getFullYear() + 1);
    }
    this.viewDate = d;
    this.buildCalendar();
  }

  today(): void {
    const d = new Date();
    d.setHours(0,0,0,0);
    this.viewDate = d;
    this.buildCalendar();
  }

  buildCalendar(): void {
    if (this.viewMode === 'month') this.buildMonthView();
    else if (this.viewMode === 'week') this.buildWeekView();
    else this.buildYearView();
  }

  private buildMonthView(): void {
    this.calendarDays = [];
    const year = this.viewDate.getFullYear();
    const month = this.viewDate.getMonth();
    
    const firstDayOfMonth = new Date(year, month, 1);
    const lastDayOfMonth = new Date(year, month + 1, 0);
    
    // Get day of week of 1st day (0 = Sun, 6 = Sat)
    const firstDayIndex = firstDayOfMonth.getDay();
    
    // Start date for the grid
    const startDate = new Date(firstDayOfMonth);
    startDate.setDate(startDate.getDate() - firstDayIndex);
    
    const today = new Date();
    today.setHours(0,0,0,0);

    for (let i = 0; i < 42; i++) {
      const d = new Date(startDate);
      d.setDate(d.getDate() + i);
      
      const isCurrentMonth = d.getMonth() === month;
      const isToday = d.getTime() === today.getTime();
      
      this.calendarDays.push({
        date: d,
        isCurrentMonth,
        isToday,
        upcomingEvents: this.getUpcomingEventsForDate(d),
        historyEvents: this.getHistoryEventsForDate(d)
      });
    }
  }

  private buildWeekView(): void {
    this.calendarDays = [];
    const d = new Date(this.viewDate);
    const dayIndex = d.getDay();
    d.setDate(d.getDate() - dayIndex); // Start of week (Sunday)
    
    const today = new Date();
    today.setHours(0,0,0,0);

    for (let i = 0; i < 7; i++) {
      const current = new Date(d);
      current.setDate(current.getDate() + i);
      
      this.calendarDays.push({
        date: current,
        isCurrentMonth: true,
        isToday: current.getTime() === today.getTime(),
        upcomingEvents: this.getUpcomingEventsForDate(current),
        historyEvents: this.getHistoryEventsForDate(current)
      });
    }
  }

  private buildYearView(): void {
    this.calendarMonths = [];
    const year = this.viewDate.getFullYear();
    const monthNames = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
    
    for (let i = 0; i < 12; i++) {
      // Find events in this month
      const upcomingInMonth = this.allAssets.filter(a => {
        if (!a.nextMaintenanceDueDate) return false;
        const d = new Date(a.nextMaintenanceDueDate);
        return d.getFullYear() === year && d.getMonth() === i;
      });

      const historyInMonth = this.allHistories.filter(h => {
        if (!h.maintenanceDate) return false;
        const d = new Date(h.maintenanceDate);
        return d.getFullYear() === year && d.getMonth() === i;
      });
      
      this.calendarMonths.push({
        monthIndex: i,
        name: monthNames[i],
        upcomingEvents: upcomingInMonth,
        historyEvents: historyInMonth
      });
    }
  }

  private getUpcomingEventsForDate(date: Date): Asset[] {
    return this.allAssets.filter(a => {
      if (!a.nextMaintenanceDueDate) return false;
      const d = new Date(a.nextMaintenanceDueDate);
      return d.getFullYear() === date.getFullYear() && 
             d.getMonth() === date.getMonth() && 
             d.getDate() === date.getDate();
    });
  }

  private getHistoryEventsForDate(date: Date): any[] {
    return this.allHistories.filter(h => {
      if (!h.maintenanceDate) return false;
      const d = new Date(h.maintenanceDate);
      return d.getFullYear() === date.getFullYear() && 
             d.getMonth() === date.getMonth() && 
             d.getDate() === date.getDate();
    });
  }

  isOverdue(dateStr?: string): boolean {
    if (!dateStr) return false;
    const due = new Date(dateStr);
    due.setHours(0,0,0,0);
    const today = new Date();
    today.setHours(0,0,0,0);
    return due.getTime() < today.getTime();
  }

  // --- Click to Schedule Workflow ---

  onEventClick(asset: Asset, event: Event): void {
    event.stopPropagation();
    this.selectedAsset = asset;
    this.showConfirmModal = true;
  }

  closeConfirmModal(): void {
    this.showConfirmModal = false;
    this.selectedAsset = null;
  }

  onHistoryClick(history: any, event: Event): void {
    event.stopPropagation();
    this.selectedHistory = history;
    this.showHistoryModal = true;
  }

  closeHistoryModal(): void {
    this.showHistoryModal = false;
    this.selectedHistory = null;
  }

  getLastServiceDate(asset: Asset): Date {
    const d = new Date(asset.nextMaintenanceDueDate!);
    d.setDate(d.getDate() - asset.maintenanceIntervalDays);
    return d;
  }

  confirmCreateWorkOrder(): void {
    if (!this.selectedAsset) return;
    this.submitting = true;

    const dto = {
      assetId: this.selectedAsset.assetId,
      description: `Preventive Maintenance for ${this.selectedAsset.assetName}`,
      scheduleDate: new Date().toISOString()
    };

    this.woSvc.createProactiveWorkOrder(dto).subscribe({
      next: () => {
        this.submitting = false;
        this.closeConfirmModal();
        this.router.navigate(['/manager/work-orders']);
      },
      error: (err) => {
        alert('Error creating Work Order: ' + (err.error?.message || err.message));
        this.submitting = false;
      }
    });
  }
}
