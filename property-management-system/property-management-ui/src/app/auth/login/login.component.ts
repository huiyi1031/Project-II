import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';

type LoginStep =
  | 'login'           // Enter email & password
  | 'help'            // "Need help?" — role selector panel
  | 'ic-entry'        // Owner: Enter IC/Passport
  | 'ic-not-found'    // IC not found
  | 'ic-found-email'  // IC found → update email
  | 'set-password';   // First login / After IC — set permanent password

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  standalone: false,
})
export class LoginComponent {
  step: LoginStep = 'login';

  // Forms
  loginForm:       FormGroup;
  setPasswordForm: FormGroup;
  icForm:          FormGroup;
  newEmailForm:    FormGroup;

  // State carried across steps
  email       = '';
  maskedEmail = '';
  updateToken = '';

  isLoading    = false;
  errorMessage = '';

  // UI state
  showPassword    = false;
  showNewPassword = false;
  showConfirmPw   = false;

  // Password strength
  get pwStrength(): number { return this._calcStrength(this.setPasswordForm.get('newPassword')?.value || ''); }
  readonly pwStrengthLabels = ['', 'Weak', 'Fair', 'Good', 'Strong'];
  readonly pwStrengthColors = ['', 'bg-red-400', 'bg-orange-400', 'bg-yellow-400', 'bg-green-500'];

  constructor(private fb: FormBuilder, private auth: AuthService) {
    this.loginForm = fb.group({
      email:    ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
    });
    this.setPasswordForm = fb.group({
      newPassword:     ['', [Validators.required, Validators.minLength(8), this._pwComplexity]],
      confirmPassword: ['', Validators.required],
    }, { validators: this._pwMatch });
    this.icForm = fb.group({
      identificationNo: ['', [Validators.required, Validators.minLength(6)]],
    });
    this.newEmailForm = fb.group({
      newEmail: ['', [Validators.required, Validators.email]],
    });
  }

  // ── Main Login ───────────────────────────────────────────────────────────────
  submitLogin(): void {
    if (this.loginForm.invalid) { this.loginForm.markAllAsTouched(); return; }
    this.email = this.loginForm.value.email.toLowerCase().trim();
    this.isLoading = true;
    this.errorMessage = '';

    this.auth.login({ email: this.email, password: this.loginForm.value.password }).subscribe({
      next: (resp) => {
        this.isLoading = false;
        if (resp.requiresPasswordChange) {
          this.updateToken = resp.updateToken || '';
          this.step = 'set-password';
        } else {
          this.auth.navigateToDashboard(resp.role, resp.occupantType);
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Incorrect email or password. Please try again.';
      }
    });
  }

  // ── Help / "Need help?" panel ─────────────────────────────────────────────────
  openHelp(): void {
    this.step = 'help';
    this.errorMessage = '';
  }

  // Only Owner gets the IC option from the help panel
  startOwnerVerification(): void {
    this.step = 'ic-entry';
    this.errorMessage = '';
    this.icForm.reset();
  }

  goBackToLogin(): void {
    this.step = 'login';
    this.errorMessage = '';
  }

  goBackToHelp(): void {
    this.step = 'help';
    this.errorMessage = '';
  }

  // ── Owner IC Path ─────────────────────────────────────────────────────────────
  submitIc(): void {
    if (this.icForm.invalid) { this.icForm.markAllAsTouched(); return; }
    this.isLoading = true;
    this.auth.verifyIc(this.icForm.value.identificationNo).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.found) {
          this.maskedEmail = res.maskedEmail || '';
          this.updateToken = res.updateToken || '';
          this.step = 'ic-found-email';
        } else {
          this.step = 'ic-not-found';
        }
      },
      error: () => { this.isLoading = false; this.errorMessage = 'Verification failed. Please try again.'; }
    });
  }

  submitNewEmail(): void {
    if (this.newEmailForm.invalid) { this.newEmailForm.markAllAsTouched(); return; }
    this.isLoading = true;
    const newEmail = this.newEmailForm.value.newEmail.toLowerCase().trim();
    this.auth.updateEmailByIc(this.updateToken, newEmail).subscribe({
      next: () => {
        this.isLoading = false;
        this.email = newEmail;
        this.step = 'set-password';
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Failed to update email. Please try again.';
      }
    });
  }

  // ── Set New Password (first-time login or after IC bypass) ───────────────────
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
        this.auth.navigateToDashboard(resp.role, resp.occupantType);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Failed to set password. Please try again.';
      }
    });
  }

  // ── Password helpers ──────────────────────────────────────────────────────────
  spLen(n: number): boolean { return (this.setPasswordForm.get('newPassword')?.value || '').length >= n; }
  spHas(pattern: 'upper' | 'number' | 'special'): boolean {
    const v = this.setPasswordForm.get('newPassword')?.value || '';
    if (pattern === 'upper')   return /[A-Z]/.test(v);
    if (pattern === 'number')  return /[0-9]/.test(v);
    if (pattern === 'special') return /[^A-Za-z0-9]/.test(v);
    return false;
  }

  private _calcStrength(pw: string): number {
    if (!pw) return 0;
    let score = 0;
    if (pw.length >= 8)          score++;
    if (/[A-Z]/.test(pw))        score++;
    if (/[0-9]/.test(pw))        score++;
    if (/[^A-Za-z0-9]/.test(pw)) score++;
    return score;
  }

  private _pwComplexity(ctrl: AbstractControl): { [key: string]: boolean } | null {
    const v = ctrl.value || '';
    const errors: Record<string, boolean> = {};
    if (!/[A-Z]/.test(v))        errors['noUppercase'] = true;
    if (!/[0-9]/.test(v))        errors['noNumber']    = true;
    if (!/[^A-Za-z0-9]/.test(v)) errors['noSpecial']   = true;
    return Object.keys(errors).length ? errors : null;
  }

  private _pwMatch(group: AbstractControl): { [key: string]: boolean } | null {
    const np = group.get('newPassword')?.value;
    const cp = group.get('confirmPassword')?.value;
    return np && cp && np !== cp ? { mismatch: true } : null;
  }
}