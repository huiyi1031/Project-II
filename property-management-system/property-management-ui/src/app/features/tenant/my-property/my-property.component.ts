import { Component, OnInit } from '@angular/core';
import { OccupantService } from '../../../core/services/occupant.service';
import { Contract, PropertyUnit } from '../../../core/models';

@Component({
  selector: 'app-my-property',
  templateUrl: './my-property.component.html',
  standalone: false,
})
export class MyPropertyComponent implements OnInit {
  contracts:      Contract[]    = [];
  currentUnit:    PropertyUnit | null = null;
  selectedUnitId: number | null = null;

  constructor(private svc: OccupantService) {}

  ngOnInit(): void {
    this.svc.getMyContracts().subscribe({
      next: (data) => { this.contracts = data; },
      error: () => {}
    });
  }
}
