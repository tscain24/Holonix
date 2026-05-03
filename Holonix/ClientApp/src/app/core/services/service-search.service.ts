import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ServiceSearchResult {
  businessId: number;
  businessName: string;
  serviceId: number;
  serviceName: string;
  serviceDescription?: string | null;
  categoryId: number;
  categoryName: string;
  semanticScore?: number | null;
  distanceMiles?: number | null;
  price?: number | null;
}

export interface BusinessSearchResult {
  businessId: number;
  businessCode?: string | null;
  businessName: string;
  businessIconBase64?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  categoryId: number;
  categoryName: string;
  semanticScore?: number | null;
  distanceMiles?: number | null;
}

export interface TopCategoryResult {
  categoryId: number;
  categoryName: string;
  semanticScore?: number | null;
}

export interface BusinessSearchResponse {
  topCategories: TopCategoryResult[];
  pageNumber?: number;
  pageSize?: number;
  totalCount?: number;
  results: BusinessSearchResult[];
}

@Injectable({
  providedIn: 'root',
})
export class ServiceSearchService {
  private readonly endpoint = '/api/search/services';
  private readonly businessEndpoint = '/api/search/businesses';

  constructor(private http: HttpClient) {}

  searchServices(query: string, lat: number, lng: number, radiusMiles = 25): Observable<ServiceSearchResult[]> {
    const params = new HttpParams()
      .set('query', query)
      .set('lat', lat.toString())
      .set('lng', lng.toString())
      .set('radiusMiles', radiusMiles.toString());

    return this.http.get<ServiceSearchResult[]>(this.endpoint, { params });
  }

  searchBusinesses(query: string, lat: number, lng: number, radiusMiles = 25): Observable<BusinessSearchResult[]> {
    const params = new HttpParams()
      .set('query', query)
      .set('lat', lat.toString())
      .set('lng', lng.toString())
      .set('radiusMiles', radiusMiles.toString());

    return this.http.get<BusinessSearchResult[]>(this.businessEndpoint, { params });
  }

  searchBusinessesWithTopCategories(
    query: string,
    lat: number,
    lng: number,
    radiusMiles = 25,
    pageNumber = 1,
    pageSize = 10
  ): Observable<BusinessSearchResponse> {
    const params = new HttpParams()
      .set('query', query)
      .set('lat', lat.toString())
      .set('lng', lng.toString())
      .set('radiusMiles', radiusMiles.toString())
      .set('includeTopCategories', 'true')
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<BusinessSearchResponse>(this.businessEndpoint, { params });
  }
}
