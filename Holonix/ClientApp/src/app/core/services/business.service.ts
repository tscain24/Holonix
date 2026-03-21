import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, catchError, finalize, map, shareReplay, switchMap, tap, throwError } from 'rxjs';

export interface BusinessServiceOption {
  serviceId: number;
  name: string;
}

export interface CountryOption {
  countryId: number;
  name: string;
  twoLetterIsoCode: string;
}

export interface CreateBusinessRequest {
  name: string;
  description?: string | null;
  address1: string;
  address2?: string | null;
  city: string;
  state: string;
  zipCode: string;
  countryId: number;
  businessIconBase64?: string | null;
  businessJobPercentage: number;
  isProductBased: boolean;
  serviceIds: number[];
}

export interface CreateBusinessResponse {
  businessId: number;
  name: string;
  userId: string;
  displayName: string;
  token: string;
  profileImageBase64?: string | null;
}

export interface UserBusinessSummary {
  businessId: number;
  name: string;
  description?: string | null;
  city?: string | null;
  state?: string | null;
  countryName?: string | null;
  businessIconBase64?: string | null;
  businessJobPercentage: number;
  isProductBased: boolean;
  serviceCount: number;
  roleName?: string | null;
  totalJobs: number;
}

@Injectable({
  providedIn: 'root',
})
export class BusinessService {
  private readonly businessEndpoint = '/api/business';
  private readonly servicesEndpoint = '/api/business/services';
  private readonly countriesEndpoint = '/api/business/countries';
  private readonly refreshEndpoint = '/api/auth/refresh';
  private readonly refreshLeadSeconds = 120;
  private readonly fallbackOrigins = [
    'http://localhost:5237',
    'https://localhost:7241',
    'http://localhost:5000',
  ];
  private refreshInFlight$: Observable<string> | null = null;

  constructor(private readonly http: HttpClient) {}

  getServices(): Observable<BusinessServiceOption[]> {
    return this.http.get<BusinessServiceOption[]>(this.servicesEndpoint).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryGetServicesFallback(0);
      })
    );
  }

  getCountries(): Observable<CountryOption[]> {
    return this.http.get<CountryOption[]>(this.countriesEndpoint).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryGetCountriesFallback(0);
      })
    );
  }

  createBusiness(payload: CreateBusinessRequest): Observable<CreateBusinessResponse> {
    return this.withFreshToken((headers) =>
      this.sendCreateBusinessRequest(payload, headers, true)
    );
  }

  getMyBusinesses(): Observable<UserBusinessSummary[]> {
    return this.withFreshToken((headers) =>
      this.http.get<UserBusinessSummary[]>(this.businessEndpoint, { headers }).pipe(
        catchError((err) => {
          if (!this.shouldTryNextOrigin(err)) {
            return throwError(() => err);
          }

          return this.tryGetMyBusinessesFallback(headers, 0);
        })
      )
    );
  }

  private sendCreateBusinessRequest(
    payload: CreateBusinessRequest,
    headers: HttpHeaders,
    allowRefreshRetry: boolean
  ): Observable<CreateBusinessResponse> {
    return this.http.post<CreateBusinessResponse>(this.businessEndpoint, payload, { headers }).pipe(
      catchError((err) => {
        if (err?.status === 401 && allowRefreshRetry) {
          return this.refreshToken().pipe(
            switchMap((refreshedToken) =>
              this.sendCreateBusinessRequest(payload, this.buildAuthHeaders(refreshedToken), false)
            )
          );
        }

        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryCreateBusinessFallback(payload, headers, 0);
      })
    );
  }

  private tryGetServicesFallback(index: number): Observable<BusinessServiceOption[]> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business services endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.servicesEndpoint}`;
    return this.http.get<BusinessServiceOption[]>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetServicesFallback(index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryGetCountriesFallback(index: number): Observable<CountryOption[]> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Countries endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.countriesEndpoint}`;
    return this.http.get<CountryOption[]>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetCountriesFallback(index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryCreateBusinessFallback(
    payload: CreateBusinessRequest,
    headers: HttpHeaders,
    index: number
  ): Observable<CreateBusinessResponse> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Create business endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}`;
    return this.http.post<CreateBusinessResponse>(url, payload, { headers }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryCreateBusinessFallback(payload, headers, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryGetMyBusinessesFallback(headers: HttpHeaders, index: number): Observable<UserBusinessSummary[]> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business list endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}`;
    return this.http.get<UserBusinessSummary[]>(url, { headers }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetMyBusinessesFallback(headers, index + 1);
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
    const refreshRequest$ = this.http.post<CreateBusinessResponse>(this.refreshEndpoint, {}, { headers }).pipe(
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

  private tryRefreshFallback(headers: HttpHeaders, index: number): Observable<CreateBusinessResponse> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Refresh endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.refreshEndpoint}`;
    return this.http.post<CreateBusinessResponse>(url, {}, { headers }).pipe(
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
