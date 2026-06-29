import { Component, OnInit } from '@angular/core';
import { PropertyUnit } from '../../../core/models';
import { OccupantService } from '../../../core/services/occupant.service';

@Component({
  selector: 'app-units',
  templateUrl: './units.component.html',
  standalone: false,
})
export class UnitsComponent implements OnInit {
  units: PropertyUnit[] = [];
  search = '';
  statusFilter = 'All';
  editMode = false;
  form: any = {
    unitNumber: '', floor: 1, block: 'A',
    unitType: 'Apartment', size: 850, status: 'Vacant'
  };

  constructor(private svc: OccupantService) {}

  ngOnInit(): void {
    this.svc.getAllUnits().subscribe({ next: d => (this.units = d), error: () => {} });
  }

  editUnit(u: PropertyUnit): void {
    this.editMode = true;
    this.form = { ...u };
  }

  saveUnit(): void {
    if (this.editMode) {
      // Update existing unit
      this.svc.updateUnit(this.form.unitID, this.form).subscribe({
        next: () => { this.resetForm(); this.ngOnInit(); },
        error: () => {}
      });
    } else {
      // Create new unit
      this.svc.createUnit(this.form).subscribe({
        next: () => { this.resetForm(); this.ngOnInit(); },
        error: () => {}
      });
    }
  }

  resetForm(): void {
    this.editMode = false;
    this.form = { unitNumber: '', floor: 1, block: 'A', unitType: 'Apartment', size: 850, status: 'Vacant' };
  }
}
