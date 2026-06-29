import { Component } from '@angular/core';
@Component({
  selector: 'app-tech-report',
  templateUrl: './report.component.html',
  standalone: false,
})
export class TechReportComponent {
  months = [
    { label: 'April 2026',  count: 18, pct: '75%' },
    { label: 'March 2026',  count: 22, pct: '90%' },
    { label: 'February 2026', count: 15, pct: '62%' },
  ];
}
