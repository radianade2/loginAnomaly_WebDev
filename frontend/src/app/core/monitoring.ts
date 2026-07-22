import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface LoginEventDto {
  id: number;
  username: string;
  ipAddress: string;
  latitude?: number;
  longitude?: number;
  loginSucceeded: boolean;
  riskScore: number;
  decision: string;
  isSimulated: boolean;
  timestampUtc: string;
}

export interface AlertDto {
  id: number;
  loginEventId: number;
  severity: string;
  summary: string;
  isAcknowledged: boolean;
  username: string;
  riskScore: number;
  timestampUtc: string;
}

export interface SummaryDto {
  total: number;
  suspicious: number;
  activeAlerts: number;
}

@Injectable({ providedIn: 'root' })
export class Monitoring {
  private apiUrl = 'http://localhost:5108/api';

  constructor(private http: HttpClient) {}

  getEvents(take = 50): Observable<LoginEventDto[]> {
    return this.http.get<LoginEventDto[]>(`${this.apiUrl}/events?take=${take}`);
  }

  getAlerts(onlyActive = false): Observable<AlertDto[]> {
    return this.http.get<AlertDto[]>(`${this.apiUrl}/alerts?onlyActive=${onlyActive}`);
  }

  getSummary(): Observable<SummaryDto> {
    return this.http.get<SummaryDto>(`${this.apiUrl}/stats/summary`);
  }

  acknowledge(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/alerts/${id}/ack`, {});
  }
}
