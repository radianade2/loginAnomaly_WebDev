import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// Bentuk data yang dikirim & diterima
export interface LoginRequest {
  username: string;
  password: string;
  ipAddress?: string;
  latitude?: number;
  longitude?: number;
  deviceFingerprint?: string;
}

export interface LoginResponse {
  decision: string;
  score: number;
  token?: string;
  message?: string;
  otp?: string;              // muncul saat challenge (demo)
  locked?: boolean;          // true saat sedang terkunci
  remainingMinutes?: number; // remaining waktu lockout
}

export interface VerifyOtpResponse {
  decision: string;
  token?: string;
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class Auth {
  private apiUrl = 'http://localhost:5108/api/auth';

  constructor(private http: HttpClient) {}

  login(data: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, data);
  }

  verifyOtp(username: string, otpCode: string): Observable<VerifyOtpResponse> {
    return this.http.post<VerifyOtpResponse>(`${this.apiUrl}/verify-otp`, { username, otpCode });
  }

  saveToken(token: string): void {
    localStorage.setItem('token', token);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  logout(): void {
    localStorage.removeItem('token');
  }

  isLoggedIn(): boolean {
    return this.getToken() !== null;
  }
}
