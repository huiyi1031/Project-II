import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WorkOrder, WorkAssignment, CreateWorkOrderDto } from '../models';

@Injectable({ providedIn: 'root' })
export class WorkOrderService {
  private base = 'http://localhost:5004/api/WorkOrders';

  constructor(private http: HttpClient) {}

  getMyWorkOrders(status?: string): Observable<WorkOrder[]> {
    let params = new HttpParams();
    if (status && status !== 'All') params = params.set('status', status);
    return this.http.get<WorkOrder[]>(`${this.base}/my`, { params });
  }

  getAllWorkOrders(status?: string): Observable<WorkOrder[]> {
    let params = new HttpParams();
    if (status && status !== 'All') params = params.set('status', status);
    return this.http.get<WorkOrder[]>(`${this.base}`, { params });
  }

  getWorkOrderById(id: number): Observable<WorkOrder> {
    return this.http.get<WorkOrder>(`${this.base}/${id}`);
  }

  createWorkOrder(dto: CreateWorkOrderDto): Observable<WorkOrder> {
    return this.http.post<WorkOrder>(`${this.base}`, dto);
  }

  updateWorkOrder(id: number, dto: Partial<WorkOrder>): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, dto);
  }

  updateStatus(id: number, status: string, report?: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/status`, { status, workReport: report });
  }

  acknowledgeWorkOrder(id: number, decision: string, declineReason?: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/acknowledge`, { decision, declineReason });
  }

  submitCompletion(id: number, report: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/complete`, { completionReport: report });
  }

  getAssignments(workOrderId: number): Observable<WorkAssignment[]> {
    return this.http.get<WorkAssignment[]>(`${this.base}/${workOrderId}/assignments`);
  }
}
