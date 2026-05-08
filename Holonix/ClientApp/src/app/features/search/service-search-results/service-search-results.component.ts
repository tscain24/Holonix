import { AfterViewInit, Component, ElementRef, HostListener, NgZone, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, catchError, distinctUntilChanged, finalize, firstValueFrom, map, of, switchMap, takeUntil } from 'rxjs';
import { BusinessSearchResult, BusinessSearchResponse, TopCategoryResult, ServiceSearchService } from '../../../core/services/service-search.service';
import { SearchOriginService } from '../../../core/services/search-origin.service';
import { MapboxConfigService } from '../../../core/services/mapbox-config.service';

declare global {
  interface Window {
    mapboxgl?: any;
  }
}

@Component({
  selector: 'app-service-search-results',
  templateUrl: './service-search-results.component.html',
  styleUrls: ['./service-search-results.component.css'],
})
export class ServiceSearchResultsComponent implements OnInit, AfterViewInit, OnDestroy {
  private static readonly PageSize = 10;
  private static readonly MobileCategoryBreakpointPx = 640;

  query = '';
  radiusMiles = 25;
  loading = false;
  error: string | null = null;
  results: BusinessSearchResult[] = [];
  topCategories: TopCategoryResult[] = [];
  locationLabel = '';
  relatedCategoriesExpanded = false;
  isMobileLayout = false;
  mapStatus: 'idle' | 'loading' | 'ready' | 'error' = 'idle';
  mapError: string | null = null;
  currentPage = 1;
  totalCount = 0;

  @ViewChild('mapEl') private mapEl?: ElementRef<HTMLDivElement>;

  private readonly destroyed$ = new Subject<void>();
  private static readonly EmptyResponse: BusinessSearchResponse = { topCategories: [], results: [] };

