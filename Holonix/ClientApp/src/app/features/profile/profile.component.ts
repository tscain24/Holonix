import { Component, ElementRef, HostListener, OnDestroy, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { Observable, Subject, Subscription, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, finalize, switchMap } from 'rxjs/operators';
import { AuthService, UserSavedLocation } from '../../core/services/auth.service';
import { AuthSessionService } from '../../core/services/auth-session.service';
import { BusinessService, UserBusinessSummary } from '../../core/services/business.service';
import { AddressSuggestion, GeocodingService } from '../../core/services/geocoding.service';

interface JwtPayload {
  sub?: string;
  email?: string;
  name?: string;
  unique_name?: string;
  role?: string | string[];
  roles?: string[];
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[];
  exp?: number;
  iat?: number;
}

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css'],
})
export class ProfileComponent implements OnInit, OnDestroy {
  displayName = 'User';
  firstName = 'User';
  lastName = '';
  email = '';
  phoneNumber = '';
  userId = '';
  initials = 'U';
  isBusinessOwner = false;
  ownedBusinesses: UserBusinessSummary[] = [];
  isUserMenuOpen = false;
  profileImageDataUrl = '';
  uploadingImage = false;
  savingProfile = false;
  deletingAccount = false;
  isEditProfileMode = false;
  isDeleteAccountModalOpen = false;
  savedDateOfBirth = '';
  editableFirstName = '';
  editableLastName = '';
  editableDateOfBirth = '';
  editableDateOfBirthDisplay = '';
  editablePhoneNumber = '';
  savedLocations: UserSavedLocation[] = [];
  locationFormLabel = '';
  locationFormAddress1 = '';
  locationFormAddress2 = '';
  locationFormCity = '';
  locationFormState = '';
  locationFormZipCode = '';
  locationFormLatitude: number | null = null;
  locationFormLongitude: number | null = null;
  locationFormIsPrimary = false;
  isLocationFormOpen = false;
  editingLocationId: number | null = null;
  savingLocation = false;
  deletingLocationIds = new Set<number>();
  locationAddressSuggestions: AddressSuggestion[] = [];
  loadingLocationAddressSuggestions = false;
  locationAddressAutocompleteError = '';
  locationAddressValidationError = '';
  highlightedLocationAddressIndex = -1;
  hasLocationAddressSearchCompleted = false;
  isProfileImageModalOpen = false;
  pendingProfileImageBase64 = '';
  pendingProfileImageDataUrl = '';
  previewSize = 220;
  pendingImageNaturalWidth = 0;
  pendingImageNaturalHeight = 0;
  pendingImageRenderedWidth = 0;
  pendingImageRenderedHeight = 0;
  pendingImageOffsetX = 0;
  pendingImageOffsetY = 0;
  maxImageOffsetX = 0;
  maxImageOffsetY = 0;
  isDraggingPreviewImage = false;
  dragStartX = 0;
  dragStartY = 0;
  dragOriginX = 0;
  dragOriginY = 0;
  private readonly locationAddressSearch$ = new Subject<string>();
  private readonly locationAddressSearchSubscription: Subscription;

