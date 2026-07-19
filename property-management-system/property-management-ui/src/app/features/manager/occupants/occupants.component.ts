import { Component, OnInit } from '@angular/core';
import { Occupant } from '../../../core/models';
import { OccupantService } from '../../../core/services/occupant.service';

@Component({
  selector: 'app-occupants',
  templateUrl: './occupants.component.html',
  standalone: false,
})
export class OccupantsComponent implements OnInit {
  occupants: Occupant[] = [];
  search    = '';
  filter    = 'All';
  editMode  = false;
  form: any = { fullName: '', identificationNo: '', contactNumber: '', email: '', unitID: 1, occupantType: 'Tenant' };

  constructor(private svc: OccupantService) {}

  ngOnInit(): void {
    this.svc.getAllOccupants().subscribe({ next: d => (this.occupants = d), error: () => {} });
  }

  editOccupant(o: Occupant): void { this.editMode = true; this.form = { ...o }; }

  toggleStatus(o: Occupant): void {
    const action = o.occupantStatus === 'Active'
      ? this.svc.deactivateOccupant(o.occupantID)
      : this.svc.activateOccupant(o.occupantID);
    action.subscribe({ next: () => this.ngOnInit(), error: () => {} });
  }

  saveOccupant(): void {
    if (this.editMode) {
      this.svc.updateOccupant(this.form.occupantID, this.form).subscribe({ next: () => { this.resetForm(); this.ngOnInit(); }, error: () => {} });
    } else {
      this.svc.createOccupant(this.form).subscribe({ next: () => { this.resetForm(); this.ngOnInit(); }, error: () => {} });
    }
  }

  resetForm(): void { this.editMode = false; this.form = { fullName: '', occupantType: 'Tenant', unitID: 1 }; }
}
