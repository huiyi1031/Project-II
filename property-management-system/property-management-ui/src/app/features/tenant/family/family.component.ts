import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { OccupantService } from '../../../core/services/occupant.service';
import { FamilyMember, UnitHeadcount } from '../../../core/models';

type FamilyView = 'list' | 'add' | 'limit-warning' | 'pending-approval';

@Component({
  selector: 'app-family',
  templateUrl: './family.component.html',
  standalone: false,
})
export class FamilyComponent implements OnInit {
  view: FamilyView = 'list';
  members: FamilyMember[] = [];
  headcount: UnitHeadcount | null = null;
  isLoading = false;
  isSaving  = false;
  errorMsg  = '';
  successMsg = '';
  removeTargetId = 0;
  removeTargetName = '';
  showRemoveModal = false;

  addForm: FormGroup;

  constructor(private fb: FormBuilder, private svc: OccupantService) {
    this.addForm = fb.group({
      fullName:         ['', [Validators.required, Validators.minLength(2)]],
      identificationNo: ['', [Validators.required, Validators.minLength(6)]],
      email:            ['', [Validators.required, Validators.email]],
      contactNumber:    ['', [Validators.required, Validators.pattern(/^[0-9+\-() ]{8,15}$/)]],
      relationship:     ['', Validators.required],
      gender:           ['', Validators.required],
      dateOfBirth:      ['', Validators.required],
    });
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    // Load members
    this.svc.getMyFamilyMembers().subscribe({
      next: (d) => { this.members = d; this.isLoading = false; },
      error: () => {
        // Demo fallback
        this.members = [
          { occupantID: 11, fullName: 'Siti Aishah',    identificationNo: '950202-10-5678', relationship: 'Spouse',  email: 'siti@demo.com', contactNumber: '012-1111111', gender: 'Female', dateOfBirth: '1995-02-02', occupantStatus: 'Active' },
          { occupantID: 12, fullName: 'Ahmad Hakim',    identificationNo: '180303-10-9999', relationship: 'Child',   email: 'hakim@demo.com', contactNumber: '',           gender: 'Male',   dateOfBirth: '2018-03-03', occupantStatus: 'Active' },
          { occupantID: 13, fullName: 'Nur Hafizah',    identificationNo: '001010-10-1111', relationship: 'Child',   email: 'hafizah@demo.com', contactNumber: '',        gender: 'Female', dateOfBirth: '2000-10-10', occupantStatus: 'Pending' },
        ];
        this.isLoading = false;
      }
    });
    // Load headcount
    this.svc.getUnitHeadcount().subscribe({
      next: (h) => { this.headcount = h; },
      error: () => {
        // Demo: simulate unit with 3/5 capacity
        this.headcount = { currentCount: 3, maxOccupants: 5, unitNumber: 'A-12-03', canAddDirect: true };
      }
    });
  }

  /** Called when user clicks "Add Family Member" button */
  initiateAdd(): void {
    if (!this.headcount) return;
    if (this.headcount.canAddDirect) {
      this.view = 'add';
    } else {
      this.view = 'limit-warning';
    }
    this.errorMsg = ''; this.successMsg = '';
  }

  /** User opts to request manager approval despite being at limit */
  requestManagerApproval(): void {
    this.view = 'add';
    // Flag so backend knows to send approval request instead of direct creation
  }

  submitAdd(): void {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }
    this.isSaving = true; this.errorMsg = '';
    const isOverLimit = !this.headcount?.canAddDirect;

    this.svc.addFamilyMember({ ...this.addForm.value, requiresApproval: isOverLimit }).subscribe({
      next: () => {
        this.isSaving = false;
        this.successMsg = isOverLimit
          ? 'Approval request sent to the Property Manager. The family member will be added once approved.'
          : 'Family member added successfully. An activation email has been sent.';
        if (isOverLimit) {
          this.view = 'pending-approval';
        } else {
          this.view = 'list';
          this.loadData();
        }
      },
      error: () => {
        this.isSaving = false;
        // Demo success
        this.successMsg = isOverLimit
          ? 'Approval request sent to the Property Manager (demo mode).'
          : 'Family member added. Activation email sent (demo mode).';
        this.view = 'list';
        // Add a demo entry
        this.members.push({ ...this.addForm.value, occupantID: Date.now(), occupantStatus: isOverLimit ? 'Pending' : 'Active' });
        this.addForm.reset();
      }
    });
  }

  confirmRemove(id: number, name: string): void {
    this.removeTargetId   = id;
    this.removeTargetName = name;
    this.showRemoveModal  = true;
  }

  removeMember(): void {
    this.showRemoveModal = false;
    this.svc.removeFamilyMember(this.removeTargetId).subscribe({
      next: () => { this.successMsg = `${this.removeTargetName}'s access has been revoked.`; this.loadData(); },
      error: () => {
        this.members = this.members.filter(m => m.occupantID !== this.removeTargetId);
        this.successMsg = `${this.removeTargetName} removed (demo mode).`;
      }
    });
  }

  cancelAdd(): void { this.view = 'list'; this.addForm.reset(); this.errorMsg = ''; }
}
