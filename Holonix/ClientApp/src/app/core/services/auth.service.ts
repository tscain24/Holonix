import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';

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
  phoneNumber?: string | null;
  dateOfBirth: string;
  email: string;
  password: string;
  profileImageBase64?: string | null;
  address1: string;
  address2?: string | null;
  city: string;
  state: string;
  zipCode: string;
  latitude?: number | null;
  longitude?: number | null;
}

export interface RegisterResponse {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  profileImageBase64?: string | null;
}

export interface UserProfileResponse {
  firstName: string;
  lastName: string;
  dateOfBirth?: string | null;
  email: string;
  phoneNumber?: string | null;
  profileImageBase64?: string | null;
  locations: UserSavedLocation[];
}

export interface UpdateUserProfileRequest {
  firstName: string;
  lastName: string;
  dateOfBirth?: string | null;
  phoneNumber?: string | null;
}

export interface UserSavedLocation {
  userSavedLocationId: number;
  label: string;
  address1: string;
  address2?: string | null;
  city: string;
  state: string;
  zipCode: string;
  latitude?: number | null;
  longitude?: number | null;
  isPrimary: boolean;
}

export interface CreateUserSavedLocationRequest {
  label: string;
  address1: string;
  address2?: string | null;
  city: string;
  state: string;
  zipCode: string;
  latitude?: number | null;
  longitude?: number | null;
  isPrimary: boolean;
}

export interface UpdateUserSavedLocationRequest extends CreateUserSavedLocationRequest {}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly profileEndpoint = '/api/auth/profile';
  private readonly profileImageEndpoint = '/api/auth/profile-image';
  private readonly profileLocationsEndpoint = '/api/auth/profile/locations';
  private readonly fallbackOrigins = [
    'http://localhost:5237',
    'https://localhost:7241',
    'https://localhost:44367',
    'http://localhost:33823',
  ];

  constructor(private readonly http: HttpClient) {}

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', payload);
  }

  register(payload: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>('/api/auth/register', payload);
  }

  updateProfileImage(profileImageBase64: string | null): Observable<{ profileImageBase64: string | null }> {
    return this.http.put<{ profileImageBase64: string | null }>(
      this.profileImageEndpoint,
      { profileImageBase64 }
    ).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryProfileImageFallback(profileImageBase64, 0);
      })
    );
  }

  getProfile(): Observable<UserProfileResponse> {
    return this.http.get<UserProfileResponse>(this.profileEndpoint).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryGetProfileFallback(0);
      })
    );
  }

  updateProfile(payload: UpdateUserProfileRequest): Observable<UserProfileResponse> {
    return this.http.put<UserProfileResponse>(this.profileEndpoint, payload).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryUpdateProfileFallback(payload, 0);
      })
    );
  }

  deleteProfile(): Observable<void> {
    return this.http.delete<void>(this.profileEndpoint).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryDeleteProfileFallback(0);
      })
    );
  }

  createSavedLocation(payload: CreateUserSavedLocationRequest): Observable<UserSavedLocation> {
    return this.http.post<UserSavedLocation>(this.profileLocationsEndpoint, payload).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryCreateSavedLocationFallback(payload, 0);
      })
    );
  }

  updateSavedLocation(userSavedLocationId: number, payload: UpdateUserSavedLocationRequest): Observable<UserSavedLocation> {
    return this.http.put<UserSavedLocation>(`${this.profileLocationsEndpoint}/${userSavedLocationId}`, payload).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryUpdateSavedLocationFallback(userSavedLocationId, payload, 0);
      })
    );
  }

  deleteSavedLocation(userSavedLocationId: number): Observable<void> {
    return this.http.delete<void>(`${this.profileLocationsEndpoint}/${userSavedLocationId}`).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryDeleteSavedLocationFallback(userSavedLocationId, 0);
      })
    );
  }

  private tryGetProfileFallback(index: number): Observable<UserProfileResponse> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Profile endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.profileEndpoint}`;
    return this.http.get<UserProfileResponse>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetProfileFallback(index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryUpdateProfileFallback(payload: UpdateUserProfileRequest, index: number): Observable<UserProfileResponse> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Profile update endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.profileEndpoint}`;
    return this.http.put<UserProfileResponse>(url, payload).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryUpdateProfileFallback(payload, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryProfileImageFallback(profileImageBase64: string | null, index: number): Observable<{ profileImageBase64: string | null }> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Profile image endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.profileImageEndpoint}`;
    return this.http.put<{ profileImageBase64: string | null }>(url, { profileImageBase64 }).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryProfileImageFallback(profileImageBase64, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryDeleteProfileFallback(index: number): Observable<void> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Profile delete endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.profileEndpoint}`;
    return this.http.delete<void>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryDeleteProfileFallback(index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryCreateSavedLocationFallback(payload: CreateUserSavedLocationRequest, index: number): Observable<UserSavedLocation> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Profile locations endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.profileLocationsEndpoint}`;
    return this.http.post<UserSavedLocation>(url, payload).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryCreateSavedLocationFallback(payload, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryUpdateSavedLocationFallback(
    userSavedLocationId: number,
    payload: UpdateUserSavedLocationRequest,
    index: number
  ): Observable<UserSavedLocation> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Profile locations endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.profileLocationsEndpoint}/${userSavedLocationId}`;
    return this.http.put<UserSavedLocation>(url, payload).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryUpdateSavedLocationFallback(userSavedLocationId, payload, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryDeleteSavedLocationFallback(userSavedLocationId: number, index: number): Observable<void> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Profile locations endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.profileLocationsEndpoint}/${userSavedLocationId}`;
    return this.http.delete<void>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryDeleteSavedLocationFallback(userSavedLocationId, index + 1);
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
