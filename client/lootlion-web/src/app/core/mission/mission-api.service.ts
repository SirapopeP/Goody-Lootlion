import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateMissionTemplateRequest,
  MissionInstanceDto,
  MissionInstanceStatus,
  MissionTemplateDto,
} from './mission.models';

/** HTTP client สำหรับ Mission Template + Instance API */
@Injectable({ providedIn: 'root' })
export class MissionApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/Missions`;

  listTemplates(householdId: string): Observable<MissionTemplateDto[]> {
    return this.http.get<MissionTemplateDto[]>(`${this.base}/templates/household/${householdId}`);
  }

  createTemplate(body: CreateMissionTemplateRequest): Observable<MissionTemplateDto> {
    return this.http.post<MissionTemplateDto>(`${this.base}/templates`, body);
  }

  cancelTemplate(templateId: string): Observable<MissionTemplateDto> {
    return this.http.post<MissionTemplateDto>(`${this.base}/templates/${templateId}/cancel`, null);
  }

  spawnTemplate(templateId: string): Observable<MissionInstanceDto> {
    return this.http.post<MissionInstanceDto>(`${this.base}/templates/${templateId}/spawn`, null);
  }

  listBoard(householdId: string): Observable<MissionInstanceDto[]> {
    return this.http.get<MissionInstanceDto[]>(`${this.base}/board/household/${householdId}`);
  }

  listMine(householdId: string, status?: MissionInstanceStatus): Observable<MissionInstanceDto[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<MissionInstanceDto[]>(`${this.base}/mine/household/${householdId}`, { params });
  }

  listPending(householdId: string): Observable<MissionInstanceDto[]> {
    return this.http.get<MissionInstanceDto[]>(`${this.base}/pending/household/${householdId}`);
  }

  claim(instanceId: string): Observable<MissionInstanceDto> {
    return this.http.post<MissionInstanceDto>(`${this.base}/instances/${instanceId}/claim`, null);
  }

  submit(instanceId: string): Observable<MissionInstanceDto> {
    return this.http.post<MissionInstanceDto>(`${this.base}/instances/${instanceId}/submit`, null);
  }

  approve(instanceId: string): Observable<MissionInstanceDto> {
    return this.http.post<MissionInstanceDto>(`${this.base}/instances/${instanceId}/approve`, null);
  }

  reject(instanceId: string): Observable<MissionInstanceDto> {
    return this.http.post<MissionInstanceDto>(`${this.base}/instances/${instanceId}/reject`, null);
  }
}
