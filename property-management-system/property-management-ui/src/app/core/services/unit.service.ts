import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PropertyUnit } from '../models';

export interface UnitFilterOptions {
  blocks: string[];
  floors: string[];
}

export interface CreateUnitDto {
  propertyId: number;
  unitNumber: string;
  floorLevel?: string;
  block?: string;
  unitType: string;
  areaSqft?: number;
  bedrooms?: number;
  bathrooms?: number;
  maxOccupants: number;
  status: string;
}

export interface UpdateUnitDto {
  unitNumber: string;
  floorLevel?: string;
  block?: string;
  unitType?: string;
  areaSqft?: number;
  bedrooms?: number;
  bathrooms?: number;
  maxOccupants: number;
  status?: string;
}

@Injectable({ providedIn: 'root' })
export class UnitService {
  private base = 'http://localhost:5004/api/PropertyUnits';

  constructor(private http: HttpClient) {}

  getAll(filters?: {
    search?: string;
    block?: string;
    floorLevel?: string;
    unitType?: string;
    minSqft?: number;
    maxSqft?: number;
    status?: string;
    propertyId?: number;
  }): Observable<PropertyUnit[]> {
    let params = new HttpParams();
    if (filters?.search)      params = params.set('search', filters.search);
    if (filters?.block)        params = params.set('block', filters.block);
    if (filters?.floorLevel)   params = params.set('floorLevel', filters.floorLevel);
    if (filters?.unitType)     params = params.set('unitType', filters.unitType);
    if (filters?.minSqft != null) params = params.set('minSqft', filters.minSqft);
    if (filters?.maxSqft != null) params = params.set('maxSqft', filters.maxSqft);
    if (filters?.status)       params = params.set('status', filters.status);
    if (filters?.propertyId)   params = params.set('propertyId', filters.propertyId);
    return this.http.get<PropertyUnit[]>(this.base, { params });
  }

  getById(id: number): Observable<PropertyUnit> {
    return this.http.get<PropertyUnit>(`${this.base}/${id}`);
  }

  create(dto: CreateUnitDto): Observable<any> {
    return this.http.post<any>(this.base, dto);
  }

  update(id: number, dto: UpdateUnitDto): Observable<any> {
    return this.http.put<any>(`${this.base}/${id}`, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete<any>(`${this.base}/${id}`);
  }

  getFilterOptions(propertyId?: number): Observable<UnitFilterOptions> {
    let params = new HttpParams();
    if (propertyId) params = params.set('propertyId', propertyId);
    return this.http.get<UnitFilterOptions>(`${this.base}/filter-options`, { params });
  }
}
