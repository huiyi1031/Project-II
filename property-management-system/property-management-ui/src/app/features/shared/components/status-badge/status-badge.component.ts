import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  standalone: false,
  template: `<span [class]="badgeClass">{{ label }}</span>`,
})
export class StatusBadgeComponent {
  @Input() status = '';

  get label(): string {
    const map: Record<string, string> = {
      Pending:    'Pending',
      Assigned:   'Assigned',
      InProgress: 'In Progress',
      Completed:  'Completed',
      Cancelled:  'Cancelled',
      Closed:     'Closed',
      Active:     'Active',
      Inactive:   'Inactive',
      Occupied:   'Occupied',
      Vacant:     'Vacant',
      High:       'High',
      Medium:     'Medium',
      Low:        'Low',
    };
    return map[this.status] ?? this.status;
  }

  get badgeClass(): string {
    const map: Record<string, string> = {
      Pending:    'badge-pending',
      Assigned:   'badge-assigned',
      InProgress: 'badge-progress',
      Completed:  'badge-complete',
      Cancelled:  'badge-cancel',
      Closed:     'badge-cancel',
      Active:     'badge-complete',
      Occupied:   'badge-complete',
      Inactive:   'badge-cancel',
      Vacant:     'badge-pending',
      High:       'badge-high',
      Medium:     'badge-medium',
      Low:        'badge-low',
    };
    return map[this.status] ?? 'badge-assigned';
  }
}
