import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, finalize, of, switchMap, takeUntil } from 'rxjs';
import { AuthSessionService } from '../../core/services/auth-session.service';
import { BusinessService } from '../../core/services/business.service';
import { AddressSuggestion, GeocodingService } from '../../core/services/geocoding.service';
import { SearchOriginService } from '../../core/services/search-origin.service';

interface JwtPayload {
  role?: string | string[];
  roles?: string[];
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[];
}

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'],
})
export class HomeComponent implements OnInit, OnDestroy {
  isLoggedIn = false;
  hasBusiness = false;
  firstName = 'User';
  serviceQuery = '';
  locationDisplay = '';
  locationSuggestions: AddressSuggestion[] = [];
  locationDropdownOpen = false;
  highlightedLocationIndex = -1;
  loadingLocationSuggestions = false;
  locationAutocompleteError: string | null = null;
  resolvingDeviceLocation = false;
  servicesNearDisplay = 'Woodstock, Georgia';
  private readonly locationSearch$ = new Subject<string>();
  private readonly destroyed$ = new Subject<void>();
  guestCategories = [
    { label: 'Cleaning', icon: 'cleaning_services' },
    { label: 'Plumbing', icon: 'plumbing' },
    { label: 'Electrical', icon: 'electrical_services' },
    { label: 'Moving', icon: 'local_shipping' },
    { label: 'Lawn Care', icon: 'grass' },
    { label: 'Painting', icon: 'format_paint' },
    { label: 'Handyman', icon: 'handyman' },
    { label: 'More', icon: 'more_horiz' },
  ];

  constructor(
    private router: Router,
    private snackBar: MatSnackBar,
    private authSession: AuthSessionService,
    private businessService: BusinessService,
    private geocodingService: GeocodingService,
    private searchOrigin: SearchOriginService
  ) {}

  ngOnInit(): void {
    const message = (history.state && history.state.toastMessage) as string | undefined;
    if (message) {
      this.snackBar.open(message, 'Close', {
        duration: 3500,
        panelClass: ['snack-success'],
      });
      history.replaceState({}, '');
    }

    this.refreshSessionFromStorage(true);

    this.authSession.sessionChanged$
      .pipe(takeUntil(this.destroyed$))
      .subscribe(() => this.refreshSessionFromStorage(false));

    this.locationDisplay = this.searchOrigin.currentOrigin?.label ?? '';
    this.searchOrigin.origin$
      .pipe(takeUntil(this.destroyed$))
      .subscribe((origin) => {
        this.locationDisplay = origin?.label ?? '';
        this.servicesNearDisplay = this.formatServicesNear(origin);
      });

    this.servicesNearDisplay = this.formatServicesNear(this.searchOrigin.currentOrigin);

    this.locationSearch$
      .pipe(
        debounceTime(600),
        distinctUntilChanged(),
        switchMap((value) => {
          const term = value.trim();
          const startsWithDigit = term.length > 0 && /^\d/.test(term);
          const minimumLength = startsWithDigit ? 2 : 3;
          if (term.length < minimumLength) {
            this.locationSuggestions = [];
            this.loadingLocationSuggestions = false;
            this.locationAutocompleteError = null;
            return of([]);
          }

          this.loadingLocationSuggestions = true;
          this.locationAutocompleteError = null;
          return this.geocodingService.getLocationSuggestions(term, 'US', 6).pipe(
            catchError((err) => {
              // eslint-disable-next-line no-console
              console.error('Failed to load location suggestions.', err);
              if (err?.status === 401) {
                this.locationAutocompleteError = 'Your session expired. Please sign in again.';
              } else {
                this.locationAutocompleteError = 'Could not load location suggestions.';
              }
              return of([]);
            }),
            finalize(() => {
              this.loadingLocationSuggestions = false;
            })
          );
        })
      )
      .subscribe((suggestions) => {
        this.locationSuggestions = suggestions;
        if (this.locationDropdownOpen && suggestions.length === 0) {
          this.highlightedLocationIndex = -1;
        }
      });
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }

  goToBusinessEntry(): void {
    if (!this.isLoggedIn) {
      this.router.navigate(['/register']);
      return;
    }
    this.router.navigate([this.hasBusiness ? '/business' : '/business/create']);
  }

