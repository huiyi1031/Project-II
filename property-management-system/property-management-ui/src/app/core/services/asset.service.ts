import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Asset, MaintenancePlan, AssetMaintenanceHistory } from '../models';

@Injectable({ providedIn: 'root' })
export class AssetService {
  private base = 'http://localhost:5004/api';

  constructor(private http: HttpClient) {}

  // Assets
  getAllAssets(): Observable<Asset[]> {
    return this.http.get<Asset[]>(`${this.base}/Assets`);
  }

  getHighRiskAssets(): Observable<Asset[]> {
    return this.http.get<Asset[]>(`${this.base}/Assets/high-risk`);
  }

  getAssetById(id: number): Observable<Asset> {
    return this.http.get<Asset>(`${this.base}/Assets/${id}`);
  }

  createAsset(dto: Partial<Asset>): Observable<Asset> {
    return this.http.post<Asset>(`${this.base}/Assets`, dto);
  }

  updateAsset(id: number, dto: Partial<Asset>): Observable<void> {
    return this.http.put<void>(`${this.base}/Assets/${id}`, dto);
  }

  generateQrCode(id: number): Observable<{ qrCode: string }> {
    return this.http.post<{ qrCode: string }>(`${this.base}/Assets/${id}/qr`, {});
  }

  // Maintenance Plans
  getAllPlans(): Observable<MaintenancePlan[]> {
    return this.http.get<MaintenancePlan[]>(`${this.base}/MaintenancePlans`);
  }

  createPlan(dto: Partial<MaintenancePlan>): Observable<MaintenancePlan> {
    return this.http.post<MaintenancePlan>(`${this.base}/MaintenancePlans`, dto);
  }

  // Asset History
  getAssetHistory(assetId: number): Observable<AssetMaintenanceHistory[]> {
    return this.http.get<AssetMaintenanceHistory[]>(`${this.base}/Assets/${assetId}/history`);
  }
}
