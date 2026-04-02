import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';

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
  canDeactivate: boolean;
  canUpdateRole: boolean;
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

export interface BusinessEmployeeInvite {
  workloadId: number;
  email: string;
  roleName: string;
  invitedByDisplayName: string;
  invitedByEmail?: string | null;
  invitedAt: string;
  status: string;
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
  availableEmployeeRoles: string[];
  serviceCount: number;
  services: BusinessWorkspaceService[];
  activeEmployeeCount: number;
  totalJobs: number;
  totalRevenue: number;
  employees: BusinessWorkspaceEmployee[];
  jobs: BusinessWorkspaceJob[];
}

export interface BusinessWorkspaceEmployeePage {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  employees: BusinessWorkspaceEmployee[];
}

@Injectable({
  providedIn: 'root',
})
export class BusinessService {
  private readonly businessEndpoint = '/api/business';
  private readonly servicesEndpoint = '/api/business/services';
  private readonly countriesEndpoint = '/api/business/countries';
  private readonly fallbackOrigins = [
    'http://localhost:5237',
    'https://localhost:7241',
    'http://localhost:5000',
  ];

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
    return this.http.post<CreateBusinessResponse>(this.businessEndpoint, payload).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryCreateBusinessFallback(payload, 0);
      })
    );
  }

  getMyBusinesses(): Observable<UserBusinessSummary[]> {
    return this.http.get<UserBusinessSummary[]>(this.businessEndpoint).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryGetMyBusinessesFallback(0);
      })
    );
  }

  getBusinessWorkspace(businessId: number): Observable<BusinessWorkspace> {
    return this.http.get<BusinessWorkspace>(`${this.businessEndpoint}/${businessId}`).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryGetBusinessWorkspaceFallback(businessId, 0);
      })
    );
  }

  getEmployeeInvites(businessId: number): Observable<BusinessEmployeeInvite[]> {
    return this.http.get<BusinessEmployeeInvite[]>(`${this.businessEndpoint}/${businessId}/employee-invites`).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryGetEmployeeInvitesFallback(businessId, 0);
      })
    );
  }

  getEmployees(businessId: number, pageNumber: number, pageSize: number): Observable<BusinessWorkspaceEmployeePage> {
    return this.http
      .get<BusinessWorkspaceEmployeePage>(`${this.businessEndpoint}/${businessId}/employees?pageNumber=${pageNumber}&pageSize=${pageSize}`)
      .pipe(
        catchError((err) => {
          if (!this.shouldTryNextOrigin(err)) {
            return throwError(() => err);
          }

          return this.tryGetEmployeesFallback(businessId, pageNumber, pageSize, 0);
        })
      );
  }

  createEmployeeInvite(businessId: number, email: string, roleName: string): Observable<BusinessEmployeeInvite> {
    return this.http.post<BusinessEmployeeInvite>(`${this.businessEndpoint}/${businessId}/employee-invites`, { email, roleName }).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryCreateEmployeeInviteFallback(businessId, email, roleName, 0);
      })
    );
  }

  deactivateEmployee(businessId: number, businessUserId: number): Observable<void> {
    return this.http.post<void>(`${this.businessEndpoint}/${businessId}/employees/${businessUserId}/deactivate`, {}).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryDeactivateEmployeeFallback(businessId, businessUserId, 0);
      })
    );
  }

  updateEmployeeRole(businessId: number, businessUserId: number, roleName: string): Observable<void> {
    return this.http.put<void>(`${this.businessEndpoint}/${businessId}/employees/${businessUserId}/role`, { roleName }).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryUpdateEmployeeRoleFallback(businessId, businessUserId, roleName, 0);
      })
    );
  }

  closeEmployeeInvite(businessId: number, workloadId: number): Observable<void> {
    return this.http.post<void>(`${this.businessEndpoint}/${businessId}/employee-invites/${workloadId}/close`, {}).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryCloseEmployeeInviteFallback(businessId, workloadId, 0);
      })
    );
  }

  updateBusinessServices(businessId: number, serviceIds: number[]): Observable<BusinessWorkspaceService[]> {
    return this.http.put<BusinessWorkspaceService[]>(`${this.businessEndpoint}/${businessId}/services`, { serviceIds }).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryUpdateBusinessServicesFallback(businessId, serviceIds, 0);
      })
    );
  }

  updateBusinessProfile(
    businessId: number,
    payload: UpdateBusinessProfileRequest
  ): Observable<UpdateBusinessProfileResponse> {
    return this.http.put<UpdateBusinessProfileResponse>(`${this.businessEndpoint}/${businessId}`, payload).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryUpdateBusinessProfileFallback(businessId, payload, 0);
      })
    );
  }

  deleteBusiness(businessId: number): Observable<void> {
    return this.http.delete<void>(`${this.businessEndpoint}/${businessId}`).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryDeleteBusinessFallback(businessId, 0);
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

  private tryCreateBusinessFallback(payload: CreateBusinessRequest, index: number): Observable<CreateBusinessResponse> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Create business endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}`;
    return this.http.post<CreateBusinessResponse>(url, payload).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryCreateBusinessFallback(payload, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryGetMyBusinessesFallback(index: number): Observable<UserBusinessSummary[]> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business list endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}`;
    return this.http.get<UserBusinessSummary[]>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetMyBusinessesFallback(index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryGetBusinessWorkspaceFallback(businessId: number, index: number): Observable<BusinessWorkspace> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business workspace endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessId}`;
    return this.http.get<BusinessWorkspace>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetBusinessWorkspaceFallback(businessId, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryGetEmployeeInvitesFallback(indexBusinessId: number, index: number): Observable<BusinessEmployeeInvite[]> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Employee invites endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${indexBusinessId}/employee-invites`;
    return this.http.get<BusinessEmployeeInvite[]>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetEmployeeInvitesFallback(indexBusinessId, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryGetEmployeesFallback(
    businessId: number,
    pageNumber: number,
    pageSize: number,
    index: number
  ): Observable<BusinessWorkspaceEmployeePage> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Employees endpoint not found on configured local origins.'));
    }

    const url =
      `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessId}/employees?pageNumber=${pageNumber}&pageSize=${pageSize}`;
    return this.http.get<BusinessWorkspaceEmployeePage>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetEmployeesFallback(businessId, pageNumber, pageSize, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryCreateEmployeeInviteFallback(
    businessId: number,
    email: string,
    roleName: string,
    index: number
  ): Observable<BusinessEmployeeInvite> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Employee invite creation endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessId}/employee-invites`;
    return this.http.post<BusinessEmployeeInvite>(url, { email, roleName }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryCreateEmployeeInviteFallback(businessId, email, roleName, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryCloseEmployeeInviteFallback(businessId: number, workloadId: number, index: number): Observable<void> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Employee invite close endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessId}/employee-invites/${workloadId}/close`;
    return this.http.post<void>(url, {}).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryCloseEmployeeInviteFallback(businessId, workloadId, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryDeactivateEmployeeFallback(businessId: number, businessUserId: number, index: number): Observable<void> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Employee deactivation endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessId}/employees/${businessUserId}/deactivate`;
    return this.http.post<void>(url, {}).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryDeactivateEmployeeFallback(businessId, businessUserId, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryUpdateEmployeeRoleFallback(
    businessId: number,
    businessUserId: number,
    roleName: string,
    index: number
  ): Observable<void> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Employee role update endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessId}/employees/${businessUserId}/role`;
    return this.http.put<void>(url, { roleName }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryUpdateEmployeeRoleFallback(businessId, businessUserId, roleName, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryUpdateBusinessServicesFallback(
    businessId: number,
    serviceIds: number[],
    index: number
  ): Observable<BusinessWorkspaceService[]> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business services update endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessId}/services`;
    return this.http.put<BusinessWorkspaceService[]>(url, { serviceIds }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryUpdateBusinessServicesFallback(businessId, serviceIds, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryUpdateBusinessProfileFallback(
    businessId: number,
    payload: UpdateBusinessProfileRequest,
    index: number
  ): Observable<UpdateBusinessProfileResponse> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business profile update endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessId}`;
    return this.http.put<UpdateBusinessProfileResponse>(url, payload).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryUpdateBusinessProfileFallback(businessId, payload, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryDeleteBusinessFallback(businessId: number, index: number): Observable<void> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business delete endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessId}`;
    return this.http.delete<void>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryDeleteBusinessFallback(businessId, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private shouldTryNextOrigin(err: { status?: number } | null | undefined): boolean {
    const status = err?.status;
    return status === 0 || status === 404;
  }
}
