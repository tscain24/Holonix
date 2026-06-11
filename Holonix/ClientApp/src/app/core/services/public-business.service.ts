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

export interface PublicBusinessAvailabilityTimeFrame {
  startTime: string;
  endTime: string;
}

export interface PublicBusinessAvailabilityDay {
  dayOfWeek: number;
  timeFrames: PublicBusinessAvailabilityTimeFrame[];
  isClosed: boolean;
}

export interface PublicBusinessDailyAvailability {
  date: string;
  isAvailable: boolean;
  timeFrames: PublicBusinessAvailabilityTimeFrame[];
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
  businessHours?: { dayOfWeek: number; openTime?: string | null; closeTime?: string | null; isClosed: boolean }[] | null;
  availability?: PublicBusinessAvailabilityDay[] | null;
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

  getDailyAvailability(
    businessCode: string,
    dateIso: string,
    businessSubServiceIds?: number[]
  ): Observable<PublicBusinessDailyAvailability> {
    const encodedBusinessCode = encodeURIComponent((businessCode ?? '').trim());
    const encodedDate = encodeURIComponent((dateIso ?? '').trim());
    const normalizedIds = (businessSubServiceIds ?? [])
      .map((id) => Number(id))
      .filter((id) => Number.isFinite(id) && id > 0);
    const serviceQuery = normalizedIds.map((id) => `businessSubServiceIds=${encodeURIComponent(`${id}`)}`).join('&');
    const query = serviceQuery ? `date=${encodedDate}&${serviceQuery}` : `date=${encodedDate}`;
    return this.http.get<PublicBusinessDailyAvailability>(`${this.endpoint}/${encodedBusinessCode}/availability/daily?${query}`);
  }
}
