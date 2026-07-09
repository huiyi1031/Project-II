import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { OccupantService } from '../../../core/services/occupant.service';
import { FamilyMember, UnitHeadcount } from '../../../core/models';

type FamilyView = 'list' | 'add' | 'limit-warning' | 'pending-approval' | 'details';

@Component({
  selector: 'app-family',
  templateUrl: './family.component.html',
  standalone: false,
})
export class FamilyComponent implements OnInit {
  view: FamilyView = 'list';
  members: FamilyMember[] = [];
  isLoading = false;
  isSaving  = false;
  errorMsg  = '';
  successMsg = '';
  removeTargetId = 0;
  removeTargetName = '';
  showRemoveModal = false;
  selectedMember: FamilyMember | null = null;

  addForm: FormGroup;

  constructor(private fb: FormBuilder, private svc: OccupantService) {
    this.addForm = fb.group({
      fullName:         ['', [Validators.required, Validators.minLength(2)]],
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
      error: (e) => {
        this.errorMsg = 'Failed to load family members.';
        this.members = [];
        this.isLoading = false;
      }
    });
  }

  /** Called when user clicks "Add Family Member" button */
  initiateAdd(): void {
    this.view = 'add';
    this.errorMsg = ''; this.successMsg = '';
  }

  submitAdd(): void {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }

    this.isSaving = true;
    this.svc.addFamilyMember(this.addForm.value).subscribe({
      next: (res: any) => {
        this.isSaving = false;
        this.successMsg = `Member added. A temporary password (${res.tempPassword}) was sent via email.`;
        this.loadData();
        this.view = 'list';
        this.addForm.reset();
        setTimeout(() => this.successMsg = '', 6000);
      },
      error: (e) => {
        this.isSaving = false;
        this.errorMsg = e.error?.message || 'Failed to add family member.';
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
        this.errorMsg = 'Failed to remove member.';
      }
    });
  }

  cancelAdd(): void {
    this.view = 'list';
    this.addForm.reset();
  }

  viewDetails(member: FamilyMember): void {
    this.selectedMember = member;
    this.view = 'details';
  }

  closeDetails(): void {
    this.selectedMember = null;
    this.view = 'list';
  }
}