  private map: any | null = null;
  private markers: any[] = [];
  private mapInitPromise: Promise<void> | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private searchOrigin: SearchOriginService,
    private searchService: ServiceSearchService,
    private mapboxConfig: MapboxConfigService,
    private zone: NgZone
  ) {}

  ngOnInit(): void {
    this.refreshMobileLayout();
    this.route.queryParamMap
      .pipe(
        map((params) => {
          const query = (params.get('query') ?? '').trim();
          const radiusRaw = (params.get('radiusMiles') ?? '').trim();
          const parsedRadius = radiusRaw ? Number(radiusRaw) : NaN;
          const radiusMiles = Number.isFinite(parsedRadius) ? parsedRadius : 25;
          const pageRaw = (params.get('page') ?? '').trim();
          const parsedPage = pageRaw ? Number(pageRaw) : NaN;
          const pageNumber = Number.isFinite(parsedPage) ? Math.trunc(parsedPage) : 1;
          const trigger = (params.get('t') ?? '').trim();
          return { query, radiusMiles, pageNumber, trigger };
        }),
        distinctUntilChanged((a, b) => a.query === b.query && a.radiusMiles === b.radiusMiles && a.pageNumber === b.pageNumber && a.trigger === b.trigger),
        switchMap(({ query, radiusMiles, pageNumber }) => {
          this.query = query;
          this.radiusMiles = clamp(radiusMiles, 1, 100);
          this.currentPage = Math.max(1, pageNumber);
          this.relatedCategoriesExpanded = false;
          if (!this.query) {
            this.results = [];
            this.topCategories = [];
            this.totalCount = 0;
            this.error = 'Enter a search query to begin.';
            return of(ServiceSearchResultsComponent.EmptyResponse);
          }

          const origin = this.searchOrigin.currentOrigin;
          if (!origin) {
            this.results = [];
            this.topCategories = [];
            this.totalCount = 0;
            this.error = 'Set a location to search nearby services.';
            return of(ServiceSearchResultsComponent.EmptyResponse);
          }

          this.locationLabel = origin.label ?? '';
          this.loading = true;
          this.error = null;
          return this.searchService.searchBusinessesWithTopCategories(
            this.query,
            origin.latitude,
            origin.longitude,
            this.radiusMiles,
            this.currentPage,
            ServiceSearchResultsComponent.PageSize
          ).pipe(
            catchError((err) => {
              if (err?.error?.errors?.length) {
                this.error = err.error.errors.join(' ');
              } else {
                this.error = 'Search failed. Please try again.';
              }
              return of(ServiceSearchResultsComponent.EmptyResponse);
            }),
            finalize(() => {
              this.loading = false;
            })
          );
        }),
        takeUntil(this.destroyed$)
      )
      .subscribe(async (response) => {
        this.topCategories = response.topCategories ?? [];
        this.results = response.results ?? [];
        this.totalCount = response.totalCount ?? this.results.length;
        this.currentPage = response.pageNumber ?? this.currentPage;
        await this.updateMapAsync();
      });
  }

  toggleRelatedCategories(): void {
    this.relatedCategoriesExpanded = !this.relatedCategoriesExpanded;
  }

  get visibleTopCategories(): TopCategoryResult[] {
    const categories = this.topCategories ?? [];
    if (!this.isMobileLayout) {
      return categories;
    }
    if (this.relatedCategoriesExpanded) {
      return categories;
    }
    return categories.slice(0, 1);
  }

  private refreshMobileLayout(): void {
    if (typeof window === 'undefined') {
      this.isMobileLayout = false;
      return;
    }
    this.isMobileLayout = window.innerWidth <= ServiceSearchResultsComponent.MobileCategoryBreakpointPx;
    if (!this.isMobileLayout) {
      this.relatedCategoriesExpanded = false;
    }
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.refreshMobileLayout();
  }

  ngAfterViewInit(): void {
    // Avoid ExpressionChangedAfterItHasBeenCheckedError by deferring map status updates
    // until after the initial view check completes.
    window.setTimeout(() => {
      void this.updateMapAsync();
    }, 0);
  }

  ngOnDestroy(): void {
    if (this.map) {
      try {
        this.map.remove();
      } catch {
        // ignore teardown errors
      }
      this.map = null;
    }
    this.markers = [];
    this.destroyed$.next();
    this.destroyed$.complete();
  }

  submit(): void {
    const nextQuery = this.query.trim();
    if (!nextQuery) {
      return;
    }

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        query: nextQuery,
        radiusMiles: this.radiusMiles,
        page: 1,
      },
      queryParamsHandling: 'merge',
    });
  }

  setRadiusMiles(nextRadiusMiles: number): void {
    this.radiusMiles = clamp(nextRadiusMiles, 1, 100);
    if (!this.query.trim()) {
      return;
    }

    void this.updateMapAsync();

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        radiusMiles: this.radiusMiles,
        page: 1,
      },
      queryParamsHandling: 'merge',
    });
  }

  viewBusiness(item: BusinessSearchResult): void {
    const businessCode = (item.businessCode ?? '').trim();
    if (!businessCode) {
      return;
    }
    this.router.navigate(['/business', businessCode]);
  }

  applyRelatedCategory(categoryName: string): void {
    const nextQuery = (categoryName ?? '').trim();
    if (!nextQuery) {
      return;
    }

    this.query = nextQuery;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        query: nextQuery,
        page: 1,
        t: Date.now(),
      },
      queryParamsHandling: 'merge',
    });
  }

  get pageStart(): number {
    if (this.totalCount === 0) {
      return 0;
    }
    return (this.currentPage - 1) * ServiceSearchResultsComponent.PageSize + 1;
  }

  get pageEnd(): number {
    if (this.totalCount === 0) {
      return 0;
    }
    return Math.min(this.currentPage * ServiceSearchResultsComponent.PageSize, this.totalCount);
  }

  get hasPreviousPage(): boolean {
    return this.currentPage > 1;
  }

  get hasNextPage(): boolean {
    return this.pageEnd < this.totalCount;
  }

  goToPreviousPage(): void {
    if (!this.hasPreviousPage || this.loading) {
      return;
    }
    this.goToPage(this.currentPage - 1);
  }

  goToNextPage(): void {
    if (!this.hasNextPage || this.loading) {
      return;
    }
    this.goToPage(this.currentPage + 1);
  }

  private goToPage(pageNumber: number): void {
    const next = Math.max(1, Math.trunc(pageNumber));
    this.currentPage = next;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        page: next,
      },
      queryParamsHandling: 'merge',
    });
  }

  formatMatchLabel(score: number | null | undefined): string {
    if (score == null) {
      return 'Match';
    }
    if (score >= 0.72) {
      return 'Best match';
    }
    if (score >= 0.58) {
      return 'Great match';
    }
    if (score >= 0.45) {
      return 'Good match';
    }
    return 'Match';
  }

  getBusinessLogoSrc(item: BusinessSearchResult): string | null {
    const raw = (item.businessIconBase64 ?? '').trim();
    if (!raw) {
      return null;
    }

    if (raw.startsWith('data:image')) {
      return raw;
    }

    return `data:image/png;base64,${raw}`;
  }

  getBusinessInitials(name: string): string {
    const parts = (name || '').trim().split(/\s+/).filter(Boolean);
    const first = parts[0]?.charAt(0) ?? '';
    const last = parts.length > 1 ? parts[parts.length - 1]?.charAt(0) ?? '' : '';
    const initials = `${first}${last}`.toUpperCase();
    return initials || 'B';
  }

  private async updateMapAsync(): Promise<void> {
    const origin = this.searchOrigin.currentOrigin;
    if (!origin) {
      return;
    }
    if (!this.mapEl?.nativeElement) {
      return;
    }
    if (!this.query.trim()) {
      this.mapStatus = 'idle';
      return;
    }

    try {
      this.mapStatus = 'loading';
      this.mapError = null;
      await (this.mapInitPromise ?? (this.mapInitPromise = this.initMapAsync(origin.latitude, origin.longitude)));
      if (!this.map) {
        return;
      }
      this.renderMarkers(origin.latitude, origin.longitude);
      this.renderRadius(origin.latitude, origin.longitude, this.radiusMiles);
      this.fitToResults(origin.latitude, origin.longitude, this.radiusMiles);
      this.mapStatus = 'ready';
    } catch (err) {
      // If mapbox fails to load (blocked network/token/etc), keep UI usable without the map.
      // Surface enough info to debug without breaking the results list.
      // eslint-disable-next-line no-console
      console.warn('Mapbox init failed', err);
      this.mapStatus = 'error';
      const message = errorMessageFromUnknown(err);
      this.mapError = message ? `Map failed: ${message}` : 'Map failed to load. Check Mapbox token and browser console.';
    }
  }

  private async initMapAsync(lat: number, lng: number): Promise<void> {
    const accessToken = await firstValueFrom(this.mapboxConfig.getAccessToken());
    if (!accessToken) {
      throw new Error('Missing Mapbox token.');
    }

    const mapboxgl = await loadMapboxGlAsync();
    mapboxgl.accessToken = accessToken;

    await this.zone.runOutsideAngular(async () => {
      this.map = new mapboxgl.Map({
        container: this.mapEl!.nativeElement,
        style: 'mapbox://styles/tscain11/cmop6iwcy000w01rw36lhexzi',
        center: [lng, lat],
        zoom: 10,
      });

      this.map.addControl(new mapboxgl.NavigationControl({ showCompass: true }), 'top-right');

      this.map.on('error', (ev: any) => {
        const message = errorMessageFromUnknown(ev?.error ?? ev);
        if (!message) {
          return;
        }
        if (message.toLowerCase().includes('style')) {
          // eslint-disable-next-line no-console
          console.warn('Mapbox style failed to load:', message);
        }
      });

      await new Promise<void>((resolve) => {
        this.map.once('load', () => resolve());
      });

      if (!this.map.getSource('search-origin')) {
        this.map.addSource('search-origin', {
          type: 'geojson',
          data: {
            type: 'FeatureCollection',
            features: [],
          },
        });
        this.map.addLayer({
          id: 'search-origin-point',
          type: 'circle',
          source: 'search-origin',
          paint: {
            'circle-radius': 7,
            'circle-color': '#1d4ed8',
            'circle-stroke-width': 2,
            'circle-stroke-color': '#ffffff',
          },
        });
      }

      if (!this.map.getSource('search-radius')) {
        this.map.addSource('search-radius', {
          type: 'geojson',
          data: { type: 'FeatureCollection', features: [] },
        });
        this.map.addLayer({
          id: 'search-radius-fill',
          type: 'fill',
          source: 'search-radius',
          paint: {
            'fill-color': '#60a5fa',
            'fill-opacity': 0.12,
          },
        });
        this.map.addLayer({
          id: 'search-radius-line',
          type: 'line',
          source: 'search-radius',
          paint: {
            'line-color': '#2563eb',
            'line-width': 2,
            'line-opacity': 0.7,
          },
        });
      }
    });
  }

  private renderMarkers(originLat: number, originLng: number): void {
    if (!this.map) {
      return;
    }

    // origin
    const originSource = this.map.getSource('search-origin');
    if (originSource?.setData) {
      originSource.setData({
        type: 'FeatureCollection',
        features: [
          {
            type: 'Feature',
            geometry: { type: 'Point', coordinates: [originLng, originLat] },
            properties: {},
          },
        ],
      });
    }

    // business markers
    for (const marker of this.markers) {
      try {
        marker.remove();
      } catch {
        // ignore
      }
    }
    this.markers = [];

    const mapboxgl = window.mapboxgl;
    if (!mapboxgl) {
      return;
    }

    for (const item of this.results) {
      if (item.latitude == null || item.longitude == null) {
        continue;
      }
      const popupHtml = `<strong>${escapeHtml(item.businessName)}</strong><div>${escapeHtml(item.categoryName)}</div>`;
      const popup = new mapboxgl.Popup({ offset: 16 }).setHTML(popupHtml);
      const marker = new mapboxgl.Marker({ color: '#1d4ed8' })
        .setLngLat([item.longitude, item.latitude])
        .setPopup(popup)
        .addTo(this.map);
      this.markers.push(marker);
    }
  }

  private renderRadius(originLat: number, originLng: number, radiusMiles: number): void {
    if (!this.map) {
      return;
    }
    const radiusSource = this.map.getSource('search-radius');
    if (!radiusSource?.setData) {
      return;
    }

    const polygon = createCirclePolygon(originLng, originLat, clamp(radiusMiles, 1, 100));
    radiusSource.setData({
      type: 'FeatureCollection',
      features: [
        {
          type: 'Feature',
          geometry: polygon,
          properties: {},
        },
      ],
    });
  }

  private fitToResults(originLat: number, originLng: number, radiusMiles: number): void {
    if (!this.map || !window.mapboxgl) {
      return;
    }

    const mapboxgl = window.mapboxgl;
    const bounds = new mapboxgl.LngLatBounds([originLng, originLat], [originLng, originLat]);
    for (const item of this.results) {
      if (item.latitude == null || item.longitude == null) {
        continue;
      }
      bounds.extend([item.longitude, item.latitude]);
    }

    const circle = createCirclePolygon(originLng, originLat, clamp(radiusMiles, 1, 100));
    const ring = circle.coordinates?.[0] ?? [];
    for (const coord of ring) {
      bounds.extend(coord);
    }

    try {
      this.map.fitBounds(bounds, { padding: 42, maxZoom: 12, duration: 450 });
    } catch {
      // ignore
    }
  }
}