  constructor(
    private router: Router,
    private elementRef: ElementRef<HTMLElement>,
    private auth: AuthService,
    private businessService: BusinessService,
    private snackBar: MatSnackBar,
    private authSession: AuthSessionService,
    private geocodingService: GeocodingService
  ) {
    this.locationAddressSearchSubscription = this.locationAddressSearch$.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      switchMap((query) => this.searchLocationAddressSuggestions(query))
    ).subscribe((suggestions) => {
      this.locationAddressSuggestions = suggestions;
      this.highlightedLocationAddressIndex = suggestions.length > 0 ? 0 : -1;
    });
  }

  ngOnInit(): void {
    const token = localStorage.getItem('holonix_token');
    const savedDisplayName = (localStorage.getItem('holonix_display_name') ?? '').trim();

    if (!token) {
      this.router.navigate(['/login']);
      return;
    }

    const payload = this.decodeJwtPayload(token);
    const nameFromToken = (payload?.name ?? payload?.unique_name ?? '').trim();
    this.displayName = savedDisplayName || nameFromToken || 'User';

    const parts = this.displayName.split(' ').filter(Boolean);
    this.firstName = parts[0] ?? 'User';
    this.lastName = parts.slice(1).join(' ');
    this.initials = (this.firstName[0] ?? 'U') + (this.lastName[0] ?? '');
    this.initials = this.initials.toUpperCase();

    this.email = payload?.email ?? '';
    this.userId = payload?.sub ?? '';
    this.isBusinessOwner = this.hasOwnerRole(payload);
    this.editableFirstName = this.firstName;
    this.editableLastName = this.lastName;

    const savedProfileImage = localStorage.getItem('holonix_profile_image_base64');
    this.profileImageDataUrl = this.buildImageDataUrl(savedProfileImage);

    this.auth.getProfile().subscribe({
      next: (profile) => {
        const profileAny = profile as unknown as {
          dateOfBirth?: unknown;
          DateOfBirth?: unknown;
          phoneNumber?: unknown;
          PhoneNumber?: unknown;
        };
        const incomingDob = profileAny.dateOfBirth ?? profileAny.DateOfBirth;
        const incomingPhoneNumber = profileAny.phoneNumber ?? profileAny.PhoneNumber;
        const incomingLocations = Array.isArray((profile as { locations?: unknown }).locations)
          ? ((profile as { locations: UserSavedLocation[] }).locations ?? [])
          : [];
        const firstName = profile.firstName?.trim() || this.firstName;
        const lastName = profile.lastName?.trim() || this.lastName;
        this.firstName = firstName;
        this.lastName = lastName;
        this.phoneNumber = this.normalizeIncomingPhoneNumber(incomingPhoneNumber);
        this.displayName = `${firstName} ${lastName}`.trim();
        this.editableFirstName = firstName;
        this.editableLastName = lastName;
        this.editablePhoneNumber = this.phoneNumber;
        this.savedDateOfBirth = this.normalizeIncomingDateOfBirth(incomingDob);
        this.setEditableDateOfBirth(this.savedDateOfBirth);
        this.savedLocations = this.normalizeSavedLocations(incomingLocations);
        if (profile.profileImageBase64) {
          localStorage.setItem('holonix_profile_image_base64', profile.profileImageBase64);
          this.profileImageDataUrl = this.buildImageDataUrl(profile.profileImageBase64);
        }
        localStorage.setItem('holonix_display_name', this.displayName);
      },
      error: () => {
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        // Keep local fallback values if profile fetch fails.
      },
    });

    this.businessService.getMyBusinesses().subscribe({
      next: (businesses) => {
        this.ownedBusinesses = businesses.filter((business) => (business.roleName ?? '').toLowerCase() === 'owner');
        this.isBusinessOwner = this.ownedBusinesses.length > 0 || this.isBusinessOwner;
      },
      error: () => {
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        this.ownedBusinesses = [];
      },
    });
  }

  ngOnDestroy(): void {
    this.locationAddressSearchSubscription.unsubscribe();
  }

  goHome(): void {
    this.router.navigate(['/home']);
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
    this.authSession.logout();
  }

  openDeleteAccountModal(): void {
    if (this.deletingAccount) {
      return;
    }

    this.isDeleteAccountModalOpen = true;
  }

  closeDeleteAccountModal(): void {
    if (this.deletingAccount) {
      return;
    }

    this.isDeleteAccountModalOpen = false;
  }

  confirmDeleteAccount(): void {
    if (this.deletingAccount) {
      return;
    }

    this.deletingAccount = true;
    this.auth.deleteProfile().subscribe({
      next: () => {
        this.deletingAccount = false;
        this.isDeleteAccountModalOpen = false;
        this.authSession.clearSession();
        this.router.navigate(['/home'], {
          state: { toastMessage: 'Your account was deleted successfully.' },
        });
      },
      error: (err) => {
        this.deletingAccount = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = err?.error?.errors as string[] | undefined;
        this.snackBar.open(errors?.[0] ?? 'Failed to delete account.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  onCreateBusinessProfile(): void {
    this.router.navigate(['/business/create']);
  }

  onProfileImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const dataUrl = typeof reader.result === 'string' ? reader.result : '';
        const base64 = this.extractBase64(dataUrl);
        if (!base64) {
          this.snackBar.open('Could not read selected image.', 'Close', {
            duration: 3000,
            panelClass: ['snack-error'],
          });
          return;
        }

      this.pendingProfileImageBase64 = base64;
      this.pendingProfileImageDataUrl = this.buildImageDataUrl(base64);
      this.initializePreviewGeometry(this.pendingProfileImageDataUrl);
      this.isProfileImageModalOpen = true;
    };

    reader.readAsDataURL(file);
    input.value = '';
  }

  cancelProfileImagePreview(): void {
    if (this.uploadingImage) {
      return;
    }

    this.isProfileImageModalOpen = false;
    this.pendingProfileImageBase64 = '';
    this.pendingProfileImageDataUrl = '';
  }

  submitProfileImagePreview(): void {
    if (!this.pendingProfileImageBase64 || this.uploadingImage) {
      return;
    }

    this.uploadingImage = true;
    this.buildAdjustedProfileImageBase64(this.pendingProfileImageDataUrl)
      .then((adjustedBase64) => {
        this.auth.updateProfileImage(adjustedBase64).subscribe({
          next: (res) => {
            this.uploadingImage = false;
            const normalized = res.profileImageBase64 ?? '';
            if (normalized) {
              localStorage.setItem('holonix_profile_image_base64', normalized);
            } else {
              localStorage.removeItem('holonix_profile_image_base64');
            }
            this.profileImageDataUrl = this.buildImageDataUrl(normalized);
            this.cancelProfileImagePreview();
            this.snackBar.open('Profile picture updated.', 'Close', {
              duration: 2500,
              panelClass: ['snack-success'],
            });
          },
          error: () => {
            this.uploadingImage = false;
            if (this.authSession.hasSessionExpiredFlag()) {
              return;
            }

            this.snackBar.open('Failed to update profile picture.', 'Close', {
              duration: 3000,
              panelClass: ['snack-error'],
            });
          },
        });
      })
      .catch(() => {
        this.uploadingImage = false;
        this.snackBar.open('Could not process selected image.', 'Close', {
          duration: 3000,
          panelClass: ['snack-error'],
        });
      });
  }

  enterEditProfileMode(): void {
    this.editableFirstName = this.firstName;
    this.editableLastName = this.lastName;
    this.editablePhoneNumber = this.phoneNumber;
    this.setEditableDateOfBirth(this.savedDateOfBirth);
    this.isEditProfileMode = true;
  }

  cancelEditProfile(): void {
    this.editableFirstName = this.firstName;
    this.editableLastName = this.lastName;
    this.editablePhoneNumber = this.phoneNumber;
    this.setEditableDateOfBirth(this.savedDateOfBirth);
    this.isEditProfileMode = false;
  }

  saveProfileChanges(): void {
    const firstName = this.editableFirstName.trim();
    const lastName = this.editableLastName.trim();
    const phoneNumber = this.editablePhoneNumber.trim();

    if (!firstName || !lastName) {
      this.snackBar.open('First and last name are required.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    if (phoneNumber.length > 32) {
      this.snackBar.open('Phone number must be 32 characters or fewer.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    if (this.editableDateOfBirthDisplay && !this.editableDateOfBirth) {
      this.snackBar.open('Enter a valid birthday.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    this.savingProfile = true;
    this.auth.updateProfile({
      firstName,
      lastName,
      dateOfBirth: this.editableDateOfBirth || null,
      phoneNumber: phoneNumber || null,
    }).subscribe({
      next: (profile) => {
        const profileAny = profile as unknown as {
          dateOfBirth?: unknown;
          DateOfBirth?: unknown;
          phoneNumber?: unknown;
          PhoneNumber?: unknown;
        };
        const incomingDob = profileAny.dateOfBirth ?? profileAny.DateOfBirth;
        const incomingPhoneNumber = profileAny.phoneNumber ?? profileAny.PhoneNumber;
        const incomingLocations = Array.isArray((profile as { locations?: unknown }).locations)
          ? ((profile as { locations: UserSavedLocation[] }).locations ?? [])
          : [];
        this.savingProfile = false;
        this.isEditProfileMode = false;
        this.firstName = profile.firstName.trim();
        this.lastName = profile.lastName.trim();
        this.phoneNumber = this.normalizeIncomingPhoneNumber(incomingPhoneNumber);
        this.displayName = `${this.firstName} ${this.lastName}`.trim();
        this.editableFirstName = this.firstName;
        this.editableLastName = this.lastName;
        this.editablePhoneNumber = this.phoneNumber;
        this.savedDateOfBirth = this.normalizeIncomingDateOfBirth(incomingDob);
        this.setEditableDateOfBirth(this.savedDateOfBirth);
        this.savedLocations = this.normalizeSavedLocations(incomingLocations);
        localStorage.setItem('holonix_display_name', this.displayName);
        this.initials = `${this.firstName.charAt(0)}${this.lastName.charAt(0)}`.toUpperCase();
        this.snackBar.open('Profile updated.', 'Close', {
          duration: 2500,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.savingProfile = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = err?.error?.errors as string[] | undefined;
        this.snackBar.open(errors?.[0] ?? 'Failed to update profile.', 'Close', {
          duration: 3000,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  onDateOfBirthInput(event: Event): void {
    if (!this.isEditProfileMode) {
      const input = event.target as HTMLInputElement;
      input.value = this.editableDateOfBirthDisplay;
      return;
    }

    const input = event.target as HTMLInputElement;
    const raw = input.value;
    const selectionStart = input.selectionStart ?? raw.length;
    const digitsBeforeCursor = raw.slice(0, selectionStart).replace(/\D/g, '').length;
    const digits = raw.replace(/\D/g, '').slice(0, 8);

    let formatted = '';
    if (digits.length <= 2) {
      formatted = digits;
    } else if (digits.length <= 4) {
      formatted = `${digits.slice(0, 2)}/${digits.slice(2)}`;
    } else {
      formatted = `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}`;
    }

    if (formatted !== raw) {
      input.value = formatted;
      let caret = digitsBeforeCursor;
      if (digitsBeforeCursor > 2 && digitsBeforeCursor <= 4) {
        caret += 1;
      } else if (digitsBeforeCursor > 4) {
        caret += 2;
      }
      input.setSelectionRange(caret, caret);
    }

    this.editableDateOfBirthDisplay = formatted;
    this.editableDateOfBirth = this.parseDateOfBirthDisplay(formatted);
  }

  onFirstNameInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!this.isEditProfileMode) {
      input.value = this.editableFirstName;
      return;
    }

    this.editableFirstName = input.value;
  }

  onLastNameInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!this.isEditProfileMode) {
      input.value = this.editableLastName;
      return;
    }

    this.editableLastName = input.value;
  }

  onPhoneNumberInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!this.isEditProfileMode) {
      input.value = this.editablePhoneNumber;
      return;
    }

    const raw = input.value;
    const digits = raw.replace(/\D/g, '').slice(0, 11);
    const formatted = this.formatPhoneNumberValue(digits, raw);
    input.value = formatted;
    input.setSelectionRange(formatted.length, formatted.length);
    this.editablePhoneNumber = formatted;
  }

  get canSaveLocation(): boolean {
    return !this.savingLocation
      && this.locationFormLabel.trim().length > 0
      && this.locationFormAddress1.trim().length > 0
      && this.locationFormCity.trim().length > 0
      && this.locationFormState.trim().length > 0
      && this.locationFormZipCode.trim().length > 0;
  }

  startAddLocation(): void {
    this.resetLocationForm();
    this.isLocationFormOpen = true;
  }

  startEditLocation(location: UserSavedLocation): void {
    this.isLocationFormOpen = true;
    this.editingLocationId = location.userSavedLocationId;
    this.locationFormLabel = location.label;
    this.locationFormAddress1 = location.address1;
    this.locationFormAddress2 = location.address2 ?? '';
    this.locationFormCity = location.city;
    this.locationFormState = location.state;
    this.locationFormZipCode = location.zipCode;
    this.locationFormLatitude = this.normalizeCoordinate(location.latitude);
    this.locationFormLongitude = this.normalizeCoordinate(location.longitude);
    this.locationFormIsPrimary = location.isPrimary;
    this.clearLocationAddressSuggestions();
    this.locationAddressValidationError = '';
  }

  cancelLocationEdit(): void {
    if (this.savingLocation) {
      return;
    }

    this.resetLocationForm();
    this.isLocationFormOpen = false;
  }

  saveLocation(): void {
    const label = this.locationFormLabel.trim();
    const address1 = this.locationFormAddress1.trim();
    const address2 = this.locationFormAddress2.trim();
    const city = this.locationFormCity.trim();
    const state = this.locationFormState.trim();
    const zipCode = this.locationFormZipCode.trim();

    if (!label || !address1 || !city || !state || !zipCode) {
      this.snackBar.open('Location label, address, city, state, and zip code are required.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    this.locationAddressValidationError = '';
    this.savingLocation = true;

    if (this.locationFormLatitude !== null && this.locationFormLongitude !== null) {
      this.persistLocation(label, address1, address2, city, state, zipCode);
      return;
    }

    this.geocodingService.resolveAddress({
      address1,
      address2: address2 || null,
      city,
      state,
      zipCode,
      countryCode: 'US',
    }).subscribe({
      next: (suggestion) => {
        if (!suggestion) {
          this.savingLocation = false;
          this.locationAddressValidationError = 'Select a valid address from the suggestions before saving.';
          this.snackBar.open('Select a valid address before saving.', 'Close', {
            duration: 3000,
            panelClass: ['snack-error'],
          });
          return;
        }

        this.applyLocationAddressSuggestion(suggestion);
        this.persistLocation(
          label,
          suggestion.address1.trim(),
          address2,
          (suggestion.city ?? city).trim(),
          (suggestion.state ?? state).trim(),
          (suggestion.zipCode ?? zipCode).trim()
        );
      },
      error: () => {
        this.savingLocation = false;
        this.locationAddressValidationError = 'Address validation is unavailable. Try again before saving.';
        this.snackBar.open('Could not validate the address.', 'Close', {
          duration: 3000,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  onLocationAddressInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.locationFormAddress1 = input.value;
    this.invalidateLocationAddress();
    this.locationAddressSearch$.next(input.value);
  }

  onLocationAddressFocus(): void {
    if (this.locationFormAddress1.trim().length >= 3) {
      this.locationAddressSearch$.next(this.locationFormAddress1);
    }
  }

  onLocationAddressKeydown(event: KeyboardEvent): void {
    if (this.locationAddressSuggestions.length === 0) {
      return;
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.highlightedLocationAddressIndex = (this.highlightedLocationAddressIndex + 1) % this.locationAddressSuggestions.length;
      return;
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.highlightedLocationAddressIndex = this.highlightedLocationAddressIndex <= 0
        ? this.locationAddressSuggestions.length - 1
        : this.highlightedLocationAddressIndex - 1;
      return;
    }

    if (event.key === 'Enter' && this.highlightedLocationAddressIndex >= 0) {
      event.preventDefault();
      this.selectLocationAddressSuggestion(this.locationAddressSuggestions[this.highlightedLocationAddressIndex]);
      return;
    }

    if (event.key === 'Escape') {
      this.clearLocationAddressSuggestions();
    }
  }

  selectLocationAddressSuggestion(suggestion: AddressSuggestion): void {
    this.applyLocationAddressSuggestion(suggestion);
    this.clearLocationAddressSuggestions();
  }

  onLocationCityInput(event: Event): void {
    this.locationFormCity = (event.target as HTMLInputElement).value;
    this.invalidateLocationAddress();
  }

  onLocationStateInput(event: Event): void {
    this.locationFormState = (event.target as HTMLInputElement).value;
    this.invalidateLocationAddress();
  }

  onLocationZipCodeInput(event: Event): void {
    this.locationFormZipCode = (event.target as HTMLInputElement).value;
    this.invalidateLocationAddress();
  }

  formatLocationAddressSuggestion(suggestion: AddressSuggestion): string {
    return suggestion.fullAddress
      ?? [suggestion.address1, suggestion.city, suggestion.state, suggestion.zipCode].filter(Boolean).join(', ');
  }

  getLocationAddressNoResultsMessage(): string {
    const query = this.locationFormAddress1.trim();
    if (/^\d+$/.test(query)) {
      return 'Keep typing the street name to find a full address, for example 303 Main.';
    }

    return 'No matching full addresses found. Check the street address or keep typing.';
  }

  get showLocationAddressNoResults(): boolean {
    return this.hasLocationAddressSearchCompleted
      && !this.loadingLocationAddressSuggestions
      && !this.locationAddressAutocompleteError
      && this.locationAddressSuggestions.length === 0
      && this.locationFormAddress1.trim().length >= 3;
  }

  private persistLocation(
    label: string,
    address1: string,
    address2: string,
    city: string,
    state: string,
    zipCode: string
  ): void {
    const wasEditing = this.editingLocationId !== null;
    const payload = {
      label,
      address1,
      address2: address2 || null,
      city,
      state,
      zipCode,
      latitude: this.locationFormLatitude,
      longitude: this.locationFormLongitude,
      isPrimary: this.locationFormIsPrimary,
    };

    const request = this.editingLocationId === null
      ? this.auth.createSavedLocation(payload)
      : this.auth.updateSavedLocation(this.editingLocationId, payload);

    request.subscribe({
      next: (location) => {
        this.savingLocation = false;
        const nextLocations = this.editingLocationId === null
          ? [...this.savedLocations, location]
          : this.savedLocations.map((item) => item.userSavedLocationId === location.userSavedLocationId ? location : item);
        this.savedLocations = this.normalizeSavedLocations(nextLocations);
        this.resetLocationForm();
        this.isLocationFormOpen = false;
        this.snackBar.open(wasEditing ? 'Location updated.' : 'Location saved.', 'Close', {
          duration: 2500,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.savingLocation = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = err?.error?.errors as string[] | undefined;
        this.snackBar.open(errors?.[0] ?? 'Failed to save location.', 'Close', {
          duration: 3000,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  deleteLocation(location: UserSavedLocation): void {
    if (this.deletingLocationIds.has(location.userSavedLocationId)) {
      return;
    }

    this.deletingLocationIds.add(location.userSavedLocationId);
    this.auth.deleteSavedLocation(location.userSavedLocationId).subscribe({
      next: () => {
        this.deletingLocationIds.delete(location.userSavedLocationId);
        this.savedLocations = this.savedLocations.filter((item) => item.userSavedLocationId !== location.userSavedLocationId);
        this.savedLocations = this.normalizeSavedLocations(this.savedLocations);
        if (this.editingLocationId === location.userSavedLocationId) {
          this.resetLocationForm();
          this.isLocationFormOpen = false;
        }
        this.snackBar.open('Location removed.', 'Close', {
          duration: 2500,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.deletingLocationIds.delete(location.userSavedLocationId);
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = err?.error?.errors as string[] | undefined;
        this.snackBar.open(errors?.[0] ?? 'Failed to remove location.', 'Close', {
          duration: 3000,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  isDeletingLocation(userSavedLocationId: number): boolean {
    return this.deletingLocationIds.has(userSavedLocationId);
  }

  formatSavedLocationAddress(location: UserSavedLocation): string {
    return [location.address1, location.address2, `${location.city}, ${location.state} ${location.zipCode}`]
      .filter((value) => !!value && value.trim().length > 0)
      .join(', ');
  }

  startPreviewImageDrag(event: MouseEvent | TouchEvent): void {
    if (this.uploadingImage) {
      return;
    }

    event.preventDefault();
    const point = this.getPointer(event);
    this.isDraggingPreviewImage = true;
    this.dragStartX = point.x;
    this.dragStartY = point.y;
    this.dragOriginX = this.pendingImageOffsetX;
    this.dragOriginY = this.pendingImageOffsetY;
  }

  onPreviewImageDrag(event: MouseEvent | TouchEvent): void {
    if (!this.isDraggingPreviewImage) {
      return;
    }

    event.preventDefault();
    const point = this.getPointer(event);
    const deltaX = point.x - this.dragStartX;
    const deltaY = point.y - this.dragStartY;

    this.pendingImageOffsetX = this.clamp(deltaX + this.dragOriginX, -this.maxImageOffsetX, this.maxImageOffsetX);
    this.pendingImageOffsetY = this.clamp(deltaY + this.dragOriginY, -this.maxImageOffsetY, this.maxImageOffsetY);
  }

  endPreviewImageDrag(): void {
    this.isDraggingPreviewImage = false;
  }

  @HostListener('document:mouseup')
  onGlobalMouseUp(): void {
    this.endPreviewImageDrag();
  }

  @HostListener('document:touchend')
  onGlobalTouchEnd(): void {
    this.endPreviewImageDrag();
  }

  @HostListener('document:touchcancel')
  onGlobalTouchCancel(): void {
    this.endPreviewImageDrag();
  }

  getPreviewImageTransform(): string {
    return `translate(-50%, -50%) translate(${this.pendingImageOffsetX}px, ${this.pendingImageOffsetY}px)`;
  }

  getPreviewHint(): string {
    return this.isDraggingPreviewImage
      ? 'Release to set position'
      : 'Drag image to reposition';
  }

  private initializePreviewGeometry(dataUrl: string): void {
    const image = new Image();
    image.onload = () => {
      this.pendingImageNaturalWidth = image.naturalWidth;
      this.pendingImageNaturalHeight = image.naturalHeight;

      const baseScale = Math.max(
        this.previewSize / this.pendingImageNaturalWidth,
        this.previewSize / this.pendingImageNaturalHeight
      );

      this.pendingImageRenderedWidth = this.pendingImageNaturalWidth * baseScale;
      this.pendingImageRenderedHeight = this.pendingImageNaturalHeight * baseScale;
      this.maxImageOffsetX = Math.max(0, (this.pendingImageRenderedWidth - this.previewSize) / 2);
      this.maxImageOffsetY = Math.max(0, (this.pendingImageRenderedHeight - this.previewSize) / 2);
      this.pendingImageOffsetX = 0;
      this.pendingImageOffsetY = 0;
    };
    image.src = dataUrl;
  }

  private async buildAdjustedProfileImageBase64(dataUrl: string): Promise<string> {
    const sourceImage = await this.loadImage(dataUrl);
    const outputSize = 256;
    const canvas = document.createElement('canvas');
    canvas.width = outputSize;
    canvas.height = outputSize;
    const context = canvas.getContext('2d');

    if (!context) {
      throw new Error('Canvas unavailable');
    }

    const drawScale = outputSize / this.previewSize;
    const drawWidth = this.pendingImageRenderedWidth * drawScale;
    const drawHeight = this.pendingImageRenderedHeight * drawScale;
    const drawX = ((this.previewSize - this.pendingImageRenderedWidth) / 2 + this.pendingImageOffsetX) * drawScale;
    const drawY = ((this.previewSize - this.pendingImageRenderedHeight) / 2 + this.pendingImageOffsetY) * drawScale;

    context.clearRect(0, 0, outputSize, outputSize);
    context.drawImage(sourceImage, drawX, drawY, drawWidth, drawHeight);

    const outputDataUrl = canvas.toDataURL('image/png');
    const outputBase64 = this.extractBase64(outputDataUrl);
    if (!outputBase64) {
      throw new Error('Image encoding failed');
    }

    return outputBase64;
  }

  private loadImage(dataUrl: string): Promise<HTMLImageElement> {
    return new Promise((resolve, reject) => {
      const image = new Image();
      image.onload = () => resolve(image);
      image.onerror = () => reject(new Error('Image load failed'));
      image.src = dataUrl;
    });
  }

  private getPointer(event: MouseEvent | TouchEvent): { x: number; y: number } {
    if ('touches' in event) {
      const touch = event.touches[0] ?? event.changedTouches[0];
      return { x: touch?.clientX ?? 0, y: touch?.clientY ?? 0 };
    }

    return { x: event.clientX, y: event.clientY };
  }

  private clamp(value: number, min: number, max: number): number {
    return Math.min(Math.max(value, min), max);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    const element = target instanceof Element ? target : target?.parentElement;
    if (!element?.closest('.auth-actions-logged-in')) {
      this.isUserMenuOpen = false;
    }
    if (!element?.closest('.saved-location-address-shell')) {
      this.clearLocationAddressSuggestions();
    }
  }

  private decodeJwtPayload(token: string): JwtPayload | null {
    try {
      const tokenParts = token.split('.');
      if (tokenParts.length < 2) {
        return null;
      }

      const base64 = tokenParts[1].replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
      const json = atob(padded);
      return JSON.parse(json) as JwtPayload;
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

  private extractBase64(dataUrl: string): string {
    const marker = 'base64,';
    const markerIndex = dataUrl.indexOf(marker);
    if (markerIndex < 0) {
      return '';
    }

    return dataUrl.slice(markerIndex + marker.length).trim();
  }

  private buildImageDataUrl(base64: string | null): string {
    if (!base64 || !base64.trim()) {
      return '';
    }

    return `data:image/*;base64,${base64.trim()}`;
  }

  private setEditableDateOfBirth(isoValue: string): void {
    this.editableDateOfBirth = isoValue;
    this.editableDateOfBirthDisplay = this.formatDateOfBirthDisplay(isoValue);
  }

  private formatDateOfBirthDisplay(isoValue: string): string {
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(isoValue);
    if (!match) {
      return '';
    }

    return `${match[2]}/${match[3]}/${match[1]}`;
  }

  private parseDateOfBirthDisplay(displayValue: string): string {
    const trimmed = displayValue.trim();
    if (!trimmed) {
      return '';
    }

    const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(trimmed);
    if (!match) {
      return '';
    }

    const month = Number(match[1]);
    const day = Number(match[2]);
    const year = Number(match[3]);

    const candidate = new Date(year, month - 1, day);
    if (
      Number.isNaN(candidate.getTime()) ||
      candidate.getFullYear() !== year ||
      candidate.getMonth() !== month - 1 ||
      candidate.getDate() !== day
    ) {
      return '';
    }

    const today = new Date();
    if (candidate > today) {
      return '';
    }

    const minAgeCutoff = new Date(today.getFullYear() - 13, today.getMonth(), today.getDate());
    if (candidate > minAgeCutoff) {
      return '';
    }

    const monthPart = String(month).padStart(2, '0');
    const dayPart = String(day).padStart(2, '0');
    return `${year}-${monthPart}-${dayPart}`;
  }

  private normalizeIncomingDateOfBirth(value: unknown): string {
    if (typeof value !== 'string') {
      return '';
    }

    const raw = value.trim();
    if (!raw) {
      return '';
    }

    const isoMatch = /^(\d{4})-(\d{1,2})-(\d{1,2})/.exec(raw);
    if (isoMatch) {
      const year = Number(isoMatch[1]);
      const month = Number(isoMatch[2]);
      const day = Number(isoMatch[3]);
      if (this.isValidCalendarDate(year, month, day)) {
        return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
      }
    }

    const slashMatch = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/.exec(raw);
    if (slashMatch) {
      const month = Number(slashMatch[1]);
      const day = Number(slashMatch[2]);
      const year = Number(slashMatch[3]);
      if (this.isValidCalendarDate(year, month, day)) {
        return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
      }
    }

    return '';
  }

  private normalizeIncomingPhoneNumber(value: unknown): string {
    return typeof value === 'string' ? this.formatPhoneNumberForDisplay(value) : '';
  }

  formatPhoneNumberForDisplay(value: string): string {
    const trimmed = value.trim();
    if (!trimmed) {
      return '';
    }

    const digits = trimmed.replace(/\D/g, '');
    return this.formatPhoneNumberValue(digits, trimmed);
  }

  private formatPhoneNumberValue(digitsOnly: string, fallback: string): string {
    if (!digitsOnly) {
      return '';
    }

    if (digitsOnly.length <= 3) {
      return digitsOnly;
    }

    if (digitsOnly.length <= 6) {
      return `(${digitsOnly.slice(0, 3)}) ${digitsOnly.slice(3)}`;
    }

    if (digitsOnly.length <= 10) {
      return `(${digitsOnly.slice(0, 3)}) ${digitsOnly.slice(3, 6)}-${digitsOnly.slice(6)}`;
    }

    if (digitsOnly.length === 11 && digitsOnly.startsWith('1')) {
      return `+1 (${digitsOnly.slice(1, 4)}) ${digitsOnly.slice(4, 7)}-${digitsOnly.slice(7)}`;
    }

    return fallback.trim();
  }

  private isValidCalendarDate(year: number, month: number, day: number): boolean {
    const candidate = new Date(year, month - 1, day);
    return (
      !Number.isNaN(candidate.getTime()) &&
      candidate.getFullYear() === year &&
      candidate.getMonth() === month - 1 &&
      candidate.getDate() === day
    );
  }

  private resetLocationForm(): void {
    this.editingLocationId = null;
    this.locationFormLabel = '';
    this.locationFormAddress1 = '';
    this.locationFormAddress2 = '';
    this.locationFormCity = '';
    this.locationFormState = '';
    this.locationFormZipCode = '';
    this.locationFormLatitude = null;
    this.locationFormLongitude = null;
    this.locationFormIsPrimary = this.savedLocations.length === 0;
    this.clearLocationAddressSuggestions();
    this.locationAddressValidationError = '';
  }

  private searchLocationAddressSuggestions(query: string): Observable<AddressSuggestion[]> {
    const trimmed = query.trim();
    if (trimmed.length < 3) {
      this.loadingLocationAddressSuggestions = false;
      this.locationAddressAutocompleteError = '';
      this.hasLocationAddressSearchCompleted = false;
      return of([]);
    }

    const contextualQuery = [
      trimmed,
      this.locationFormCity.trim(),
      this.locationFormState.trim(),
      this.locationFormZipCode.trim(),
    ].filter(Boolean).join(', ');

    this.loadingLocationAddressSuggestions = true;
    this.locationAddressAutocompleteError = '';

    return this.geocodingService.getAddressSuggestions(contextualQuery, 'US').pipe(
      catchError(() => {
        this.locationAddressAutocompleteError = 'Address suggestions are unavailable right now.';
        return of([]);
      }),
      finalize(() => {
        this.loadingLocationAddressSuggestions = false;
        this.hasLocationAddressSearchCompleted = true;
      })
    );
  }

  private applyLocationAddressSuggestion(suggestion: AddressSuggestion): void {
    this.locationFormAddress1 = suggestion.address1;
    this.locationFormCity = suggestion.city ?? this.locationFormCity;
    this.locationFormState = suggestion.state ?? this.locationFormState;
    this.locationFormZipCode = suggestion.zipCode ?? this.locationFormZipCode;
    this.locationFormLatitude = suggestion.latitude;
    this.locationFormLongitude = suggestion.longitude;
    this.locationAddressValidationError = '';
  }

  private invalidateLocationAddress(): void {
    this.locationFormLatitude = null;
    this.locationFormLongitude = null;
    this.locationAddressValidationError = '';
  }

  private clearLocationAddressSuggestions(): void {
    this.locationAddressSuggestions = [];
    this.highlightedLocationAddressIndex = -1;
    this.hasLocationAddressSearchCompleted = false;
  }

  private normalizeCoordinate(value: number | null | undefined): number | null {
    return typeof value === 'number' && Number.isFinite(value) ? value : null;
  }

  private normalizeSavedLocations(locations: UserSavedLocation[]): UserSavedLocation[] {
    return [...locations].sort((a, b) => {
      if (a.isPrimary !== b.isPrimary) {
        return a.isPrimary ? -1 : 1;
      }

      return a.label.localeCompare(b.label);
    });
  }
}
