import { Component, HostListener } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthSessionService } from '../../services/auth-session.service';
import { AddressSuggestion, GeocodingService } from '../../services/geocoding.service';
import { SearchOriginService } from '../../services/search-origin.service';
import { Subject, catchError, debounceTime, distinctUntilChanged, finalize, of, switchMap, takeUntil } from 'rxjs';

@Component({
  selector: 'app-header',
  templateUrl: './app-header.component.html',
  styleUrls: ['./app-header.component.css'],
})
export class AppHeaderComponent {
  hideHeader = false;
  isLoggedIn = false;
  displayName = '';
  userInitials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;
  locationDisplay = '';
  locationSuggestions: AddressSuggestion[] = [];
  locationDropdownOpen = false;
  highlightedLocationIndex = -1;
  loadingLocationSuggestions = false;
  locationAutocompleteError: string | null = null;
  resolvingDeviceLocation = false;
  private readonly locationSearch$ = new Subject<string>();
  private readonly destroyed$ = new Subject<void>();

  constructor(
    private router: Router,
    private authSession: AuthSessionService,
    private geocodingService: GeocodingService,
    private searchOrigin: SearchOriginService
  ) {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.applyRouteVisibility(event.urlAfterRedirects);
        this.refreshSessionFromStorage();
        this.isUserMenuOpen = false;
      });
  }

  ngOnInit(): void {
    this.applyRouteVisibility(this.router.url);
    this.refreshSessionFromStorage();

    this.locationDisplay = this.searchOrigin.currentOrigin?.label ?? '';
    this.searchOrigin.origin$
      .pipe(takeUntil(this.destroyed$))
      .subscribe((origin) => {
        this.locationDisplay = origin?.label ?? '';
      });

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

  toggleUserMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.isUserMenuOpen = !this.isUserMenuOpen;
  }

  viewProfile(): void {
    this.isUserMenuOpen = false;
    this.router.navigate(['/profile']);
  }

  signOut(): void {
    this.isUserMenuOpen = false;
    this.authSession.logout();
    this.refreshSessionFromStorage();
  }

  onSearchSubmit(event: Event): void {
    event.preventDefault();
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

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    const element = target instanceof Element ? target : target?.parentElement;
    if (!element?.closest('.header-actions-logged-in')) {
      this.isUserMenuOpen = false;
    }

    if (!element?.closest('.header-location')) {
      this.locationDropdownOpen = false;
      this.highlightedLocationIndex = -1;
    }
  }

  private refreshSessionFromStorage(): void {
    const token = localStorage.getItem('holonix_token');
    const savedDisplayName = localStorage.getItem('holonix_display_name') ?? '';
    const savedProfileImage = localStorage.getItem('holonix_profile_image_base64') ?? '';
    this.isLoggedIn = !!token;
    this.displayName = savedDisplayName.trim() || 'User';
    const parts = this.displayName.split(' ').filter(Boolean);
    const firstInitial = parts[0]?.charAt(0) ?? 'U';
    const lastInitial = parts[1]?.charAt(0) ?? '';
    this.userInitials = `${firstInitial}${lastInitial}`.toUpperCase();
    this.profileImageDataUrl = savedProfileImage
      ? `data:image/*;base64,${savedProfileImage.trim()}`
      : '';
  }

  private applyRouteVisibility(url: string): void {
    const route = url.split('?')[0].split('#')[0];
    this.hideHeader = route === '/login' || route === '/register';
  }
}
