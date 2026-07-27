import { Component, OnInit } from '@angular/core';
import { OccupantService } from '../../../core/services/occupant.service';

@Component({
  selector: 'app-my-property',
  templateUrl: './my-property.component.html',
  standalone: false,
})
export class MyPropertyComponent implements OnInit {
  contracts:     any[]        = [];
  currentUnit:   any | null   = null;
  ownerInfo:     any | null   = null;
  myProfile:     any | null   = null;
  isLoading      = false;
  ownerLoading   = false;

  // Role detection — is this user a tenant or resident (not owner)?
  isOwner        = false;

  constructor(
    private svc:  OccupantService,
  ) {}

  ngOnInit(): void {
    this.loadProfile();
    this.loadContracts();
  }

  loadProfile(): void {
    this.svc.getMyProfile().subscribe({
      next: (p: any) => {
        this.myProfile = p;
        // OccupantType: 1=Owner, 2=Tenant, 3=Resident(FamilyMember)
        // Only Owner does NOT see the owner info card
        this.isOwner = p.occupantType === 1;
        if (!this.isOwner) {
          this.loadOwnerInfo();
        }
      },
      error: () => {}
    });
  }

  loadContracts(): void {
    this.isLoading = true;
    this.svc.getMyContracts().subscribe({
      next: (data: any[]) => {
        this.contracts = data;
        if (data.length > 0) {
          this.currentUnit = data[0];
        }
        this.isLoading = false;
      },
      error: () => {
        this.contracts = [];
        this.isLoading = false;
      }
    });
  }

  loadOwnerInfo(): void {
    this.ownerLoading = true;
    this.svc.getMyOwner().subscribe({
      next: (owner: any) => {
        this.ownerInfo = owner;
        this.ownerLoading = false;
      },
      error: () => {
        this.ownerInfo = null;
        this.ownerLoading = false;
      }
    });
  }

  selectUnit(contract: any): void {
    this.currentUnit = contract;
  }
}
