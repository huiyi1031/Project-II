import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private base = 'http://localhost:5004/api/Users';

  constructor(private http: HttpClient) { }

  getProfile(): Observable<any> {
    return this.http.get<any>(`${this.base}/profile`);
  }

  updateProfile(data: any): Observable<any> {
    return this.http.put<any>(`${this.base}/profile`, data);
  }

  uploadProfilePicture(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<any>(`${this.base}/profile/picture`, formData);
  }
}
