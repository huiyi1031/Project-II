import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { Router } from '@angular/router';
import {
  LoginCheckResult, VerifyIcResult, SetPasswordDto,
  VerifyTempPasswordDto, ChangePasswordDto
} from '../models';

export interface LoginRequest  { email: string; password: string; }
export interface LoginResponse { token: string; userId: number; email: string; role: string; fullName: string; accountStatus: string; occupantType?: string; }

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = 'http://localhost:5004/api';
  private currentUserSubject = new BehaviorSubject<LoginResponse | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  // ── Demo account database (simulates UserAccount table) ──────────────────────
  private readonly DEMO_USERS: Record<string, { password: string; role: string; occupantType?: string; fullName: string; passwordStatus: 'None' | 'Temporary' | 'Active' }> = {
    'owner@demo.com':    { password: 'Test123!', role: 'Occupant',        occupantType: 'Owner',    fullName: 'Nurul Ain (Owner)',       passwordStatus: 'Active' },
    'tenant@demo.com':   { password: 'Test123!', role: 'Occupant',        occupantType: 'Tenant',   fullName: 'Ravi Kumar (Tenant)',      passwordStatus: 'Active' },
    'resident@demo.com': { password: 'Test123!', role: 'Occupant',        occupantType: 'Resident', fullName: 'Siti Rahimah (Resident)',  passwordStatus: 'Active' },
    'tech@demo.com':     { password: 'Test123!', role: 'Technician',                                fullName: 'Daniel Tan',              passwordStatus: 'Active' },
    'admin@demo.com':    { password: 'Test123!', role: 'PropertyManager',                           fullName: 'Ahmad Fauzi (Manager)',   passwordStatus: 'Active' },
    // First-login simulations:
    'new.tenant@demo.com': { password: '',        role: 'Occupant',       occupantType: 'Tenant',   fullName: 'New Tenant',              passwordStatus: 'None' },
    'new.staff@demo.com':  { password: 'TEMP9999', role: 'Technician',                              fullName: 'New Technician',          passwordStatus: 'Temporary' },
  };

  constructor(private http: HttpClient, private router: Router) {
    const stored = localStorage.getItem('currentUser');
    if (stored) this.currentUserSubject.next(JSON.parse(stored));
  }

  // ── STEP 1: Check if email exists in UserAccount table ───────────────────────
  // Real-world: POST /api/Auth/check-email → { found, roleType, passwordStatus }
  // Decision: Dedicated endpoint (NOT login). Reason: separates "does this user exist"
  // from "authenticate". LinkedIn, Microsoft, Google all use this pattern.
  // It also prevents leaking which password failed vs which email is unknown.
  checkEmail(email: string): Observable<LoginCheckResult> {
    const demo = this.DEMO_USERS[email.toLowerCase()];
    if (demo !== undefined) {
      // Simulate DB hit
      return of({
        found: true,
        roleType: demo.role,
        occupantType: demo.occupantType,
        accountStatus: 'Active',
        passwordStatus: demo.passwordStatus,
      });
    }
    if (email.endsWith('@demo.com')) {
      // Unknown demo email = not found
      return of({ found: false });
    }
    // Real API call
    return this.http.post<LoginCheckResult>(`${this.apiUrl}/Auth/check-email`, { email });
  }

  // ── STEP 2 (Owner path): Verify IC/Passport in Occupant table ───────────────
  // Real-world: POST /api/Auth/verify-ic
  // Returns a short-lived updateToken (15 min JWT with scope=email-update-only).
  // This token is NOT a full session — it only allows the next 2 steps:
  //   (a) update email  (b) set password
  // Pattern used by: banking portals for forgotten credentials.
  verifyIc(identificationNo: string): Observable<VerifyIcResult> {
    if (identificationNo === '900101-10-1234') {
      return of({ found: true, maskedEmail: 'n****@gmail.com', updateToken: 'ic-update-token-demo' });
    }
    if (identificationNo === '000000-00-0000') {
      return of({ found: false });
    }
    return this.http.post<VerifyIcResult>(`${this.apiUrl}/Auth/verify-ic`, { identificationNo });
  }

  // ── STEP 2b: Update email after IC verification ──────────────────────────────
  // Called when owner found by IC but wants to correct their email.
  // Real-world: PATCH /api/Auth/update-email  (requires updateToken in header)
  updateEmailByIc(updateToken: string, newEmail: string): Observable<void> {
    if (updateToken === 'ic-update-token-demo') return of(undefined);
    return this.http.patch<void>(`${this.apiUrl}/Auth/update-email`, { email: newEmail },
      { headers: { 'X-Update-Token': updateToken } });
  }

  // ── STEP 3a (Staff): Verify temporary password ───────────────────────────────
  // For PropertyManager/Technician first login.
  // Real-world: POST /api/Auth/verify-temp-password
  // Returns a tempVerifiedToken scoped to password-set-only (not a full session).
  verifyTempPassword(dto: VerifyTempPasswordDto): Observable<{ tempVerifiedToken: string }> {
    const demo = this.DEMO_USERS[dto.email.toLowerCase()];
    if (demo?.passwordStatus === 'Temporary' && dto.temporaryPassword === demo.password) {
      return of({ tempVerifiedToken: 'temp-verified-demo-token' });
    }
    if (demo?.passwordStatus === 'Temporary') {
      return throwError(() => ({ error: { message: 'Incorrect temporary password. Please check your activation email.' } }));
    }
    return this.http.post<{ tempVerifiedToken: string }>(`${this.apiUrl}/Auth/verify-temp-password`, dto);
  }

  // ── STEP 3b (First login & after IC bypass): Set permanent password ──────────
  // Real-world: POST /api/Auth/set-password
  // Validates: min 8 chars, 1 uppercase, 1 number, 1 special char.
  // On success: activates account (status: Pending → Active) + returns full session.
  setPassword(dto: SetPasswordDto): Observable<LoginResponse> {
    if (dto.email.includes('demo.com')) {
      const mockResp: LoginResponse = {
        token: 'demo-token-' + Date.now(),
        userId: 99, email: dto.email,
        role: this.DEMO_USERS[dto.email]?.role || 'Occupant',
        fullName: this.DEMO_USERS[dto.email]?.fullName || 'New User',
        accountStatus: 'Active',
      };
      this._storeSession(mockResp);
      return of(mockResp);
    }
    return this.http.post<LoginResponse>(`${this.apiUrl}/Auth/set-password`, dto);
  }

  // ── STEP 4 (Normal login): Verify password ───────────────────────────────────
  login(request: LoginRequest): Observable<LoginResponse> {
    const demo = this.DEMO_USERS[request.email.toLowerCase()];
    if (demo && request.password === demo.password && demo.passwordStatus === 'Active') {
      const mockResp: LoginResponse = {
        token: 'demo-token-' + Date.now(),
        userId: Object.keys(this.DEMO_USERS).indexOf(request.email.toLowerCase()) + 1,
        email: request.email,
        role: demo.role,
        occupantType: demo.occupantType,
        fullName: demo.fullName,
        accountStatus: 'Active',
      };
      this._storeSession(mockResp);
      return of(mockResp);
    }
    if (demo && demo.passwordStatus === 'Active') {
      return throwError(() => ({ error: { message: 'Incorrect password. Please try again.' } }));
    }
    return this.http.post<LoginResponse>(`${this.apiUrl}/Auth/login`, request);
  }

  // ── Profile: Change password ─────────────────────────────────────────────────
  changePassword(dto: ChangePasswordDto): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/Auth/change-password`, dto);
  }

  // ── Session management ───────────────────────────────────────────────────────
  private _storeSession(resp: LoginResponse): void {
    localStorage.setItem('token', resp.token);
    localStorage.setItem('currentUser', JSON.stringify(resp));
    this.currentUserSubject.next(resp);
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('currentUser');
    this.currentUserSubject.next(null);
    this.router.navigate(['/auth/login']);
  }

  getToken(): string | null                  { return localStorage.getItem('token'); }
  getCurrentUser(): LoginResponse | null     { return this.currentUserSubject.value; }
  isLoggedIn(): boolean                      { return !!this.getToken(); }
  hasRole(roles: string[]): boolean          {
    const u = this.getCurrentUser();
    return !!u && roles.includes(u.role);
  }
  isOwner(): boolean {
    const u = this.getCurrentUser();
    return !!u && u.role === 'Occupant' && u.occupantType === 'Owner';
  }

  /** Navigate to correct dashboard after login */
  navigateToDashboard(role: string): void {
    const map: Record<string, string> = {
      Occupant: '/tenant/dashboard',
      Technician: '/technician/dashboard',
      PropertyManager: '/manager/dashboard',
    };
    this.router.navigate([map[role] || '/auth/login']);
  }
}