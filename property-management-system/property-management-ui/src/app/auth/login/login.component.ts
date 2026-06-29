import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';

/** All possible states of the login wizard */
type LoginStep =
  | 'email'           // Step 1: Enter email
  | 'not-found'       // Step 2a: Email not found → choose role fallback
  | 'ic-entry'        // Step 2a-Owner: Enter IC/Passport
  | 'ic-not-found'    // IC not found in system
  | 'ic-found-email'  // IC found → update email
  | 'set-password'    // First login (null pw) — set permanent password
  | 'verify-temp'     // Staff first login — verify temporary password
  | 'new-password'    // Staff after temp verified — set permanent password
  | 'enter-password'; // Normal login — enter password

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  standalone: false,
})
export class LoginComponent {
  step: LoginStep = 'email';

  // Forms for each step
  emailForm:       FormGroup;
  passwordForm:    FormGroup;
  setPasswordForm: FormGroup;
  tempPwForm:      FormGroup;
  icForm:          FormGroup;
  newEmailForm:    FormGroup;

  // State carried across steps
  email          = '';
  roleType       = '';
  occupantType   = '';
  passwordStatus: 'None' | 'Temporary' | 'Active' = 'Active';
  maskedEmail    = '';
  updateToken    = '';
  tempVerifiedToken = '';

  isLoading = false;
  errorMessage = '';
  successMessage = '';

  // UI state
  showPassword     = false;
  showNewPassword  = false;
  showConfirmPw    = false;
  showTempPw       = false;
  selectedFallback = '';   // 'tenant' | 'staff' | 'owner'

  // Password strength (0-4)
  get pwStrength(): number { return this._calcStrength(this.setPasswordForm.get('newPassword')?.value || ''); }
  get newPwStrength(): number { return this._calcStrength(this.setPasswordForm.get('newPassword')?.value || ''); }

  readonly pwStrengthLabels = ['', 'Weak', 'Fair', 'Good', 'Strong'];
  readonly pwStrengthColors = ['', 'bg-red-400', 'bg-orange-400', 'bg-yellow-400', 'bg-green-500'];

  constructor(private fb: FormBuilder, private auth: AuthService) {
    this.emailForm = fb.group({
      email: ['', [Validators.required, Validators.email]],
    });
    this.passwordForm = fb.group({
      password: ['', Validators.required],
    });
    this.setPasswordForm = fb.group({
      newPassword:     ['', [Validators.required, Validators.minLength(8), this._pwComplexity]],
      confirmPassword: ['', Validators.required],
    }, { validators: this._pwMatch });
    this.tempPwForm = fb.group({
      temporaryPassword: ['', Validators.required],
    });
    this.icForm = fb.group({
      identificationNo: ['', [Validators.required, Validators.minLength(6)]],
    });
    this.newEmailForm = fb.group({
      newEmail: ['', [Validators.required, Validators.email]],
    });
  }

  // ── Step 1: Submit email ────────────────────────────────────────────────────
  submitEmail(): void {
    if (this.emailForm.invalid) { this.emailForm.markAllAsTouched(); return; }
    this.email = this.emailForm.value.email.toLowerCase();
    this.isLoading = true;
    this.errorMessage = '';

    this.auth.checkEmail(this.email).subscribe({
      next: (result) => {
        this.isLoading = false;
        if (!result.found) {
          this.step = 'not-found';
          return;
        }
        this.roleType       = result.roleType || '';
        this.occupantType   = result.occupantType || '';
        this.passwordStatus = result.passwordStatus || 'Active';

        if (result.passwordStatus === 'None') {
          // Occupant first login — set new password
          this.step = 'set-password';
        } else if (result.passwordStatus === 'Temporary') {
          // Staff first login — verify temp password first
          this.step = 'verify-temp';
        } else {
          // Normal login — enter password
          this.step = 'enter-password';
        }
      },
      error: () => { this.isLoading = false; this.errorMessage = 'Network error. Please try again.'; }
    });
  }

  // ── Step 2a: Not-found fallback — select role ────────────────────────────────
  selectFallback(option: 'tenant' | 'staff' | 'owner'): void {
    this.selectedFallback = option;
    this.errorMessage = '';
    if (option === 'owner') {
      this.step = 'ic-entry';
    }
    // For tenant/staff, the message is shown inline — user can re-enter email
  }

  goBackToEmail(): void {
    this.step = 'email';
    this.errorMessage = '';
    this.selectedFallback = '';
  }

