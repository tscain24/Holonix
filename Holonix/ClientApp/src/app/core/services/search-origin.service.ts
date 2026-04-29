import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export type SearchOriginSource = 'device' | 'typed';

export interface SearchOrigin {
  latitude: number;
  longitude: number;
  label: string;
  city?: string | null;
  state?: string | null;
  source: SearchOriginSource;
  updatedAt: string;
}

@Injectable({
  providedIn: 'root',
})
export class SearchOriginService {
  private static readonly StorageKey = 'holonix_search_origin_v1';
  private readonly originSubject = new BehaviorSubject<SearchOrigin | null>(this.loadFromStorage());
  readonly origin$: Observable<SearchOrigin | null> = this.originSubject.asObservable();

  get currentOrigin(): SearchOrigin | null {
    return this.originSubject.value;
  }

  setOrigin(origin: Omit<SearchOrigin, 'updatedAt'>): void {
    const next: SearchOrigin = { ...origin, updatedAt: new Date().toISOString() };
    this.originSubject.next(next);
    localStorage.setItem(SearchOriginService.StorageKey, JSON.stringify(next));
  }

  clearOrigin(): void {
    this.originSubject.next(null);
    localStorage.removeItem(SearchOriginService.StorageKey);
  }

  requestDeviceOrigin(timeoutMs = 8000): Promise<SearchOrigin> {
    return new Promise<SearchOrigin>((resolve, reject) => {
      if (!('geolocation' in navigator)) {
        reject(new Error('Geolocation is not supported.'));
        return;
      }

      navigator.geolocation.getCurrentPosition(
        (position) => {
          const latitude = position.coords.latitude;
          const longitude = position.coords.longitude;
          resolve({
            latitude,
            longitude,
            label: 'Current location',
            source: 'device',
            updatedAt: new Date().toISOString(),
          });
        },
        (error) => {
          reject(error);
        },
        { enableHighAccuracy: false, timeout: timeoutMs, maximumAge: 60_000 }
      );
    });
  }

  private loadFromStorage(): SearchOrigin | null {
    try {
      const raw = localStorage.getItem(SearchOriginService.StorageKey);
      if (!raw) {
        return null;
      }
      const parsed = JSON.parse(raw) as Partial<SearchOrigin>;
      if (!parsed || typeof parsed !== 'object') {
        return null;
      }
      if (typeof parsed.latitude !== 'number' || typeof parsed.longitude !== 'number') {
        return null;
      }
      const label = typeof parsed.label === 'string' ? parsed.label : '';
      if (!label.trim()) {
        return null;
      }
      const source = parsed.source === 'device' || parsed.source === 'typed' ? parsed.source : 'typed';
      const updatedAt = typeof parsed.updatedAt === 'string' ? parsed.updatedAt : new Date().toISOString();
      return {
        latitude: parsed.latitude,
        longitude: parsed.longitude,
        label: label.trim(),
        city: typeof parsed.city === 'string' ? parsed.city : null,
        state: typeof parsed.state === 'string' ? parsed.state : null,
        source,
        updatedAt,
      };
    } catch {
      return null;
    }
  }
}
