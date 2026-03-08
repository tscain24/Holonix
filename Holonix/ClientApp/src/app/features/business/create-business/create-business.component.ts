import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { BusinessService, BusinessServiceOption, CountryOption } from '../../../core/services/business.service';

@Component({
  selector: 'app-create-business',
  templateUrl: './create-business.component.html',
  styleUrls: ['./create-business.component.css'],
})
export class CreateBusinessComponent implements OnInit {
  private static readonly DropdownPreferredHeight = 288;

  currentStep: 1 | 2 | 3 = 1;
  submitAttempted = false;
  serviceSearch = '';
  allServices: BusinessServiceOption[] = [];
  selectedServices: BusinessServiceOption[] = [];
  serviceDropdownOpen = false;
  serviceDropdownDirection: 'down' | 'up' = 'down';
  highlightedServiceIndex = -1;
  loadingServices = false;
  countrySearch = '';
  allCountries: CountryOption[] = [];
  countryDropdownOpen = false;
  countryDropdownDirection: 'down' | 'up' = 'down';
  highlightedCountryIndex = -1;
  loadingCountries = false;
  iconPreview: string | null = null;
  showLeaveConfirmation = false;

  businessForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(1000)]],
    address1: ['', [Validators.required, Validators.maxLength(200)]],
    address2: ['', [Validators.maxLength(200)]],
    city: ['', [Validators.required, Validators.maxLength(120)]],
    state: ['', [Validators.required, Validators.maxLength(120)]],
    zipCode: ['', [Validators.required, Validators.maxLength(32)]],
    countryId: [null as number | null, [Validators.required]],
    businessIconBase64: [''],
    businessJobPercentage: [100, [Validators.required, Validators.min(0), Validators.max(100)]],
    isSingleService: [true, [Validators.required]],
    isRecurring: [false, [Validators.required]],
  });

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
    private readonly businessService: BusinessService
  ) {}

  ngOnInit(): void {
    if (!localStorage.getItem('holonix_token')) {
      this.router.navigate(['/login']);
      return;
    }

    this.loadingServices = true;
    this.businessService.getServices().subscribe({
      next: (services) => {
        this.allServices = services;
        this.loadingServices = false;
      },
      error: () => {
        this.loadingServices = false;
        this.snackBar.open('Could not load business services.', 'Close', {
          duration: 3000,
          panelClass: ['snack-error'],
        });
      },
    });

    this.loadingCountries = true;
    this.businessService.getCountries().subscribe({
      next: (countries) => {
        this.allCountries = countries;
        this.loadingCountries = false;

        const defaultCountry = countries.find((country) => country.twoLetterIsoCode === 'US') ?? countries[0];
        if (defaultCountry) {
          this.selectCountry(defaultCountry, false);
        }
      },
      error: () => {
        this.loadingCountries = false;
        this.snackBar.open('Could not load countries.', 'Close', {
          duration: 3000,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  goBack(): void {
    if (this.currentStep > 1) {
      this.currentStep = (this.currentStep - 1) as 1 | 2 | 3;
      return;
    }

    if (this.hasUnsavedBusinessData) {
      this.showLeaveConfirmation = true;
      return;
    }

    this.router.navigate(['/profile']);
  }

  cancelLeaveConfirmation(): void {
    this.showLeaveConfirmation = false;
  }

  confirmLeave(): void {
    this.showLeaveConfirmation = false;
    this.router.navigate(['/profile']);
  }

  get currentStepTitle(): string {
    switch (this.currentStep) {
      case 1:
        return 'Business Details';
      case 2:
        return 'Business Setup';
      case 3:
        return 'Review and Launch';
    }
  }

  get currentStepDescription(): string {
    switch (this.currentStep) {
      case 1:
        return 'Enter the basics for your business profile. We will use this address for future geocoding.';
      case 2:
        return 'Set up how your business operates and choose the identity details customers will see first.';
      case 3:
        return 'Review your business profile before the backend save flow is wired in.';
    }
  }

  get selectedCountryName(): string {
    const countryId = this.businessForm.controls.countryId.value;
    return this.allCountries.find((country) => country.countryId === countryId)?.name ?? 'Not selected';
  }

  get selectedServicesSummary(): string {
    return this.selectedServices.length > 0
      ? this.selectedServices.map((service) => service.name).join(', ')
      : 'None selected';
  }

  get addressSummary(): string {
    const parts = [
      this.businessForm.controls.address1.value,
      this.businessForm.controls.address2.value,
      this.businessForm.controls.city.value,
      this.businessForm.controls.state.value,
      this.businessForm.controls.zipCode.value,
    ].filter((value): value is string => !!value && value.trim().length > 0);

    return parts.length > 0 ? parts.join(', ') : 'Not provided';
  }

  get hasUnsavedBusinessData(): boolean {
    return this.businessForm.dirty || this.selectedServices.length > 0;
  }

  get filteredServices(): BusinessServiceOption[] {
    const query = this.serviceSearch.trim().toLowerCase();
    return this.allServices
      .filter((service) => !this.selectedServices.some((selected) => selected.serviceId === service.serviceId))
      .filter((service) => !query || service.name.toLowerCase().includes(query))
      .slice(0, 10);
  }

  get filteredCountries(): CountryOption[] {
    const query = this.normalizeSearchValue(this.countrySearch);
    return this.allCountries
      .filter((country) =>
        !query ||
        this.normalizeSearchValue(country.name).includes(query) ||
        country.twoLetterIsoCode.toLowerCase().includes(query)
      )
      .slice(0, 10);
  }

  onServiceSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.serviceSearch = input.value;
    this.serviceDropdownDirection = this.getDropdownDirection(input);
    this.serviceDropdownOpen = true;
    this.highlightedServiceIndex = this.filteredServices.length > 0 ? 0 : -1;
  }

  openServiceDropdown(input: HTMLInputElement): void {
    this.serviceDropdownDirection = this.getDropdownDirection(input);
    this.serviceDropdownOpen = true;
    this.highlightedServiceIndex = this.filteredServices.length > 0 ? 0 : -1;
  }

  closeServiceDropdown(): void {
    window.setTimeout(() => {
      this.serviceDropdownOpen = false;
      this.highlightedServiceIndex = -1;
    }, 120);
  }

  onServiceSearchKeydown(event: KeyboardEvent, input: HTMLInputElement): void {
    const options = this.filteredServices;
    if (options.length === 0 && event.key !== 'Escape') {
      return;
    }

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.serviceDropdownDirection = this.getDropdownDirection(input);
        this.serviceDropdownOpen = true;
        this.highlightedServiceIndex = options.length === 0
          ? -1
          : (this.highlightedServiceIndex + 1 + options.length) % options.length;
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.serviceDropdownDirection = this.getDropdownDirection(input);
        this.serviceDropdownOpen = true;
        this.highlightedServiceIndex = options.length === 0
          ? -1
          : (this.highlightedServiceIndex - 1 + options.length) % options.length;
        break;
      case 'Enter':
        if (!this.serviceDropdownOpen || this.highlightedServiceIndex < 0 || this.highlightedServiceIndex >= options.length) {
          return;
        }

        event.preventDefault();
        this.selectService(options[this.highlightedServiceIndex]);
        break;
      case 'Escape':
        this.serviceDropdownOpen = false;
        this.highlightedServiceIndex = -1;
        break;
    }
  }

  selectService(service: BusinessServiceOption): void {
    if (this.selectedServices.some((selected) => selected.serviceId === service.serviceId)) {
      return;
    }

    this.selectedServices = [...this.selectedServices, service];
    this.serviceSearch = '';
    this.serviceDropdownOpen = false;
    this.highlightedServiceIndex = -1;
  }

  removeService(serviceId: number): void {
    this.selectedServices = this.selectedServices.filter((service) => service.serviceId !== serviceId);
  }

  onCountrySearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.countrySearch = input.value;
    this.countryDropdownDirection = this.getDropdownDirection(input);
    this.countryDropdownOpen = true;
    this.highlightedCountryIndex = this.filteredCountries.length > 0 ? 0 : -1;

    const selectedCountryId = this.businessForm.controls.countryId.value;
    const selectedCountry = this.allCountries.find((country) => country.countryId === selectedCountryId);
    if (!selectedCountry || !this.isCountryMatch(selectedCountry, this.countrySearch)) {
      this.businessForm.controls.countryId.setValue(null);
    }
  }

  openCountryDropdown(input: HTMLInputElement): void {
    this.countryDropdownDirection = this.getDropdownDirection(input);
    this.countryDropdownOpen = true;
    this.highlightedCountryIndex = this.filteredCountries.length > 0 ? 0 : -1;
  }

  closeCountryDropdown(): void {
    window.setTimeout(() => {
      this.commitCountrySelection();
      this.countryDropdownOpen = false;
      this.highlightedCountryIndex = -1;
    }, 120);
  }

  onCountrySearchKeydown(event: KeyboardEvent, input: HTMLInputElement): void {
    const options = this.filteredCountries;
    if (options.length === 0 && event.key !== 'Escape') {
      return;
    }

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.countryDropdownDirection = this.getDropdownDirection(input);
        this.countryDropdownOpen = true;
        this.highlightedCountryIndex = options.length === 0
          ? -1
          : (this.highlightedCountryIndex + 1 + options.length) % options.length;
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.countryDropdownDirection = this.getDropdownDirection(input);
        this.countryDropdownOpen = true;
        this.highlightedCountryIndex = options.length === 0
          ? -1
          : (this.highlightedCountryIndex - 1 + options.length) % options.length;
        break;
      case 'Enter':
        if (!this.countryDropdownOpen || this.highlightedCountryIndex < 0 || this.highlightedCountryIndex >= options.length) {
          return;
        }

        event.preventDefault();
        this.selectCountry(options[this.highlightedCountryIndex]);
        break;
      case 'Escape':
        this.countryDropdownOpen = false;
        this.highlightedCountryIndex = -1;
        break;
    }
  }

  selectCountry(country: CountryOption, markAsDirty = true): void {
    this.countrySearch = country.name;
    this.businessForm.controls.countryId.setValue(country.countryId);
    if (markAsDirty) {
      this.businessForm.controls.countryId.markAsDirty();
    }
    this.countryDropdownOpen = false;
    this.highlightedCountryIndex = -1;
  }

  onBusinessIconSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const result = typeof reader.result === 'string' ? reader.result : '';
      const normalized = this.normalizeBase64Image(result);
      this.businessForm.controls.businessIconBase64.setValue(normalized);
      this.iconPreview = result;
    };

    reader.readAsDataURL(file);
  }

  removeBusinessIcon(): void {
    this.businessForm.controls.businessIconBase64.setValue('');
    this.iconPreview = null;
  }

  goToStepTwo(): void {
    this.submitAttempted = true;
    this.commitCountrySelection();

    if (this.businessForm.invalid) {
      this.businessForm.markAllAsTouched();
      return;
    }

    if (this.selectedServices.length === 0) {
      this.serviceDropdownOpen = true;
      this.snackBar.open('Select at least one service to continue.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    this.currentStep = 2;
    this.submitAttempted = false;
  }

  goToStepThree(): void {
    this.submitAttempted = true;

    if (
      this.businessForm.controls.businessJobPercentage.invalid ||
      this.businessForm.controls.isSingleService.invalid ||
      this.businessForm.controls.isRecurring.invalid
    ) {
      this.businessForm.controls.businessJobPercentage.markAsTouched();
      return;
    }

    this.currentStep = 3;
    this.submitAttempted = false;
  }

  editStep(step: 1 | 2): void {
    this.currentStep = step;
  }

  submit(): void {
    this.snackBar.open('Business creation flow is ready for backend wiring.', 'Close', {
      duration: 3000,
      panelClass: ['snack-success'],
    });
  }

  private commitCountrySelection(): void {
    const normalizedSearch = this.normalizeSearchValue(this.countrySearch);
    if (!normalizedSearch) {
      this.businessForm.controls.countryId.setValue(null);
      return;
    }

    const exactMatch = this.allCountries.find((country) => this.isCountryMatch(country, normalizedSearch));
    if (exactMatch) {
      this.countrySearch = exactMatch.name;
      this.businessForm.controls.countryId.setValue(exactMatch.countryId);
      return;
    }

    this.businessForm.controls.countryId.setValue(null);
  }

  private isCountryMatch(country: CountryOption, value: string): boolean {
    const normalizedValue = this.normalizeSearchValue(value);
    return this.normalizeSearchValue(country.name) === normalizedValue || country.twoLetterIsoCode.toLowerCase() === normalizedValue;
  }

  private normalizeSearchValue(value: string): string {
    return value
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
  }

  private getDropdownDirection(input: HTMLInputElement): 'down' | 'up' {
    const rect = input.getBoundingClientRect();
    const spaceBelow = window.innerHeight - rect.bottom;
    const spaceAbove = rect.top;

    if (
      spaceBelow < CreateBusinessComponent.DropdownPreferredHeight &&
      spaceAbove > spaceBelow
    ) {
      return 'up';
    }

    return 'down';
  }

  private normalizeBase64Image(value: string): string {
    const marker = 'base64,';
    const markerIndex = value.indexOf(marker);
    return markerIndex >= 0 ? value.slice(markerIndex + marker.length) : value;
  }
}