function clamp(value: number, min: number, max: number): number {
  if (!Number.isFinite(value)) {
    return min;
  }
  return Math.max(min, Math.min(max, value));
}

const mapboxGlCssHref = 'https://api.mapbox.com/mapbox-gl-js/v3.4.0/mapbox-gl.css';
const mapboxGlJsSrc = 'https://api.mapbox.com/mapbox-gl-js/v3.4.0/mapbox-gl.js';
let mapboxGlLoader: Promise<any> | null = null;

function loadMapboxGlAsync(): Promise<any> {
  if (typeof window === 'undefined') {
    return Promise.reject(new Error('No window.'));
  }
  if (window.mapboxgl) {
    return Promise.resolve(window.mapboxgl);
  }
  if (mapboxGlLoader) {
    return mapboxGlLoader;
  }

  mapboxGlLoader = new Promise<any>((resolve, reject) => {
    try {
      const hasCss = Array.from(document.styleSheets).some((sheet) => (sheet as CSSStyleSheet).href === mapboxGlCssHref);
      if (!hasCss) {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = mapboxGlCssHref;
        document.head.appendChild(link);
      }

      const script = document.createElement('script');
      script.src = mapboxGlJsSrc;
      script.async = true;
      script.onload = () => resolve(window.mapboxgl);
      script.onerror = () => reject(new Error('Failed to load Mapbox GL JS.'));
      document.head.appendChild(script);
    } catch (err) {
      reject(err);
    }
  });

  return mapboxGlLoader;
}

