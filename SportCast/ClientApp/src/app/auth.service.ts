import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  userId: string;
  displayName: string;
  token: string;
}

export interface RegisterRequest {
  firstname: string;
  lastname: string;
  dateOfBirth: string;
  email: string;
  password: string;
}

export interface RegisterResponse {
  firstname: string;
  lastname: string;
  dateOfBirth: string;
  email: string;
  password: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  constructor(private http: HttpClient) {}

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', payload);
  }

  register(payload: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>('/api/auth/register', payload);
  }
}
