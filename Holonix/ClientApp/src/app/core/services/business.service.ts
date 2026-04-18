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
  businessEmail?: string | null;
  businessPhoneNumber?: string | null;
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
  businessCode: string;
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
  businessEmail?: string | null;
  businessPhoneNumber?: string | null;
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
  businessEmail?: string | null;
  businessPhoneNumber?: string | null;
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
  businessCode: string;
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
  email?: string | null;
  phoneNumber?: string | null;
  dateOfBirth?: string | null;
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
  subServices: BusinessWorkspaceSubService[];
}

export interface BusinessWorkspaceSubService {
  businessSubServiceId: number;
  name: string;
  description?: string | null;
  consultationNeeded: boolean;
  durationMinutes: number;
  price: number;
  employeeCount: number;
  effectiveDate: string;
  assignedUsers: BusinessWorkspaceSubServiceAssignment[];
}

export interface BusinessWorkspaceSubServiceAssignment {
  businessUserId: number;
  displayName: string;
}

export interface CreateBusinessSubServiceRequest {
  name: string;
  description?: string | null;
  consultationNeeded: boolean;
  durationMinutes: number;
  price: number;
  employeeCount: number;
  assignedBusinessUserIds?: number[];
  effectiveDate: string;
}

export interface UpdateBusinessSubServiceRequest extends CreateBusinessSubServiceRequest {
  serviceId: number;
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

export interface MyBusinessAvailabilityDay {
  dayOfWeek: number;
  startTime?: string | null;
  endTime?: string | null;
}

export interface MyBusinessAvailability {
  businessUserId: number;
  effectiveStartDate: string;
  availability: MyBusinessAvailabilityDay[];
}

export interface UpdateMyBusinessAvailabilityDayRequest {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

export interface UpdateMyBusinessAvailabilityRequest {
  availability: UpdateMyBusinessAvailabilityDayRequest[];
}

export interface BusinessWorkspace {
  businessId: number;
  businessCode: string;
  name: string;
  description?: string | null;
  businessEmail?: string | null;
  businessPhoneNumber?: string | null;
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
  filterEmployeeRoles: string[];
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

export interface EmployeeFilter {
  field: 'employee' | 'role' | 'status';
  operator: 'contains' | 'is';
  value: string;
}

export interface EmployeeSort {
  field: 'employee' | 'role' | 'hiredDate' | 'status' | 'assignedJobCount';
  direction: 'asc' | 'desc';
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

  getBusinessWorkspace(businessCode: string): Observable<BusinessWorkspace> {
    return this.http.get<BusinessWorkspace>(`${this.businessEndpoint}/${businessCode}`).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryGetBusinessWorkspaceFallback(businessCode, 0);
      })
    );
  }