function escapeHtml(value: string): string {
  return (value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

function errorMessageFromUnknown(err: unknown): string {
  if (!err) {
    return '';
  }
  if (err instanceof Error) {
    return err.message;
  }
  if (typeof err === 'string') {
    return err;
  }
  if (typeof err === 'object') {
    const record = err as Record<string, unknown>;
    const message = record['message'];
    if (typeof message === 'string' && message.trim()) {
      return message.trim();
    }
    const errors = (record['error'] as any)?.errors;
    if (Array.isArray(errors) && errors.length) {
      return errors.filter((x) => typeof x === 'string').join(' ');
    }
    try {
      return JSON.stringify(err);
    } catch {
      return '';
    }
  }
  return '';
}

function createCirclePolygon(centerLng: number, centerLat: number, radiusMiles: number, steps = 72): { type: 'Polygon'; coordinates: number[][][] } {
  const radiusMeters = radiusMiles * 1609.344;
  const coords: number[][] = [];
  for (let i = 0; i <= steps; i++) {
    const bearing = (i / steps) * 360;
    coords.push(destinationPoint(centerLng, centerLat, radiusMeters, bearing));
  }
  return { type: 'Polygon', coordinates: [coords] };
}

function destinationPoint(lng: number, lat: number, distanceMeters: number, bearingDegrees: number): number[] {
  const R = 6371008.8;
  const bearing = (bearingDegrees * Math.PI) / 180;
  const φ1 = (lat * Math.PI) / 180;
  const λ1 = (lng * Math.PI) / 180;
  const δ = distanceMeters / R;

  const sinφ1 = Math.sin(φ1);
  const cosφ1 = Math.cos(φ1);
  const sinδ = Math.sin(δ);
  const cosδ = Math.cos(δ);

  const sinφ2 = sinφ1 * cosδ + cosφ1 * sinδ * Math.cos(bearing);
  const φ2 = Math.asin(sinφ2);
  const y = Math.sin(bearing) * sinδ * cosφ1;
  const x = cosδ - sinφ1 * sinφ2;
  const λ2 = λ1 + Math.atan2(y, x);

  const outLat = (φ2 * 180) / Math.PI;
  const outLng = (((λ2 * 180) / Math.PI + 540) % 360) - 180;
  return [outLng, outLat];
}
