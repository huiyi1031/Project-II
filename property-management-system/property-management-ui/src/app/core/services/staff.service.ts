import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Technician, PropertyManager, CreateStaffDto, UpdateStaffDto } from '../models';

@Injectable({ providedIn: 'root' })
export class StaffService {
  private base = 'http://localhost:5004/api/Staff';

  constructor(private http: HttpClient) {}

  getAllStaff(): Observable<(Technician | PropertyManager)[]> {
    return this.http.get<(Technician | PropertyManager)[]>(`${this.base}`);
  }

  getTechnicians(): Observable<Technician[]> {
    return this.http.get<Technician[]>(`${this.base}/technicians`);
  }

  getStaffById(id: number): Observable<Technician | PropertyManager> {
    return this.http.get<Technician | PropertyManager>(`${this.base}/${id}`);
  }

  createStaff(dto: CreateStaffDto): Observable<any> {
    return this.http.post<any>(`${this.base}`, dto);
  }

  updateStaff(id: number, dto: UpdateStaffDto): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, dto);
  }

  deactivateStaff(id: number, reason: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/deactivate`, { reason });
  }
}
