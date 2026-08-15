import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  MaintenanceRequest, CreateMaintenanceRequestDto, DashboardStats,
  MaintenanceRequestDetail, MaintenanceRequestFilter, MaintenanceRequester, PagedResponse
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
  getMyRequests(status?: string): Observable<PagedResponse<MaintenanceRequest>> {
    let params = new HttpParams();
    if (status && status !== 'All') params = params.set('status', status);
    return this.http.get<PagedResponse<MaintenanceRequest>>(`${this.base}/MaintenanceRequests/my`, { params });
  }

  getAllRequests(status?: string, date?: string): Observable<MaintenanceRequest[]> {
    let params = new HttpParams();
    if (status && status !== 'All') params = params.set('status', status);
    if (date) params = params.set('date', date);
    return this.http.get<MaintenanceRequest[]>(`${this.base}/MaintenanceRequests`, { params });
  }

  getRequestPage(filter: MaintenanceRequestFilter): Observable<PagedResponse<MaintenanceRequest>> {
    let params = new HttpParams();
    Object.entries(filter).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, value.toString());
      }
    });
    return this.http.get<PagedResponse<MaintenanceRequest>>(`${this.base}/MaintenanceRequests/paged`, { params });
  }

  getRequesters(): Observable<MaintenanceRequester[]> {
    return this.http.get<MaintenanceRequester[]>(`${this.base}/MaintenanceRequests/requesters`);
  }

  getRequestById(id: number): Observable<MaintenanceRequestDetail> {
    return this.http.get<MaintenanceRequestDetail>(`${this.base}/MaintenanceRequests/${id}`);
  }

  createRequest(data: FormData | CreateMaintenanceRequestDto): Observable<MaintenanceRequestDetail> {
    return this.http.post<MaintenanceRequestDetail>(`${this.base}/MaintenanceRequests`, data);
  }

  updateRequest(id: number, data: any): Observable<MaintenanceRequestDetail> {
    return this.http.put<MaintenanceRequestDetail>(`${this.base}/MaintenanceRequests/${id}`, data);
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

  cancelRequest(id: number, reason: string): Observable<void> {
    return this.http.post<void>(`${this.base}/MaintenanceRequests/${id}/cancel`, { reason });
  }

  scheduleRequest(id: number, scheduledDate: string): Observable<void> {
    return this.http.post<void>(`${this.base}/MaintenanceRequests/${id}/schedule`, { scheduledDate });
  }

  getCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}/MaintenanceRequests/categories`);
  }
}
