import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, catchError, finalize, map, shareReplay, switchMap, tap, throwError } from 'rxjs';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  userId: string;
  displayName: string;
  token: string;
  profileImageBase64?: string | null;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  email: string;
  password: string;
  profileImageBase64?: string | null;
}

export interface RegisterResponse {
  firstName: string;
  lastName: string;
  email: string;
  profileImageBase64?: string | null;
}

export interface UserProfileResponse {
  firstName: string;
  lastName: string;
  dateOfBirth?: string | null;
  email: string;
  profileImageBase64?: string | null;
}

export interface UpdateUserProfileRequest {
  firstName: string;
  lastName: string;
  dateOfBirth?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly profileEndpoint = '/api/auth/profile';
  private readonly profileImageEndpoint = '/api/auth/profile-image';
  private readonly refreshEndpoint = '/api/auth/refresh';
  private readonly refreshLeadSeconds = 120;
  private readonly profileFallbackOrigins = [
    'http://localhost:5237',
    'https://localhost:7241',
    'https://localhost:44367',
    'http://localhost:33823',
  ];
  private refreshInFlight$: Observable<string> | null = null;

  constructor(private http: HttpClient) {}

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', payload);
  }

  register(payload: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>('/api/auth/register', payload);
  }

  updateProfileImage(profileImageBase64: string | null): Observable<{ profileImageBase64: string | null }> {
    return this.withFreshToken((headers) =>
      this.http.put<{ profileImageBase64: string | null }>(
        this.profileImageEndpoint,
        { profileImageBase64 },
        { headers }
      ).pipe(
        catchError((err) => {
          if (!this.shouldTryNextOrigin(err)) {
            return throwError(() => err);
          }

          return this.tryProfileImageFallback(profileImageBase64, headers, 0);
        })
      )
    );
  }

  getProfile(): Observable<UserProfileResponse> {
    return this.withFreshToken((headers) =>
      this.http.get<UserProfileResponse>(this.profileEndpoint, { headers }).pipe(
        catchError((err) => {
          if (!this.shouldTryNextOrigin(err)) {
            return throwError(() => err);
          }

          return this.tryGetProfileFallback(headers, 0);
        })
      )
    );
  }

  updateProfile(payload: UpdateUserProfileRequest): Observable<UserProfileResponse> {
    return this.withFreshToken((headers) =>
      this.http.put<UserProfileResponse>(this.profileEndpoint, payload, { headers }).pipe(
        catchError((err) => {
          if (!this.shouldTryNextOrigin(err)) {
            return throwError(() => err);
          }

          return this.tryUpdateProfileFallback(payload, headers, 0);
        })
      )
    );
  }

  private tryGetProfileFallback(headers: HttpHeaders, index: number): Observable<UserProfileResponse> {
    if (index >= this.profileFallbackOrigins.length) {
      return throwError(() => new Error('Profile endpoint not found on configured local origins.'));
    }

    const url = `${this.profileFallbackOrigins[index]}${this.profileEndpoint}`;
    return this.http.get<UserProfileResponse>(url, { headers }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetProfileFallback(headers, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryUpdateProfileFallback(
    payload: UpdateUserProfileRequest,
    headers: HttpHeaders,
    index: number
  ): Observable<UserProfileResponse> {
    if (index >= this.profileFallbackOrigins.length) {
      return throwError(() => new Error('Profile update endpoint not found on configured local origins.'));
    }

    const url = `${this.profileFallbackOrigins[index]}${this.profileEndpoint}`;
    return this.http.put<UserProfileResponse>(url, payload, { headers }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryUpdateProfileFallback(payload, headers, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryProfileImageFallback(
    profileImageBase64: string | null,
    headers: HttpHeaders,
    index: number
  ): Observable<{ profileImageBase64: string | null }> {
    if (index >= this.profileFallbackOrigins.length) {
      return throwError(() => new Error('Profile image endpoint not found on configured local origins.'));
    }

    const url = `${this.profileFallbackOrigins[index]}${this.profileImageEndpoint}`;
    return this.http.put<{ profileImageBase64: string | null }>(url, { profileImageBase64 }, { headers }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryProfileImageFallback(profileImageBase64, headers, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private withFreshToken<T>(requestFactory: (headers: HttpHeaders) => Observable<T>): Observable<T> {
    const token = this.getStoredToken();
    if (!token) {
      return throwError(() => new Error('Missing auth token.'));
    }

    if (!this.isTokenExpiringSoon(token)) {
      return requestFactory(this.buildAuthHeaders(token));
    }

    return this.refreshToken().pipe(
      switchMap((refreshedToken) => requestFactory(this.buildAuthHeaders(refreshedToken)))
    );
  }

  private refreshToken(): Observable<string> {
    if (this.refreshInFlight$) {
      return this.refreshInFlight$;
    }

    const token = this.getStoredToken();
    if (!token) {
      return throwError(() => new Error('Missing auth token.'));
    }

    const headers = this.buildAuthHeaders(token);
    const refreshRequest$ = this.http.post<LoginResponse>(this.refreshEndpoint, {}, { headers }).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryRefreshFallback(headers, 0);
      }),
      tap((res) => {
        localStorage.setItem('holonix_token', res.token);
        localStorage.setItem('holonix_display_name', res.displayName);
        if (res.profileImageBase64) {
          localStorage.setItem('holonix_profile_image_base64', res.profileImageBase64);
        } else {
          localStorage.removeItem('holonix_profile_image_base64');
        }
      }),
      map((res) => res.token),
      finalize(() => {
        this.refreshInFlight$ = null;
      }),
      shareReplay(1)
    );

    this.refreshInFlight$ = refreshRequest$;
    return refreshRequest$;
  }

  private tryRefreshFallback(headers: HttpHeaders, index: number): Observable<LoginResponse> {
    if (index >= this.profileFallbackOrigins.length) {
      return throwError(() => new Error('Refresh endpoint not found on configured local origins.'));
    }

    const url = `${this.profileFallbackOrigins[index]}${this.refreshEndpoint}`;
    return this.http.post<LoginResponse>(url, {}, { headers }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryRefreshFallback(headers, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private buildAuthHeaders(token: string): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  private getStoredToken(): string {
    return (localStorage.getItem('holonix_token') ?? '').trim();
  }

  private isTokenExpiringSoon(token: string): boolean {
    try {
      const tokenParts = token.split('.');
      if (tokenParts.length < 2) {
        return true;
      }

      const base64 = tokenParts[1].replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
      const payloadJson = atob(padded);
      const payload = JSON.parse(payloadJson) as { exp?: number };
      if (!payload.exp) {
        return true;
      }

      const nowEpochSeconds = Math.floor(Date.now() / 1000);
      return payload.exp - nowEpochSeconds <= this.refreshLeadSeconds;
    } catch {
      return true;
    }
  }

  private shouldTryNextOrigin(err: { status?: number } | null | undefined): boolean {
    const status = err?.status;
    return status === 0 || status === 404;
  }
}
