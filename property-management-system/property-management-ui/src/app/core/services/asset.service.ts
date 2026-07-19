import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Asset, AssetMaintenanceHistory } from '../models';

export interface CreateAssetDto {
  propertyId: number;
  assetName: string;
  assetType?: string;
  location?: string;
  installationDate: string;
  manufacturer?: string;
  modelNumber?: string;
  expLifespanYears: number;
  maintenanceIntervalDays: number;
  supplierName?: string;
  warrantyExpiryDate?: string;
}

export interface UpdateAssetDto extends CreateAssetDto {
  nextMaintenanceDueDate?: string;
  status?: string;
}

export interface AddHistoryDto {
  maintenanceType: number;  // 0=Corrective, 1=Preventive, 2=Inspection
  description?: string;
  cost?: number;
  maintenanceDate: string;
  resultStatus?: string;
  performedBy?: string;
}

@Injectable({ providedIn: 'root' })
export class AssetService {
  private base = 'http://localhost:5004/api/Assets';

  constructor(private http: HttpClient) {}

  getAll(filters?: {
    search?: string;
    assetType?: string;
    status?: string;
    propertyId?: number;
  }): Observable<Asset[]> {
    let params = new HttpParams();
    if (filters?.search)    params = params.set('search', filters.search);
    if (filters?.assetType) params = params.set('assetType', filters.assetType);
    if (filters?.status)    params = params.set('status', filters.status);
    if (filters?.propertyId) params = params.set('propertyId', filters.propertyId);
    return this.http.get<Asset[]>(this.base, { params });
  }

  getById(id: number): Observable<any> {
    return this.http.get<any>(`${this.base}/${id}`);
  }

  create(dto: CreateAssetDto): Observable<any> {
    return this.http.post<any>(this.base, dto);
  }

  update(id: number, dto: UpdateAssetDto): Observable<any> {
    return this.http.put<any>(`${this.base}/${id}`, dto);
  }

  deactivate(id: number): Observable<any> {
    return this.http.patch<any>(`${this.base}/${id}/deactivate`, {});
  }

  getHistory(id: number): Observable<AssetMaintenanceHistory[]> {
    return this.http.get<AssetMaintenanceHistory[]>(`${this.base}/${id}/history`);
  }

  addHistory(id: number, dto: AddHistoryDto): Observable<any> {
    return this.http.post<any>(`${this.base}/${id}/history`, dto);
  }

  /** Returns QR code image URL using free public API (no npm needed) */
  getQrImageUrl(qrCode: string, size = 150): string {
    return `https://api.qrserver.com/v1/create-qr-code/?size=${size}x${size}&data=${encodeURIComponent(qrCode)}&format=png&margin=10`;
  }
}
