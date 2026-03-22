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

export interface UpdateBusinessProfileRequest {
  name: string;
  description?: string | null;
  businessIconBase64?: string | null;
  address1: string;
  address2?: string | null;
  city: string;
  state: string;
  zipCode: string;
  businessJobPercentage: number;
}

export interface UpdateBusinessProfileResponse {
  businessId: number;
  name: string;
  description?: string | null;
  businessIconBase64?: string | null;
  address1?: string | null;
  address2?: string | null;
  city?: string | null;
  state?: string | null;
  zipCode?: string | null;
  countryName?: string | null;
  businessJobPercentage: number;
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

export interface BusinessWorkspaceEmployee {
  businessUserId: number;
  displayName: string;
  roleName?: string | null;
  hiredDate: string;
  isActive: boolean;
  assignedJobCount: number;
}

export interface BusinessWorkspaceService {
  serviceId: number;
  name: string;
}

export interface BusinessWorkspaceJob {
  jobId: number;
  name: string;
  startDateTime: string;
  endDateTime: string;
  cost: number;
  netCost: number;
  assignedEmployeeName?: string | null;
}

export interface BusinessWorkspace {
  businessId: number;
  name: string;
  description?: string | null;
  address1?: string | null;
  address2?: string | null;
  city?: string | null;
  state?: string | null;
  zipCode?: string | null;
  countryName?: string | null;
  businessIconBase64?: string | null;
  businessJobPercentage: number;
  isProductBased: boolean;
  currentUserRoleName?: string | null;
  serviceCount: number;
  services: BusinessWorkspaceService[];
  activeEmployeeCount: number;
  totalJobs: number;
  totalRevenue: number;
  employees: BusinessWorkspaceEmployee[];
  jobs: BusinessWorkspaceJob[];
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
    const token = this.getStoredToken();
    if (!token) {
      return throwError(() => new Error('Missing auth token.'));
    }

    return this.sendCreateBusinessRequest(payload, this.buildAuthHeaders(token), true);
  }

  getMyBusinesses(): Observable<UserBusinessSummary[]> {
    const token = this.getStoredToken();
    if (!token) {
      return throwError(() => new Error('Missing auth token.'));
    }

    return this.sendGetMyBusinessesRequest(this.buildAuthHeaders(token), true);
  }

  getBusinessWorkspace(businessId: number): Observable<BusinessWorkspace> {
    const token = this.getStoredToken();
    if (!token) {
      return throwError(() => new Error('Missing auth token.'));
    }

    return this.sendGetBusinessWorkspaceRequest(businessId, this.buildAuthHeaders(token), true);
  }

  updateBusinessServices(businessId: number, serviceIds: number[]): Observable<BusinessWorkspaceService[]> {
    const token = this.getStoredToken();
    if (!token) {
      return throwError(() => new Error('Missing auth token.'));
    }

    return this.sendUpdateBusinessServicesRequest(businessId, serviceIds, this.buildAuthHeaders(token), true);
  }

  updateBusinessProfile(
    businessId: number,
    payload: UpdateBusinessProfileRequest
  ): Observable<UpdateBusinessProfileResponse> {
    const token = this.getStoredToken();
    if (!token) {
      return throwError(() => new Error('Missing auth token.'));
    }

    return this.sendUpdateBusinessProfileRequest(businessId, payload, this.buildAuthHeaders(token), true);
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

  private tryGetBusinessWorkspaceFallback(
    businessId: number,
    headers: HttpHeaders,
    index: number
  ): Observable<BusinessWorkspace> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business workspace endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessId}`;
    return this.http.get<BusinessWorkspace>(url, { headers }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetBusinessWorkspaceFallback(businessId, headers, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryUpdateBusinessServicesFallback(
    businessId: number,
    serviceIds: number[],
    headers: HttpHeaders,
    index: number
  ): Observable<BusinessWorkspaceService[]> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business services update endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessId}/services`;
    return this.http.put<BusinessWorkspaceService[]>(url, { serviceIds }, { headers }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryUpdateBusinessServicesFallback(businessId, serviceIds, headers, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryUpdateBusinessProfileFallback(
    businessId: number,
    payload: UpdateBusinessProfileRequest,
    headers: HttpHeaders,
    index: number
  ): Observable<UpdateBusinessProfileResponse> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business profile update endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessId}`;
    return this.http.put<UpdateBusinessProfileResponse>(url, payload, { headers }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryUpdateBusinessProfileFallback(businessId, payload, headers, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private sendGetMyBusinessesRequest(
    headers: HttpHeaders,
    allowRefreshRetry: boolean
  ): Observable<UserBusinessSummary[]> {
    return this.http.get<UserBusinessSummary[]>(this.businessEndpoint, { headers }).pipe(
      catchError((err) => {
        if (err?.status === 401 && allowRefreshRetry) {
          return this.refreshToken().pipe(
            switchMap((refreshedToken) =>
              this.sendGetMyBusinessesRequest(this.buildAuthHeaders(refreshedToken), false)
            )
          );
        }

        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryGetMyBusinessesFallback(headers, 0);
      })
    );
  }

  private sendGetBusinessWorkspaceRequest(
    businessId: number,
    headers: HttpHeaders,
    allowRefreshRetry: boolean
  ): Observable<BusinessWorkspace> {
    return this.http.get<BusinessWorkspace>(`${this.businessEndpoint}/${businessId}`, { headers }).pipe(
      catchError((err) => {
        if (err?.status === 401 && allowRefreshRetry) {
          return this.refreshToken().pipe(
            switchMap((refreshedToken) =>
              this.sendGetBusinessWorkspaceRequest(businessId, this.buildAuthHeaders(refreshedToken), false)
            )
          );
        }

        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryGetBusinessWorkspaceFallback(businessId, headers, 0);
      })
    );
  }

  private sendUpdateBusinessServicesRequest(
    businessId: number,
    serviceIds: number[],
    headers: HttpHeaders,
    allowRefreshRetry: boolean
  ): Observable<BusinessWorkspaceService[]> {
    return this.http.put<BusinessWorkspaceService[]>(`${this.businessEndpoint}/${businessId}/services`, { serviceIds }, { headers }).pipe(
      catchError((err) => {
        if (err?.status === 401 && allowRefreshRetry) {
          return this.refreshToken().pipe(
            switchMap((refreshedToken) =>
              this.sendUpdateBusinessServicesRequest(businessId, serviceIds, this.buildAuthHeaders(refreshedToken), false)
            )
          );
        }

        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryUpdateBusinessServicesFallback(businessId, serviceIds, headers, 0);
      })
    );
  }

  private sendUpdateBusinessProfileRequest(
    businessId: number,
    payload: UpdateBusinessProfileRequest,
    headers: HttpHeaders,
    allowRefreshRetry: boolean
  ): Observable<UpdateBusinessProfileResponse> {
    return this.http.put<UpdateBusinessProfileResponse>(`${this.businessEndpoint}/${businessId}`, payload, { headers }).pipe(
      catchError((err) => {
        if (err?.status === 401 && allowRefreshRetry) {
          return this.refreshToken().pipe(
            switchMap((refreshedToken) =>
              this.sendUpdateBusinessProfileRequest(businessId, payload, this.buildAuthHeaders(refreshedToken), false)
            )
          );
        }

        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryUpdateBusinessProfileFallback(businessId, payload, headers, 0);
      })
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

  private shouldTryNextOrigin(err: { status?: number } | null | undefined): boolean {
    const status = err?.status;
    return status === 0 || status === 404;
  }
}
