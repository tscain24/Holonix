import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of, shareReplay, throwError } from 'rxjs';

interface MapboxTokenResponse {
  accessToken?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class MapboxConfigService {
  private readonly endpoint = '/api/mapbox/public-token';
  private readonly fallbackOrigins = resolveFallbackOrigins();

  private readonly token$ = this.http.get<MapboxTokenResponse>(this.endpoint).pipe(
    catchError((err) => {
      if (this.shouldTryNextOrigin(err)) {
        return this.tryFetchTokenFallback(0);
      }
      return throwError(() => err);
    }),
    map((response) => (response?.accessToken ?? '').trim()),
    shareReplay({ bufferSize: 1, refCount: true })
  );

  constructor(private readonly http: HttpClient) {}

  getAccessToken(): Observable<string> {
    return this.token$;
  }

  private tryFetchTokenFallback(index: number): Observable<MapboxTokenResponse> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Mapbox token endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.endpoint}`;
    return this.http.get<MapboxTokenResponse>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryFetchTokenFallback(index + 1);
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

function resolveFallbackOrigins(): string[] {
  if (typeof window === 'undefined') {
    return [];
  }

  const host = window.location.hostname;
  const isLocal = host === 'localhost' || host === '127.0.0.1';
  if (!isLocal) {
    return [];
  }

  return [
    'http://localhost:5237',
    'https://localhost:7241',
    'http://localhost:5000',
  ];
}
