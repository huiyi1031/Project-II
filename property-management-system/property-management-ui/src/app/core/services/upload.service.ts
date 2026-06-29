import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { TempUploadResult } from '../models';

/**
 * Two-phase document upload service.
 * Real-world pattern used by AWS S3, Azure Blob, Google Cloud Storage:
 *   Phase 1: Upload file → receive a short-lived fileRef (UUID)
 *   Phase 2: Include fileRef in the main form submission
 * This prevents large file re-uploads if form validation fails,
 * and separates file storage from business logic.
 */
@Injectable({ providedIn: 'root' })
export class UploadService {
  private base = 'http://localhost:5004/api';

  constructor(private http: HttpClient) {}

  /** Phase 1: Upload file to temp storage, get a fileRef back */
  uploadTemp(file: File): Observable<TempUploadResult> {
    // Demo mode simulation
    if (!file) return of({ fileRef: '', fileName: '', expiresAt: '' });

    // Simulate API for demo
    const demoResult: TempUploadResult = {
      fileRef: 'temp-' + Math.random().toString(36).slice(2, 10),
      fileName: file.name,
      expiresAt: new Date(Date.now() + 3600_000).toISOString(), // 1 hour
    };
    return of(demoResult);

    // Real API:
    // const form = new FormData();
    // form.append('file', file);
    // return this.http.post<TempUploadResult>(`${this.base}/Upload/temp`, form);
  }
}
