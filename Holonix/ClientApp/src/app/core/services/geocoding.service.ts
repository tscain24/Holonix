import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';

export interface AddressSuggestion {
  mapboxId?: string | null;
  fullAddress?: string | null;
  address1: string;
  city?: string | null;
  state?: string | null;
  zipCode?: string | null;
  countryCode?: string | null;
  latitude: number;
  longitude: number;
}

export interface ResolveAddressRequest {
  address1: string;
  address2?: string | null;
  city: string;
  state: string;
  zipCode: string;
  countryCode?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class GeocodingService {
  constructor(private readonly http: HttpClient) {}

  private normalizeSuggestion(raw: unknown): AddressSuggestion | null {
    if (!raw || typeof raw !== 'object') {
      return null;
    }

    const record = raw as Record<string, unknown>;
    const readString = (...keys: string[]): string | null => {
      for (const key of keys) {
        const value = record[key];
        if (typeof value === 'string') {
          const trimmed = value.trim();
          if (trimmed) {
            return trimmed;
          }
          return null;
        }
      }

      return null;
    };

    const readNumber = (...keys: string[]): number | null => {
      for (const key of keys) {
        const value = record[key];
        if (typeof value === 'number' && Number.isFinite(value)) {
          return value;
        }
        if (typeof value === 'string') {
          const parsed = Number.parseFloat(value);
          if (Number.isFinite(parsed)) {
            return parsed;
          }
        }
      }

      return null;
    };

    const address1 = readString('address1', 'Address1');
    const latitude = readNumber('latitude', 'Latitude');
    const longitude = readNumber('longitude', 'Longitude');

    if (!address1 || latitude === null || longitude === null) {
      return null;
    }

    return {
      mapboxId: readString('mapboxId', 'MapboxId'),
      fullAddress: readString('fullAddress', 'FullAddress'),
      address1,
      city: readString('city', 'City'),
      state: readString('state', 'State'),
      zipCode: readString('zipCode', 'ZipCode'),
      countryCode: readString('countryCode', 'CountryCode'),
      latitude,
      longitude,
    };
  }

  getAddressSuggestions(query: string, countryCode?: string | null, limit = 5): Observable<AddressSuggestion[]> {
    let params = new HttpParams()
      .set('q', query)
      .set('limit', limit);

    if (countryCode && countryCode.trim()) {
      params = params.set('countryCode', countryCode.trim());
    }

    return this.http.get<unknown[]>('/api/geocode/address-suggestions', { params }).pipe(
      map((results) => (Array.isArray(results) ? results : [])),
      map((results) => results.map((entry) => this.normalizeSuggestion(entry)).filter((entry): entry is AddressSuggestion => !!entry))
    );
  }

  resolveAddress(request: ResolveAddressRequest): Observable<AddressSuggestion | null> {
    return this.http.post<unknown>('/api/geocode/resolve-address', request).pipe(
      map((result) => this.normalizeSuggestion(result))
    );
  }
}