  // ── Step 2a-IC: Submit IC number ─────────────────────────────────────────────
  submitIc(): void {
    if (this.icForm.invalid) { this.icForm.markAllAsTouched(); return; }
    this.isLoading = true;
    this.auth.verifyIc(this.icForm.value.identificationNo).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.found) {
          this.maskedEmail  = res.maskedEmail || '';
          this.updateToken  = res.updateToken || '';
          this.step = 'ic-found-email';
        } else {
          this.step = 'ic-not-found';
        }
      },
      error: () => { this.isLoading = false; this.errorMessage = 'Verification failed. Try again.'; }
    });
  }

  // ── IC found — update email, then set password ───────────────────────────────
  submitNewEmail(): void {
    if (this.newEmailForm.invalid) { this.newEmailForm.markAllAsTouched(); return; }
    this.isLoading = true;
    const newEmail = this.newEmailForm.value.newEmail;
    this.auth.updateEmailByIc(this.updateToken, newEmail).subscribe({
      next: () => {
        this.isLoading = false;
        this.email = newEmail;
        this.step = 'set-password';
      },
      error: () => { this.isLoading = false; this.errorMessage = 'Failed to update email. Please contact the management office.'; }
    });
  }

  // ── Step 3a (Staff): Verify temp password ────────────────────────────────────
  submitTempPassword(): void {
    if (this.tempPwForm.invalid) { this.tempPwForm.markAllAsTouched(); return; }
    this.isLoading = true;
    this.auth.verifyTempPassword({ email: this.email, temporaryPassword: this.tempPwForm.value.temporaryPassword }).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.tempVerifiedToken = res.tempVerifiedToken;
        this.step = 'new-password'; // Now set permanent password
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Incorrect temporary password. Check your activation email.';
      }
    });
  }

  // ── Step 3b / Set new password (first login or after IC) ────────────────────
  submitSetPassword(): void {
    if (this.setPasswordForm.invalid) { this.setPasswordForm.markAllAsTouched(); return; }
    this.isLoading = true;
    this.auth.setPassword({
      email:           this.email,
      newPassword:     this.setPasswordForm.value.newPassword,
      confirmPassword: this.setPasswordForm.value.confirmPassword,
      updateToken:     this.updateToken || undefined,
    }).subscribe({
      next: (resp) => {
        this.isLoading = false;
        this.auth.navigateToDashboard(resp.role);
      },
      error: (err) => { this.isLoading = false; this.errorMessage = err.error?.message || 'Failed to set password. Try again.'; }
    });
  }

  // ── Step 4: Normal password login ────────────────────────────────────────────
  submitPassword(): void {
    if (this.passwordForm.invalid) { this.passwordForm.markAllAsTouched(); return; }
    this.isLoading = true;
    this.auth.login({ email: this.email, password: this.passwordForm.value.password }).subscribe({
      next: (resp) => { this.isLoading = false; this.auth.navigateToDashboard(resp.role); },
      error: (err) => { this.isLoading = false; this.errorMessage = err.error?.message || 'Incorrect password. Please try again.'; }
    });
  }

  // ── Helpers ──────────────────────────────────────────────────────────────────
  getStepNumber(): number {
    const map: Partial<Record<LoginStep, number>> = {
      'email': 1,
      'not-found': 2, 'ic-entry': 2, 'ic-not-found': 2, 'ic-found-email': 2,
      'enter-password': 3, 'set-password': 3, 'verify-temp': 3, 'new-password': 3,
    };
    return map[this.step] || 1;
  }

  getFallbackMessage(option: string): string {
    const msgs: Record<string, string> = {
      'tenant': 'You may re-enter your email. New Tenants and Residents must be invited by their Property Owner or Manager to gain access.',
      'staff':  'Please ensure your email is entered correctly, then re-enter it. If the issue persists, contact your administrator.',
      'owner':  'You may re-enter your email, or enter your Identification Number (IC or Passport) as per your Sale and Purchase Agreement to claim your property.',
    };
    return msgs[option] || '';
  }

  private _calcStrength(pw: string): number {
    if (!pw) return 0;
    let score = 0;
    if (pw.length >= 8)         score++;
    if (/[A-Z]/.test(pw))       score++;
    if (/[0-9]/.test(pw))       score++;
    if (/[^A-Za-z0-9]/.test(pw)) score++;
    return score;
  }

  private _pwComplexity(ctrl: AbstractControl): { [key: string]: boolean } | null {
    const v = ctrl.value || '';
    const errors: Record<string, boolean> = {};
    if (!/[A-Z]/.test(v))       errors['noUppercase'] = true;
    if (!/[0-9]/.test(v))       errors['noNumber']    = true;
    if (!/[^A-Za-z0-9]/.test(v)) errors['noSpecial']  = true;
    return Object.keys(errors).length ? errors : null;
  }

  private _pwMatch(group: AbstractControl): { [key: string]: boolean } | null {
    const np = group.get('newPassword')?.value;
    const cp = group.get('confirmPassword')?.value;
    return np && cp && np !== cp ? { mismatch: true } : null;
  }

  // ── Template-safe helpers (Angular can't parse /regex/ in templates) ──────
  spLen(n: number): boolean { return (this.setPasswordForm.get('newPassword')?.value || '').length >= n; }
  spHas(pattern: 'upper' | 'number' | 'special'): boolean {
    const v = this.setPasswordForm.get('newPassword')?.value || '';
    if (pattern === 'upper')   return /[A-Z]/.test(v);
    if (pattern === 'number')  return /[0-9]/.test(v);
    if (pattern === 'special') return /[^A-Za-z0-9]/.test(v);
    return false;
  }
}