import { Injectable } from '@angular/core';
import { HttpBackend, HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, Subject, catchError, finalize, map, of, shareReplay, tap, throwError } from 'rxjs';
import { LoginResponse } from './auth.service';

@Injectable({
  providedIn: 'root',
})
export class AuthSessionService {
  private readonly tokenKey = 'holonix_token';
  private readonly displayNameKey = 'holonix_display_name';
  private readonly profileImageKey = 'holonix_profile_image_base64';
  private readonly sessionExpiredKey = 'holonix_session_expired';
  private readonly refreshEndpoint = '/api/auth/refresh';
  private readonly refreshLeadSeconds = 120;
  private readonly fallbackOrigins = [
    'http://localhost:5237',
    'https://localhost:7241',
    'http://localhost:5000',
    'https://localhost:44367',
    'http://localhost:33823',
  ];

  private readonly rawHttp: HttpClient;
  private refreshInFlight$: Observable<string> | null = null;
  private readonly sessionChangedSubject = new Subject<void>();
  readonly sessionChanged$ = this.sessionChangedSubject.asObservable();

  constructor(
    httpBackend: HttpBackend,
    private readonly router: Router
  ) {
    this.rawHttp = new HttpClient(httpBackend);
  }

  getStoredToken(): string {
    return (localStorage.getItem(this.tokenKey) ?? '').trim();
  }

  hasToken(): boolean {
    return this.getStoredToken().length > 0;
  }

  persistSession(response: LoginResponse): void {
    localStorage.setItem(this.tokenKey, response.token);
    localStorage.setItem(this.displayNameKey, response.displayName);

    if (response.profileImageBase64) {
      localStorage.setItem(this.profileImageKey, response.profileImageBase64);
    } else {
      localStorage.removeItem(this.profileImageKey);
    }

    localStorage.removeItem(this.sessionExpiredKey);
    this.sessionChangedSubject.next();
  }

  clearSession(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.displayNameKey);
    localStorage.removeItem(this.profileImageKey);
    this.sessionChangedSubject.next();
  }

  logout(): void {
    this.clearSession();
    void this.router.navigate(['/home'], { replaceUrl: true });
  }

  logoutDueToExpiredSession(): void {
    this.clearSession();
    localStorage.setItem(this.sessionExpiredKey, '1');
    void this.router.navigate(['/login']);
  }

  consumeSessionExpiredFlag(): boolean {
    const value = localStorage.getItem(this.sessionExpiredKey) === '1';
    if (value) {
      localStorage.removeItem(this.sessionExpiredKey);
    }

    return value;
  }

  hasSessionExpiredFlag(): boolean {
    return localStorage.getItem(this.sessionExpiredKey) === '1';
  }

  getValidToken(): Observable<string | null> {
    const token = this.getStoredToken();
    if (!token) {
      return of(null);
    }

    if (!this.isTokenExpiringSoon(token)) {
      return of(token);
    }

    return this.refreshToken().pipe(
      catchError(() => {
        this.logoutDueToExpiredSession();
        return of(null);
      })
    );
  }

  refreshToken(): Observable<string> {
    if (this.refreshInFlight$) {
      return this.refreshInFlight$;
    }

    const token = this.getStoredToken();
    if (!token) {
      return throwError(() => new Error('Missing auth token.'));
    }

    const headers = new HttpHeaders({ Authorization: `Bearer ${token}` });
    const refreshRequest$ = this.rawHttp.post<LoginResponse>(this.refreshEndpoint, {}, { headers }).pipe(
      catchError((err) => this.tryRefreshFallback(headers, 0, err)),
      tap((response) => this.persistSession(response)),
      map((response) => response.token),
      finalize(() => {
        this.refreshInFlight$ = null;
      }),
      shareReplay(1)
    );

    this.refreshInFlight$ = refreshRequest$;
    return refreshRequest$;
  }

  private tryRefreshFallback(headers: HttpHeaders, index: number, originalError: unknown): Observable<LoginResponse> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => originalError);
    }

    const url = `${this.fallbackOrigins[index]}${this.refreshEndpoint}`;
    return this.rawHttp.post<LoginResponse>(url, {}, { headers }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryRefreshFallback(headers, index + 1, originalError);
        }

        return throwError(() => err);
      })
    );
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