  getEmployeeInvites(businessCode: string): Observable<BusinessEmployeeInvite[]> {
    return this.http.get<BusinessEmployeeInvite[]>(`${this.businessEndpoint}/${businessCode}/employee-invites`).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryGetEmployeeInvitesFallback(businessCode, 0);
      })
    );
  }

  getMyBusinessAvailability(businessCode: string): Observable<MyBusinessAvailability> {
    return this.http.get<MyBusinessAvailability>(`${this.businessEndpoint}/${businessCode}/my-availability`).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryGetMyBusinessAvailabilityFallback(businessCode, 0);
      })
    );
  }

  updateMyBusinessAvailability(
    businessCode: string,
    payload: UpdateMyBusinessAvailabilityRequest
  ): Observable<MyBusinessAvailability> {
    return this.http.put<MyBusinessAvailability>(`${this.businessEndpoint}/${businessCode}/my-availability`, payload).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryUpdateMyBusinessAvailabilityFallback(businessCode, payload, 0);
      })
    );
  }

  getEmployees(
    businessCode: string,
    pageNumber: number,
    pageSize: number,
    filters: EmployeeFilter[] = [],
    includeInactive = false,
    sort?: EmployeeSort | null
  ): Observable<BusinessWorkspaceEmployeePage> {
    const query = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
      includeInactive: includeInactive.toString(),
    });

    if (filters.length > 0) {
      query.set('filters', filters.map((filter) => `${filter.field}~${filter.operator}~${encodeURIComponent(filter.value)}`).join(';'));
    }

    if (sort) {
      query.set('sortBy', sort.field);
      query.set('sortDirection', sort.direction);
    }

    return this.http
      .get<BusinessWorkspaceEmployeePage>(`${this.businessEndpoint}/${businessCode}/employees?${query.toString()}`)
      .pipe(
        catchError((err) => {
          if (!this.shouldTryNextOrigin(err)) {
            return throwError(() => err);
          }

          return this.tryGetEmployeesFallback(businessCode, pageNumber, pageSize, filters, includeInactive, sort, 0);
        })
      );
  }

  createEmployeeInvite(businessCode: string, email: string, roleName: string): Observable<BusinessEmployeeInvite> {
    return this.http.post<BusinessEmployeeInvite>(`${this.businessEndpoint}/${businessCode}/employee-invites`, { email, roleName }).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryCreateEmployeeInviteFallback(businessCode, email, roleName, 0);
      })
    );
  }

  deactivateEmployee(businessCode: string, businessUserId: number): Observable<void> {
    return this.http.post<void>(`${this.businessEndpoint}/${businessCode}/employees/${businessUserId}/deactivate`, {}).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryDeactivateEmployeeFallback(businessCode, businessUserId, 0);
      })
    );
  }

  updateEmployeeRole(businessCode: string, businessUserId: number, roleName: string): Observable<void> {
    return this.http.put<void>(`${this.businessEndpoint}/${businessCode}/employees/${businessUserId}/role`, { roleName }).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryUpdateEmployeeRoleFallback(businessCode, businessUserId, roleName, 0);
      })
    );
  }

  leaveBusiness(businessCode: string): Observable<void> {
    return this.http.post<void>(`${this.businessEndpoint}/${businessCode}/leave`, {}).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryLeaveBusinessFallback(businessCode, 0);
      })
    );
  }

  closeEmployeeInvite(businessCode: string, workloadId: number): Observable<void> {
    return this.http.post<void>(`${this.businessEndpoint}/${businessCode}/employee-invites/${workloadId}/close`, {}).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryCloseEmployeeInviteFallback(businessCode, workloadId, 0);
      })
    );
  }

  updateBusinessServices(businessCode: string, serviceIds: number[]): Observable<BusinessWorkspaceService[]> {
    return this.http.put<BusinessWorkspaceService[]>(`${this.businessEndpoint}/${businessCode}/services`, { serviceIds }).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryUpdateBusinessServicesFallback(businessCode, serviceIds, 0);
      })
    );
  }

  createBusinessSubService(
    businessCode: string,
    serviceId: number,
    payload: CreateBusinessSubServiceRequest
  ): Observable<BusinessWorkspaceSubService> {
    return this.http.post<BusinessWorkspaceSubService>(`${this.businessEndpoint}/${businessCode}/services/${serviceId}/sub-services`, payload).pipe(
      catchError((error) => {
        if (error?.status === 0) {
          return this.tryCreateBusinessSubServiceFallback(businessCode, serviceId, payload, 0);
        }

        return throwError(() => error);
      })
    );
  }

  deleteBusinessSubService(businessCode: string, businessSubServiceId: number): Observable<void> {
    return this.http.delete<void>(`${this.businessEndpoint}/${businessCode}/sub-services/${businessSubServiceId}`).pipe(
      catchError((error) => {
        if (error?.status === 0) {
          return this.tryDeleteBusinessSubServiceFallback(businessCode, businessSubServiceId, 0);
        }

        return throwError(() => error);
      })
    );
  }

  updateBusinessSubService(
    businessCode: string,
    businessSubServiceId: number,
    payload: UpdateBusinessSubServiceRequest
  ): Observable<BusinessWorkspaceSubService> {
    return this.http.put<BusinessWorkspaceSubService>(`${this.businessEndpoint}/${businessCode}/sub-services/${businessSubServiceId}`, payload).pipe(
      catchError((error) => {
        if (error?.status === 0) {
          return this.tryUpdateBusinessSubServiceFallback(businessCode, businessSubServiceId, payload, 0);
        }

        return throwError(() => error);
      })
    );
  }

  updateBusinessProfile(
    businessCode: string,
    payload: UpdateBusinessProfileRequest
  ): Observable<UpdateBusinessProfileResponse> {
    return this.http.put<UpdateBusinessProfileResponse>(`${this.businessEndpoint}/${businessCode}`, payload).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryUpdateBusinessProfileFallback(businessCode, payload, 0);
      })
    );
  }

  deleteBusiness(businessCode: string): Observable<void> {
    return this.http.delete<void>(`${this.businessEndpoint}/${businessCode}`).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryDeleteBusinessFallback(businessCode, 0);
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

  private tryGetBusinessWorkspaceFallback(businessCode: string, index: number): Observable<BusinessWorkspace> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business workspace endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}`;
    return this.http.get<BusinessWorkspace>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetBusinessWorkspaceFallback(businessCode, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryGetEmployeeInvitesFallback(businessCode: string, index: number): Observable<BusinessEmployeeInvite[]> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Employee invites endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}/employee-invites`;
    return this.http.get<BusinessEmployeeInvite[]>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetEmployeeInvitesFallback(businessCode, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryGetMyBusinessAvailabilityFallback(businessCode: string, index: number): Observable<MyBusinessAvailability> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business availability endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}/my-availability`;
    return this.http.get<MyBusinessAvailability>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetMyBusinessAvailabilityFallback(businessCode, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryGetEmployeesFallback(
    businessCode: string,
    pageNumber: number,
    pageSize: number,
    filters: EmployeeFilter[],
    includeInactive: boolean,
    sort: EmployeeSort | null | undefined,
    index: number
  ): Observable<BusinessWorkspaceEmployeePage> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Employees endpoint not found on configured local origins.'));
    }

    const query = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
      includeInactive: includeInactive.toString(),
    });

    if (filters.length > 0) {
      query.set('filters', filters.map((filter) => `${filter.field}~${filter.operator}~${encodeURIComponent(filter.value)}`).join(';'));
    }

    if (sort) {
      query.set('sortBy', sort.field);
      query.set('sortDirection', sort.direction);
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}/employees?${query.toString()}`;
    return this.http.get<BusinessWorkspaceEmployeePage>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetEmployeesFallback(businessCode, pageNumber, pageSize, filters, includeInactive, sort, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryCreateEmployeeInviteFallback(
    businessCode: string,
    email: string,
    roleName: string,
    index: number
  ): Observable<BusinessEmployeeInvite> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Employee invite creation endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}/employee-invites`;
    return this.http.post<BusinessEmployeeInvite>(url, { email, roleName }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryCreateEmployeeInviteFallback(businessCode, email, roleName, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryCloseEmployeeInviteFallback(businessCode: string, workloadId: number, index: number): Observable<void> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Employee invite close endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}/employee-invites/${workloadId}/close`;
    return this.http.post<void>(url, {}).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryCloseEmployeeInviteFallback(businessCode, workloadId, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryDeactivateEmployeeFallback(businessCode: string, businessUserId: number, index: number): Observable<void> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Employee deactivation endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}/employees/${businessUserId}/deactivate`;
    return this.http.post<void>(url, {}).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryDeactivateEmployeeFallback(businessCode, businessUserId, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryUpdateEmployeeRoleFallback(
    businessCode: string,
    businessUserId: number,
    roleName: string,
    index: number
  ): Observable<void> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Employee role update endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}/employees/${businessUserId}/role`;
    return this.http.put<void>(url, { roleName }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryUpdateEmployeeRoleFallback(businessCode, businessUserId, roleName, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryLeaveBusinessFallback(businessCode: string, index: number): Observable<void> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Leave business endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}/leave`;
    return this.http.post<void>(url, {}).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryLeaveBusinessFallback(businessCode, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryUpdateBusinessServicesFallback(
    businessCode: string,
    serviceIds: number[],
    index: number
  ): Observable<BusinessWorkspaceService[]> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business services update endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}/services`;
    return this.http.put<BusinessWorkspaceService[]>(url, { serviceIds }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryUpdateBusinessServicesFallback(businessCode, serviceIds, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryUpdateBusinessProfileFallback(
    businessCode: string,
    payload: UpdateBusinessProfileRequest,
    index: number
  ): Observable<UpdateBusinessProfileResponse> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business profile update endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}`;
    return this.http.put<UpdateBusinessProfileResponse>(url, payload).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryUpdateBusinessProfileFallback(businessCode, payload, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryUpdateMyBusinessAvailabilityFallback(
    businessCode: string,
    payload: UpdateMyBusinessAvailabilityRequest,
    index: number
  ): Observable<MyBusinessAvailability> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business availability update endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}/my-availability`;
    return this.http.put<MyBusinessAvailability>(url, payload).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryUpdateMyBusinessAvailabilityFallback(businessCode, payload, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryDeleteBusinessFallback(businessCode: string, index: number): Observable<void> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Business delete endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}`;
    return this.http.delete<void>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryDeleteBusinessFallback(businessCode, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private shouldTryNextOrigin(err: { status?: number } | null | undefined): boolean {
    const status = err?.status;
    return status === 0 || status === 404;
  }

  private tryCreateBusinessSubServiceFallback(
    businessCode: string,
    serviceId: number,
    payload: CreateBusinessSubServiceRequest,
    index: number
  ): Observable<BusinessWorkspaceSubService> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Unable to reach the business service endpoint.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}/services/${serviceId}/sub-services`;
    return this.http.post<BusinessWorkspaceSubService>(url, payload).pipe(
      catchError((error) => {
        if (error?.status === 0) {
          return this.tryCreateBusinessSubServiceFallback(businessCode, serviceId, payload, index + 1);
        }

        return throwError(() => error);
      })
    );
  }

  private tryDeleteBusinessSubServiceFallback(
    businessCode: string,
    businessSubServiceId: number,
    index: number
  ): Observable<void> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Unable to reach the business service endpoint.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}/sub-services/${businessSubServiceId}`;
    return this.http.delete<void>(url).pipe(
      catchError((error) => {
        if (error?.status === 0) {
          return this.tryDeleteBusinessSubServiceFallback(businessCode, businessSubServiceId, index + 1);
        }

        return throwError(() => error);
      })
    );
  }

  private tryUpdateBusinessSubServiceFallback(
    businessCode: string,
    businessSubServiceId: number,
    payload: UpdateBusinessSubServiceRequest,
    index: number
  ): Observable<BusinessWorkspaceSubService> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Unable to reach the business service endpoint.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.businessEndpoint}/${businessCode}/sub-services/${businessSubServiceId}`;
    return this.http.put<BusinessWorkspaceSubService>(url, payload).pipe(
      catchError((error) => {
        if (error?.status === 0) {
          return this.tryUpdateBusinessSubServiceFallback(businessCode, businessSubServiceId, payload, index + 1);
        }

        return throwError(() => error);
      })
    );
  }
}
