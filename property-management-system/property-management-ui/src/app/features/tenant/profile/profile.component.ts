import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { UserService } from '../../../core/services/user.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  standalone: false,
})
export class ProfileComponent implements OnInit {
  activeTab: 'info' | 'password' = 'info';
  isLoading = false;
  isSaving  = false;
  successMsg = '';
  errorMsg   = '';

  profileForm:  FormGroup;
  passwordForm: FormGroup;
  showCurrent = false;
  showNew     = false;
  showConfirm = false;

  profile: any = null;

  get pwStrength(): number {
    const v = this.passwordForm.get('newPassword')?.value || '';
    let s = 0;
    if (v.length >= 8)            s++;
    if (/[A-Z]/.test(v))          s++;
    if (/[0-9]/.test(v))          s++;
    if (/[^A-Za-z0-9]/.test(v))   s++;
    return s;
  }
  readonly strengthLabels = ['', 'Weak', 'Fair', 'Good', 'Strong'];
  readonly strengthColors = ['', 'bg-red-400', 'bg-orange-400', 'bg-yellow-400', 'bg-green-500'];

  constructor(
    private fb: FormBuilder,
    private userSvc: UserService,
    private authSvc: AuthService,
  ) {
    this.profileForm = fb.group({
      fullName:      ['', [Validators.required, Validators.minLength(2)]],
      contactNumber: ['', [Validators.required, Validators.pattern(/^[0-9+\-() ]{8,15}$/)]],
      gender:        ['', Validators.required],
    });

    this.passwordForm = fb.group({
      currentPassword: ['', Validators.required],
      newPassword:     ['', [Validators.required, Validators.minLength(8), this._pwComplexity]],
      confirmPassword: ['', Validators.required],
    }, { validators: this._pwMatch });
  }

  ngOnInit(): void {
    this.isLoading = true;
    this.userSvc.getProfile().subscribe({
      next: (p) => {
        this.profile = p;
        this.profileForm.patchValue({ fullName: p.fullName, contactNumber: p.contactNumber, gender: p.gender });
        this.isLoading = false;
      },
      error: () => {
        this.errorMsg = 'Failed to load profile data.';
        this.isLoading = false;
      }
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) { this.profileForm.markAllAsTouched(); return; }
    this.isSaving = true; this.errorMsg = ''; this.successMsg = '';
    this.userSvc.updateProfile(this.profileForm.value).subscribe({
      next: () => { this.isSaving = false; this.successMsg = 'Profile updated successfully.'; setTimeout(() => this.successMsg = '', 3500); },
      error: () => { this.isSaving = false; this.errorMsg = 'Failed to update profile.'; setTimeout(() => this.errorMsg = '', 3500); }
    });
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.isLoading = true;
      this.userSvc.uploadProfilePicture(file).subscribe({
        next: (res) => {
          this.profile.profilePictureUrl = res.profilePictureUrl;
          this.isLoading = false;
          this.successMsg = 'Profile picture updated.';
          setTimeout(() => this.successMsg = '', 3500);
        },
        error: () => {
          this.isLoading = false;
          this.errorMsg = 'Failed to upload picture.';
          setTimeout(() => this.errorMsg = '', 3500);
        }
      });
    }
  }

  changePassword(): void {
    if (this.passwordForm.invalid) { this.passwordForm.markAllAsTouched(); return; }
    this.isSaving = true; this.errorMsg = ''; this.successMsg = '';
    
    const payload = {
      ...this.passwordForm.value,
      email: this.profile?.email || this.authSvc.getCurrentUser()?.email
    };

    this.authSvc.changePassword(payload).subscribe({
      next: () => { this.isSaving = false; this.successMsg = 'Password changed successfully.'; this.passwordForm.reset(); setTimeout(() => this.successMsg = '', 3500); },
      error: () => { this.isSaving = false; this.errorMsg = 'Current password is incorrect or request failed.'; }
    });
  }

  private _pwComplexity(c: AbstractControl) {
    const v = c.value || '';
    const e: Record<string, boolean> = {};
    if (!/[A-Z]/.test(v)) e['noUppercase'] = true;
    if (!/[0-9]/.test(v)) e['noNumber'] = true;
    if (!/[^A-Za-z0-9]/.test(v)) e['noSpecial'] = true;
    return Object.keys(e).length ? e : null;
  }
  private _pwMatch(g: AbstractControl) {
    const np = g.get('newPassword')?.value;
    const cp = g.get('confirmPassword')?.value;
    return np && cp && np !== cp ? { mismatch: true } : null;
  }

  // ── Template helpers (replaces regex in templates — Angular doesn't allow /regex/) ──
  pwLen(n: number):   boolean { return (this.passwordForm.get('newPassword')?.value || '').length >= n; }
  pwHas(pattern: 'upper' | 'number' | 'special'): boolean {
    const v = this.passwordForm.get('newPassword')?.value || '';
    if (pattern === 'upper')   return /[A-Z]/.test(v);
    if (pattern === 'number')  return /[0-9]/.test(v);
    if (pattern === 'special') return /[^A-Za-z0-9]/.test(v);
    return false;
  }
}
