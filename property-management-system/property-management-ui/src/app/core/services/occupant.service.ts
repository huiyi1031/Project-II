import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Occupant, PropertyUnit, Contract } from '../models';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class OccupantService {
  private base = 'http://localhost:5004/api';
  constructor(private http: HttpClient) {}

  // Occupants (Manager view)
  getAllOccupants(filter?: string, page = 1, pageSize = 10, search = ''): Observable<PagedResult<Occupant>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (filter && filter !== 'All') params = params.set('roleType', filter);
    if (search.trim()) params = params.set('search', search.trim());

    return this.http.get<PagedResult<Occupant>>(`${this.base}/Occupants`, { params });
  }

  getOccupantById(id: number): Observable<Occupant>          { return this.http.get<Occupant>(`${this.base}/Occupants/${id}`); }
  createOccupant(dto: any): Observable<Occupant>             { return this.http.post<Occupant>(`${this.base}/Occupants`, dto); }
  updateOccupant(id: number, dto: any): Observable<Occupant> { return this.http.put<Occupant>(`${this.base}/Occupants/${id}`, dto); }
  deactivateOccupant(id: number): Observable<Occupant>       { return this.http.patch<Occupant>(`${this.base}/Occupants/${id}/deactivate`, {}); }
  activateOccupant(id: number): Observable<Occupant>         { return this.http.patch<Occupant>(`${this.base}/Occupants/${id}/activate`, {}); }
  deleteOccupant(id: number): Observable<{ occupantID: number; message: string }> { return this.http.delete<{ occupantID: number; message: string }>(`${this.base}/Occupants/${id}`); }

  // My Profile (self-service)
  getMyProfile(): Observable<Occupant>                       { return this.http.get<Occupant>(`${this.base}/Occupants/me`); }
  updateMyProfile(dto: any): Observable<Occupant>            { return this.http.put<Occupant>(`${this.base}/Occupants/me`, dto); }
  getMyContracts(): Observable<Contract[]>                   { return this.http.get<Contract[]>(`${this.base}/Contracts/my`); }

  // Family Members (Resident/Owner)
  getMyFamilyMembers(): Observable<any[]>                    { return this.http.get<any[]>(`${this.base}/Occupants/me/family`); }
  addFamilyMember(dto: any): Observable<any>                 { return this.http.post<any>(`${this.base}/Occupants/me/family`, dto); }
  removeFamilyMember(occupantID: number): Observable<void>   { return this.http.delete<void>(`${this.base}/Occupants/me/family/${occupantID}`); }
  getUnitHeadcount(): Observable<any>                        { return this.http.get<any>(`${this.base}/PropertyUnits/my/headcount`); }

  // Tenants (Owner)
  getMyTenants(): Observable<any[]>                          { return this.http.get<any[]>(`${this.base}/Occupants/me/tenants`); }
  addTenant(dto: any): Observable<any>                       { return this.http.post<any>(`${this.base}/Occupants/me/tenants`, dto); }
  removeTenant(occupantID: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/Occupants/me/tenants/${occupantID}`);
  }

  // Property Units (Manager)
  getAllUnits(): Observable<PropertyUnit[]>                   { return this.http.get<PropertyUnit[]>(`${this.base}/PropertyUnits`); }
  getUnitById(id: number): Observable<PropertyUnit>          { return this.http.get<PropertyUnit>(`${this.base}/PropertyUnits/${id}`); }
  createUnit(dto: Partial<PropertyUnit>): Observable<PropertyUnit> { return this.http.post<PropertyUnit>(`${this.base}/PropertyUnits`, dto); }
  updateUnit(id: number, dto: Partial<PropertyUnit>): Observable<PropertyUnit> { return this.http.put<PropertyUnit>(`${this.base}/PropertyUnits/${id}`, dto); }
}




