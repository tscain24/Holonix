import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, catchError, map, of, switchMap, takeUntil } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PublicBusinessProfile, PublicBusinessService as PublicBusinessApiService, PublicBusinessSubService } from '../../../core/services/public-business.service';

@Component({
  selector: 'app-public-business-page',
  templateUrl: './public-business-page.component.html',
  styleUrls: ['./public-business-page.component.css'],
})
export class PublicBusinessPageComponent implements OnInit, OnDestroy {
  loading = false;
  error: string | null = null;
  business: PublicBusinessProfile | null = null;
  selectedSubService: PublicBusinessSubService | null = null;

  private readonly destroyed$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: PublicBusinessApiService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        map((params) => (params.get('businessCode') ?? '').trim()),
        switchMap((businessCode) => {
          this.selectedSubService = null;
          if (!businessCode) {
            this.business = null;
            this.error = 'Business code is missing.';
            return of(null);
          }
          this.loading = true;
          this.error = null;
          return this.api.getProfile(businessCode).pipe(
            catchError((err) => {
              this.business = null;
              const serverErrors = err?.error?.errors;
              if (Array.isArray(serverErrors) && serverErrors.length) {
                this.error = serverErrors.filter((x: any) => typeof x === 'string').join(' ');
              } else if (typeof err?.status === 'number' && err.status) {
                this.error = `Unable to load business profile (HTTP ${err.status}).`;
              } else {
                this.error = 'Unable to load business profile.';
              }
              // Helpful for debugging local dev/proxy issues.
              // eslint-disable-next-line no-console
              console.error('Public business profile load failed', err);
              return of(null);
            })
          );
        }),
        takeUntil(this.destroyed$)
      )
      .subscribe((profile) => {
        if (profile && !((profile.categoryName ?? '').trim())) {
          try {
            const stored = sessionStorage.getItem(`holonix_business_category_v1_${profile.businessCode}`) ?? '';
            const categoryName = stored.trim();
            if (categoryName) {
              profile = { ...profile, categoryName };
            }
          } catch {
            // ignore storage failures
          }
        }
        this.business = profile;
        this.loading = false;
      });
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }

  goBackToSearch(): void {
    const last = sessionStorage.getItem('holonix_last_search_url_v1');
    if (last && last.startsWith('/')) {
      void this.router.navigateByUrl(last);
      return;
    }
    void this.router.navigate(['/search']);
  }

  selectSubService(sub: PublicBusinessSubService): void {
    this.selectedSubService = sub;
    this.snackBar.open('Booking flow coming soon.', 'OK', { duration: 2500 });
  }

  showComingSoon(section: string): void {
    const label = (section ?? '').trim();
    this.snackBar.open(`${label || 'This section'} is coming soon.`, 'OK', { duration: 2200 });
  }

  get businessIconSrc(): string | null {
    const raw = (this.business?.businessIconBase64 ?? '').trim();
    if (!raw) {
      return null;
    }
    if (raw.startsWith('data:image')) {
      return raw;
    }
    return `data:image/png;base64,${raw}`;
  }

  get businessInitials(): string {
    const name = (this.business?.name ?? '').trim();
    if (!name) {
      return 'B';
    }
    const parts = name.split(/\s+/).filter(Boolean);
    const first = parts[0]?.[0] ?? 'B';
    const last = parts.length > 1 ? parts[parts.length - 1]?.[0] ?? '' : '';
    return `${first}${last}`.toUpperCase();
  }

  formatDuration(minutes: number): string {
    const safe = Number.isFinite(minutes) ? Math.max(0, Math.trunc(minutes)) : 0;
    if (safe === 0) {
      return '';
    }
    if (safe < 60) {
      return `${safe} min`;
    }
    const hours = Math.floor(safe / 60);
    const rem = safe % 60;
    if (rem === 0) {
      return `${hours} hr`;
    }
    return `${hours} hr ${rem} min`;
  }

  formatPrice(value: number): string {
    const safe = Number.isFinite(value) ? value : 0;
    return `$${safe.toFixed(0)}`;
  }

  get locationLabel(): string {
    const city = (this.business?.city ?? '').trim();
    const state = (this.business?.state ?? '').trim();
    if (city && state) {
      return `${city}, ${state}`;
    }
    return city || state || '';
  }

  get fullAddress(): string {
    const parts = [
      (this.business?.address1 ?? '').trim(),
      (this.business?.address2 ?? '').trim(),
      this.locationLabel,
      (this.business?.zipCode ?? '').trim(),
    ].filter(Boolean);
    return parts.join(', ');
  }
}
