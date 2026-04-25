import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

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

  getAddressSuggestions(query: string, countryCode?: string | null, limit = 5): Observable<AddressSuggestion[]> {
    let params = new HttpParams()
      .set('q', query)
      .set('limit', limit);

    if (countryCode && countryCode.trim()) {
      params = params.set('countryCode', countryCode.trim());
    }

    return this.http.get<AddressSuggestion[]>('/api/geocode/address-suggestions', { params });
  }

  resolveAddress(request: ResolveAddressRequest): Observable<AddressSuggestion | null> {
    return this.http.post<AddressSuggestion | null>('/api/geocode/resolve-address', request);
  }
}
