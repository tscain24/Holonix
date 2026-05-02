import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, catchError, distinctUntilChanged, finalize, map, of, switchMap, takeUntil } from 'rxjs';
import { BusinessSearchResult, BusinessSearchResponse, TopCategoryResult, ServiceSearchService } from '../../../core/services/service-search.service';
import { SearchOriginService } from '../../../core/services/search-origin.service';

@Component({
  selector: 'app-service-search-results',
  templateUrl: './service-search-results.component.html',
  styleUrls: ['./service-search-results.component.css'],
})
export class ServiceSearchResultsComponent implements OnInit, OnDestroy {
  query = '';
  radiusMiles = 25;
  loading = false;
  error: string | null = null;
  results: BusinessSearchResult[] = [];
  topCategories: TopCategoryResult[] = [];
  locationLabel = '';

  private readonly destroyed$ = new Subject<void>();
  private static readonly EmptyResponse: BusinessSearchResponse = { topCategories: [], results: [] };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private searchOrigin: SearchOriginService,
    private searchService: ServiceSearchService
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(
        map((params) => {
          const query = (params.get('query') ?? '').trim();
          const radiusRaw = (params.get('radiusMiles') ?? '').trim();
          const parsedRadius = radiusRaw ? Number(radiusRaw) : NaN;
          const radiusMiles = Number.isFinite(parsedRadius) ? parsedRadius : 25;
          const trigger = (params.get('t') ?? '').trim();
          return { query, radiusMiles, trigger };
        }),
        distinctUntilChanged((a, b) => a.query === b.query && a.radiusMiles === b.radiusMiles && a.trigger === b.trigger),
        switchMap(({ query, radiusMiles }) => {
          this.query = query;
          this.radiusMiles = clamp(radiusMiles, 1, 100);
          if (!this.query) {
            this.results = [];
            this.topCategories = [];
            this.error = 'Enter a search query to begin.';
            return of(ServiceSearchResultsComponent.EmptyResponse);
          }

          const origin = this.searchOrigin.currentOrigin;
          if (!origin) {
            this.results = [];
            this.topCategories = [];
            this.error = 'Set a location to search nearby services.';
            return of(ServiceSearchResultsComponent.EmptyResponse);
          }

          this.locationLabel = origin.label ?? '';
          this.loading = true;
          this.error = null;
          return this.searchService.searchBusinessesWithTopCategories(this.query, origin.latitude, origin.longitude, this.radiusMiles).pipe(
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
        })
      )
      .subscribe((response) => {
        this.topCategories = response.topCategories ?? [];
        this.results = response.results ?? [];
      });
  }

  ngOnDestroy(): void {
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
      },
      queryParamsHandling: 'merge',
    });
  }

  setRadiusMiles(nextRadiusMiles: number): void {
    this.radiusMiles = clamp(nextRadiusMiles, 1, 100);
    if (!this.query.trim()) {
      return;
    }

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        radiusMiles: this.radiusMiles,
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
}

function clamp(value: number, min: number, max: number): number {
  if (!Number.isFinite(value)) {
    return min;
  }
  return Math.max(min, Math.min(max, value));
}