  submitGuestSearch(): void {
    const query = this.serviceQuery.trim();
    if (!query) {
      this.snackBar.open('Enter what you need help with to search.', 'Close', { duration: 3000 });
      return;
    }

    if (!this.searchOrigin.currentOrigin) {
      this.snackBar.open('Set a location to search nearby services.', 'Close', { duration: 3500 });
      return;
    }

    this.router.navigate(['/search'], {
      queryParams: { query, t: Date.now() },
    });
  }

  clearServiceQuery(): void {
    this.serviceQuery = '';
  }

  private decodeJwtPayload(token: string | null): JwtPayload | null {
    try {
      if (!token) {
        return null;
      }

      const tokenParts = token.split('.');
      if (tokenParts.length < 2) {
        return null;
      }

      const base64 = tokenParts[1].replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
      return JSON.parse(atob(padded)) as JwtPayload;
    } catch {
      return null;
    }
  }

  private hasOwnerRole(payload: JwtPayload | null): boolean {
    if (!payload) {
      return false;
    }

    const roleValues = [
      payload.role,
      payload.roles,
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
    ].flatMap((value) => Array.isArray(value) ? value : value ? [value] : []);

    return roleValues.some((role) => role.toLowerCase() === 'owner');
  }

  private refreshSessionFromStorage(forceBusinessRefresh: boolean): void {
    const previousLoggedIn = this.isLoggedIn;
    const token = localStorage.getItem('holonix_token');
    const savedDisplayName = localStorage.getItem('holonix_display_name') ?? '';
    const nextLoggedIn = !!token;

    this.isLoggedIn = nextLoggedIn;
    this.hasBusiness = nextLoggedIn ? this.hasOwnerRole(this.decodeJwtPayload(token)) : false;
    this.firstName = (savedDisplayName.trim() || 'User').split(' ')[0] || 'User';

    const shouldRefreshBusinesses = nextLoggedIn && (forceBusinessRefresh || !previousLoggedIn);
    if (!shouldRefreshBusinesses) {
      return;
    }

    this.businessService.getMyBusinesses().subscribe({
      next: (businesses) => {
        this.hasBusiness = businesses.length > 0;
      },
      error: () => {
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }
      },
    });
  }

  onLocationInput(input: HTMLInputElement): void {
    const value = input.value;
    this.locationDisplay = value;
    this.locationDropdownOpen = true;
    this.highlightedLocationIndex = this.locationSuggestions.length > 0 ? 0 : -1;
    this.locationSearch$.next(value);
  }

  openLocationDropdown(input: HTMLInputElement): void {
    this.locationDropdownOpen = true;
    this.highlightedLocationIndex = this.locationSuggestions.length > 0 ? 0 : -1;
    this.locationSearch$.next(input.value);
  }

  closeLocationDropdown(): void {
    window.setTimeout(() => {
      this.locationDropdownOpen = false;
      this.highlightedLocationIndex = -1;
    }, 120);
  }

  onLocationKeydown(event: KeyboardEvent): void {
    const options = this.locationSuggestions;
    if (options.length === 0 && event.key !== 'Escape') {
      return;
    }

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.locationDropdownOpen = true;
        this.highlightedLocationIndex = options.length === 0
          ? -1
          : (this.highlightedLocationIndex + 1 + options.length) % options.length;
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.locationDropdownOpen = true;
        this.highlightedLocationIndex = options.length === 0
          ? -1
          : (this.highlightedLocationIndex - 1 + options.length) % options.length;
        break;
      case 'Enter':
        if (!this.locationDropdownOpen || this.highlightedLocationIndex < 0 || this.highlightedLocationIndex >= options.length) {
          return;
        }

        event.preventDefault();
        this.selectLocationSuggestion(options[this.highlightedLocationIndex]);
        break;
      case 'Escape':
        this.locationDropdownOpen = false;
        this.highlightedLocationIndex = -1;
        break;
    }
  }

  selectLocationSuggestion(suggestion: AddressSuggestion): void {
    const label = suggestion.fullAddress || suggestion.address1;
    this.searchOrigin.setOrigin({
      latitude: suggestion.latitude,
      longitude: suggestion.longitude,
      label,
      city: suggestion.city ?? null,
      state: suggestion.state ?? null,
      source: 'typed',
    });

    this.locationDisplay = label;
    this.locationDropdownOpen = false;
    this.highlightedLocationIndex = -1;
  }

  useCurrentLocation(): void {
    if (this.resolvingDeviceLocation) {
      return;
    }

    this.resolvingDeviceLocation = true;
    this.searchOrigin.requestDeviceOrigin()
      .then((origin) => {
        this.searchOrigin.setOrigin({
          latitude: origin.latitude,
          longitude: origin.longitude,
          label: origin.label,
          city: null,
          state: null,
          source: origin.source,
        });
      })
      .catch((err) => {
        // eslint-disable-next-line no-console
        console.error('Failed to resolve device location.', err);
        this.locationAutocompleteError = 'Location access denied or unavailable.';
      })
      .finally(() => {
        this.resolvingDeviceLocation = false;
      });
  }

  private formatServicesNear(origin: { city?: string | null; state?: string | null; label?: string | null; source?: string | null } | null): string {
    if (!origin) {
      return 'your area';
    }

    if (origin.source === 'device') {
      return 'Current location';
    }

    const city = (origin.city ?? '').trim();
    const state = (origin.state ?? '').trim();
    if (city && state) {
      return `${city}, ${this.normalizeStateName(state)}`;
    }

    const label = (origin.label ?? '').trim();
    const parsedFromLabel = this.tryParseCityStateFromLabel(label);
    return parsedFromLabel || label || 'your area';
  }

  private tryParseCityStateFromLabel(label: string): string | null {
    const trimmed = label.trim();
    if (!trimmed) {
      return null;
    }

    const parts = trimmed.split(',').map((part) => part.trim()).filter(Boolean);
    if (parts.length < 2) {
      return null;
    }

    const firstPart = parts[0];
    const isStreetAddress = /^\d/.test(firstPart);

    const cityCandidate = isStreetAddress ? (parts[1] ?? '') : (parts[0] ?? '');
    const stateCandidate = isStreetAddress ? (parts[2] ?? '') : (parts[1] ?? '');

    const city = cityCandidate.trim();
    if (!city) {
      return null;
    }

    const cleanedState = stateCandidate
      .replace(/\b\d{5}(?:-\d{4})?\b/g, '')
      .replace(/\bUnited States\b/gi, '')
      .trim();

    if (!cleanedState) {
      return city;
    }

    return `${city}, ${this.normalizeStateName(cleanedState)}`;
  }

  private normalizeStateName(value: string): string {
    const key = value.trim().toUpperCase();
    const stateNames: Record<string, string> = {
      AL: 'Alabama',
      AK: 'Alaska',
      AZ: 'Arizona',
      AR: 'Arkansas',
      CA: 'California',
      CO: 'Colorado',
      CT: 'Connecticut',
      DE: 'Delaware',
      FL: 'Florida',
      GA: 'Georgia',
      HI: 'Hawaii',
      ID: 'Idaho',
      IL: 'Illinois',
      IN: 'Indiana',
      IA: 'Iowa',
      KS: 'Kansas',
      KY: 'Kentucky',
      LA: 'Louisiana',
      ME: 'Maine',
      MD: 'Maryland',
      MA: 'Massachusetts',
      MI: 'Michigan',
      MN: 'Minnesota',
      MS: 'Mississippi',
      MO: 'Missouri',
      MT: 'Montana',
      NE: 'Nebraska',
      NV: 'Nevada',
      NH: 'New Hampshire',
      NJ: 'New Jersey',
      NM: 'New Mexico',
      NY: 'New York',
      NC: 'North Carolina',
      ND: 'North Dakota',
      OH: 'Ohio',
      OK: 'Oklahoma',
      OR: 'Oregon',
      PA: 'Pennsylvania',
      RI: 'Rhode Island',
      SC: 'South Carolina',
      SD: 'South Dakota',
      TN: 'Tennessee',
      TX: 'Texas',
      UT: 'Utah',
      VT: 'Vermont',
      VA: 'Virginia',
      WA: 'Washington',
      WV: 'West Virginia',
      WI: 'Wisconsin',
      WY: 'Wyoming',
      DC: 'District of Columbia',
    };

    return stateNames[key] ?? value.trim();
  }

  searchCategory(label: string): void {
    const query = (label ?? '').trim();
    if (!query) {
      return;
    }

    if (!this.searchOrigin.currentOrigin) {
      this.snackBar.open('Set a location to search nearby services.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    this.router.navigate(['/search'], {
      queryParams: {
        query,
        radiusMiles: 25,
        page: 1,
        t: Date.now(),
      },
    });
  }
}
