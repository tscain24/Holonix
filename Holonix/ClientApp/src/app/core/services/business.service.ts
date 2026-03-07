import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';

export interface BusinessServiceOption {
  serviceId: number;
  name: string;
}

@Injectable({
  providedIn: 'root',
})
export class BusinessService {
  private readonly servicesEndpoint = '/api/business/services';
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

  private shouldTryNextOrigin(err: { status?: number } | null | undefined): boolean {
    const status = err?.status;
    return status === 0 || status === 404;
  }
}
