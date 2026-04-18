import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse } from '../../api/generated/model/authResponse';

export interface UpdateProfileBody {
  displayName: string;
}

@Injectable({ providedIn: 'root' })
export class ProfileApiService {
  private readonly http = inject(HttpClient);

  updateMe(body: UpdateProfileBody): Observable<AuthResponse> {
    return this.http.put<AuthResponse>(`${environment.apiBaseUrl}/api/Profile/me`, body);
  }
}
