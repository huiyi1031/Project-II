import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import {
  VerifyIcResult, SetPasswordDto,
  VerifyTempPasswordDto, ChangePasswordDto
} from '../models';

export interface LoginRequest  { email: string; password: string; }
export interface LoginResponse { 
  token?: string; 
  userId: number; 
  email: string; 
  role: string; 
  fullName?: string; 
  accountStatus?: string; 
  /** Only present when role === 'Occupant': 'Owner' | 'Tenant' | 'Resident' */
  occupantType?: string;
  requiresPasswordChange: boolean;
  updateToken?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = 'https://localhost:7147/api'; // Or your actual .NET port (e.g. 5004) - adjust if needed
  private currentUserSubject = new BehaviorSubject<LoginResponse | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {
    // If you are using port 5004, ensure this URL matches. I'll default to localhost:5004 for safety.
    this.apiUrl = 'http://localhost:5004/api'; 
    const stored = localStorage.getItem('currentUser');
    if (stored) this.currentUserSubject.next(JSON.parse(stored));
  }

  // ── STEP 1: Normal login ───────────────────────────────────────────────────
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/Auth/login`, request).pipe(
      tap(resp => {
        if (!resp.requiresPasswordChange) {
          this._storeSession(resp);
        }
      })
    );
  }

  // ── STEP 2 (Owner path): Verify IC/Passport in Occupant table ───────────────
  verifyIc(identificationNo: string): Observable<VerifyIcResult> {
    return this.http.post<VerifyIcResult>(`${this.apiUrl}/Auth/verify-ic`, { identificationNo });
  }

  // ── STEP 2b: Update email after IC verification ──────────────────────────────
  updateEmailByIc(updateToken: string, newEmail: string): Observable<void> {
    const headers = new HttpHeaders().set('X-Update-Token', updateToken);
    return this.http.patch<void>(`${this.apiUrl}/Auth/update-email`, { email: newEmail }, { headers });
  }

  // ── STEP 3a (Staff): Verify temporary password ───────────────────────────────
  verifyTempPassword(dto: VerifyTempPasswordDto): Observable<{ tempVerifiedToken: string }> {
    return this.http.post<{ tempVerifiedToken: string }>(`${this.apiUrl}/Auth/verify-temp-password`, dto);
  }

  // ── STEP 3b (First login & after IC bypass): Set permanent password ──────────
  setPassword(dto: SetPasswordDto): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/Auth/set-password`, dto).pipe(
      tap(resp => {
        this._storeSession(resp);
      })
    );
  }

  // ── Profile: Change password ─────────────────────────────────────────────────
  changePassword(dto: ChangePasswordDto): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/Auth/change-password`, dto);
  }

  // ── Session management ───────────────────────────────────────────────────────
  private _storeSession(resp: LoginResponse): void {
    if (resp.token) {
      localStorage.setItem('token', resp.token);
    }
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
  navigateToDashboard(role: string, occupantType?: string): void {
    if (role === 'Occupant') {
      // Owners get their own shell; Tenants & Residents share the tenant shell
      this.router.navigate(['/tenant/dashboard']);
    } else if (role === 'Technician') {
      this.router.navigate(['/technician/dashboard']);
    } else if (role === 'PropertyManager') {
      this.router.navigate(['/manager/dashboard']);
    } else {
      this.router.navigate(['/auth/login']);
    }
  }
}