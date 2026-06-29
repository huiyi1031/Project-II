import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  MaintenanceRequest, CreateMaintenanceRequestDto, DashboardStats
} from '../models';

@Injectable({ providedIn: 'root' })
export class MaintenanceService {
  private base = 'http://localhost:5004/api';

  constructor(private http: HttpClient) {}

  // ── Dashboard ──────────────────────────────────────────────
  getDashboardStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.base}/Dashboard`);
  }

  // ── Maintenance Requests ────────────────────────────────────
  getMyRequests(status?: string): Observable<MaintenanceRequest[]> {
    let params = new HttpParams();
    if (status && status !== 'All') params = params.set('status', status);
    return this.http.get<MaintenanceRequest[]>(`${this.base}/MaintenanceRequests/my`, { params });
  }

  getAllRequests(status?: string, date?: string): Observable<MaintenanceRequest[]> {
    let params = new HttpParams();
    if (status && status !== 'All') params = params.set('status', status);
    if (date) params = params.set('date', date);
    return this.http.get<MaintenanceRequest[]>(`${this.base}/MaintenanceRequests`, { params });
  }

  getRequestById(id: number): Observable<MaintenanceRequest> {
    return this.http.get<MaintenanceRequest>(`${this.base}/MaintenanceRequests/${id}`);
  }

  createRequest(dto: CreateMaintenanceRequestDto): Observable<MaintenanceRequest> {
    return this.http.post<MaintenanceRequest>(`${this.base}/MaintenanceRequests`, dto);
  }

  updateRequestStatus(id: number, status: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/MaintenanceRequests/${id}/status`, { status });
  }

  approveRequest(id: number): Observable<void> {
    return this.http.post<void>(`${this.base}/MaintenanceRequests/${id}/approve`, {});
  }

  rejectRequest(id: number, reason: string): Observable<void> {
    return this.http.post<void>(`${this.base}/MaintenanceRequests/${id}/reject`, { reason });
  }
}
