import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PublicBusinessSubService {
  businessSubServiceId: number;
  name: string;
  description?: string | null;
  consultationNeeded: boolean;
  durationMinutes: number;
  price: number;
}

export interface PublicBusinessService {
  serviceId: number;
  name: string;
  subServices: PublicBusinessSubService[];
}

export interface PublicBusinessProfile {
  businessId: number;
  businessCode: string;
  name: string;
  description?: string | null;
  categoryName?: string | null;
  city?: string | null;
  state?: string | null;
  countryName?: string | null;
  businessEmail?: string | null;
  businessPhoneNumber?: string | null;
  address1?: string | null;
  address2?: string | null;
  zipCode?: string | null;
  businessIconBase64?: string | null;
  services: PublicBusinessService[];
}

@Injectable({
  providedIn: 'root',
})
export class PublicBusinessService {
  private readonly endpoint = '/api/business/public';

  constructor(private http: HttpClient) {}

  getProfile(businessCode: string): Observable<PublicBusinessProfile> {
    const encoded = encodeURIComponent((businessCode ?? '').trim());
    return this.http.get<PublicBusinessProfile>(`${this.endpoint}/${encoded}`);
  }
}

