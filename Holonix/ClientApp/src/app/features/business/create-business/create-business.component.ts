import { AfterViewChecked, Component, ElementRef, HostListener, OnInit, QueryList, ViewChildren } from '@angular/core';
import { FormBuilder, FormControl, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { BusinessService, BusinessServiceOption, CountryOption } from '../../../core/services/business.service';
import { AuthSessionService } from '../../../core/services/auth-session.service';

type CreateBusinessForm = {
  name: FormControl<string | null>;
  description: FormControl<string | null>;
  businessEmail: FormControl<string | null>;
  businessPhoneNumber: FormControl<string | null>;
  address1: FormControl<string | null>;
  address2: FormControl<string | null>;
  city: FormControl<string | null>;
  state: FormControl<string | null>;
  zipCode: FormControl<string | null>;
  countryId: FormControl<number | null>;
  businessIconBase64: FormControl<string | null>;
  businessJobPercentage: FormControl<number | null>;
  isProductBased: FormControl<boolean | null>;
};

@Component({
  selector: 'app-create-business',
  templateUrl: './create-business.component.html',
  styleUrls: ['./create-business.component.css'],
})
export class CreateBusinessComponent implements OnInit, AfterViewChecked {
  private static readonly DropdownPreferredHeight = 288;
  private pendingIconPreviewRender: File | null = null;
  private businessIconPreviewFile: File | null = null;

  @ViewChildren('iconPreviewCanvas')
  private iconPreviewCanvases?: QueryList<ElementRef<HTMLCanvasElement>>;

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
  launchingBusiness = false;
  displayName = 'User';
  initials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;
  iconPreview: string | null = null;
  iconPreviewLoadFailed = false;
  showLeaveConfirmation = false;

  businessForm = this.formBuilder.group<CreateBusinessForm>({
    name: this.formBuilder.control('', [Validators.required, Validators.maxLength(200)]),
    description: this.formBuilder.control('', [Validators.maxLength(1000)]),
    businessEmail: this.formBuilder.control('', [Validators.email, Validators.maxLength(256)]),
    businessPhoneNumber: this.formBuilder.control('', [Validators.maxLength(32)]),
    address1: this.formBuilder.control('', [Validators.required, Validators.maxLength(200)]),
    address2: this.formBuilder.control('', [Validators.maxLength(200)]),
    city: this.formBuilder.control('', [Validators.required, Validators.maxLength(120)]),
    state: this.formBuilder.control('', [Validators.required, Validators.maxLength(120)]),
    zipCode: this.formBuilder.control('', [Validators.required, Validators.maxLength(32)]),
    countryId: this.formBuilder.control<number | null>(null, [Validators.required]),
    businessIconBase64: this.formBuilder.control(''),
    businessJobPercentage: this.formBuilder.control(100, [Validators.required, Validators.min(0), Validators.max(100)]),
    isProductBased: this.formBuilder.control(true, [Validators.required]),
  });
  isProductBasedControl = this.businessForm.controls.isProductBased;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
    private readonly businessService: BusinessService,
    private readonly elementRef: ElementRef<HTMLElement>,
    private readonly authSession: AuthSessionService
  ) {}

  ngOnInit(): void {
    if (!localStorage.getItem('holonix_token')) {
      this.router.navigate(['/login']);
      return;
    }

    this.displayName = (localStorage.getItem('holonix_display_name') ?? '').trim() || 'User';
    const parts = this.displayName.split(' ').filter(Boolean);
    this.initials = `${parts[0]?.[0] ?? 'U'}${parts[1]?.[0] ?? ''}`.toUpperCase();
    this.profileImageDataUrl = this.buildImageDataUrl(localStorage.getItem('holonix_profile_image_base64'));

    this.loadingServices = true;
    this.businessService.getServices().subscribe({
      next: (services) => {
        this.allServices = services;
        this.loadingServices = false;
      },
      error: () => {
        this.loadingServices = false;
        this.snackBar.open('Could not load business categories.', 'Close', {
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

  ngAfterViewChecked(): void {
    if (!this.pendingIconPreviewRender || !this.iconPreviewCanvases || this.iconPreviewCanvases.length === 0) {
      return;
    }

    const nextPreviewFile = this.pendingIconPreviewRender;
    this.pendingIconPreviewRender = null;
    this.drawBusinessIconPreview(nextPreviewFile);
  }

  goHome(): void {
    this.router.navigate(['/home']);
  }

  goToBusinessOverview(): void {
    this.router.navigate(['/business']);
  }

  goToProfile(): void {
    this.router.navigate(['/profile']);
  }

  toggleUserMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.isUserMenuOpen = !this.isUserMenuOpen;
  }

  signOut(): void {
    this.authSession.clearSession();
    this.router.navigate(['/login']);
  }

  goBack(): void {
    if (this.currentStep > 1) {
      this.currentStep = (this.currentStep - 1) as 1 | 2 | 3;
      this.queueBusinessIconPreviewRender();
      return;
    }

    if (this.hasUnsavedBusinessData) {
      this.showLeaveConfirmation = true;
      return;
    }

    this.router.navigate(['/business']);
  }

  cancelLeaveConfirmation(): void {
    this.showLeaveConfirmation = false;
  }

  confirmLeave(): void {
    this.showLeaveConfirmation = false;
    this.router.navigate(['/business']);
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

  get businessModelLabel(): string {
    return this.isProductBasedControl.value ? 'Product Based' : 'Service Based';
  }

  get hasUnsavedBusinessData(): boolean {
    return this.businessForm.dirty || this.selectedServices.length > 0;
  }

  get filteredServices(): BusinessServiceOption[] {
    const query = this.serviceSearch.trim().toLowerCase();
    return this.allServices
      .filter((service) => !this.selectedServices.some((selected) => selected.serviceId === service.serviceId))
      .filter((service) => !query || service.name.toLowerCase().includes(query));
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

  onBusinessPhoneNumberInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const raw = input.value;
    const digits = raw.replace(/\D/g, '').slice(0, 11);
    const formatted = this.formatPhoneNumberValue(digits, raw);

    if (formatted !== raw) {
      input.value = formatted;
      input.setSelectionRange(formatted.length, formatted.length);
    }

    this.businessForm.controls.businessPhoneNumber.setValue(formatted, { emitEvent: false });
  }

  formatPhoneNumberForDisplay(value?: string | null): string {
    const trimmed = value?.trim() ?? '';
    if (!trimmed) {
      return '';
    }

    const digits = trimmed.replace(/\D/g, '');
    return this.formatPhoneNumberValue(digits, trimmed);
  }

  onBusinessIconSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    if (!file.type.startsWith('image/')) {
      this.snackBar.open('Choose an image file for the business icon.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      input.value = '';
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const dataUrl = typeof reader.result === 'string' ? reader.result : '';
      const normalized = this.extractBase64(dataUrl);
      const mimeType = this.extractImageMimeType(dataUrl) || file.type;
      if (!normalized) {
        this.businessForm.controls.businessIconBase64.setValue('');
        this.iconPreview = null;
        this.snackBar.open('Could not read that image file.', 'Close', {
          duration: 3000,
          panelClass: ['snack-error'],
        });
        return;
      }

      this.iconPreviewLoadFailed = false;
      this.iconPreview = this.buildImageDataUrl(normalized, mimeType);
      this.businessIconPreviewFile = file;
      this.pendingIconPreviewRender = file;
      this.businessForm.controls.businessIconBase64.setValue(normalized);
      this.businessForm.controls.businessIconBase64.markAsDirty();
    };
    reader.onerror = () => {
      this.businessForm.controls.businessIconBase64.setValue('');
      this.iconPreview = null;
      this.snackBar.open('Could not read that image file.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
    };

    reader.readAsDataURL(file);
  }

  removeBusinessIcon(): void {
    this.businessForm.controls.businessIconBase64.setValue('');
    this.iconPreview = null;
    this.iconPreviewLoadFailed = false;
    this.businessIconPreviewFile = null;
    this.pendingIconPreviewRender = null;
    this.clearBusinessIconCanvas();
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
    this.queueBusinessIconPreviewRender();
    this.submitAttempted = false;
  }

  goToStepThree(): void {
    this.submitAttempted = true;

    if (
      this.businessForm.controls.businessJobPercentage.invalid ||
      this.isProductBasedControl.invalid
    ) {
      this.businessForm.controls.businessJobPercentage.markAsTouched();
      return;
    }

    this.currentStep = 3;
    this.queueBusinessIconPreviewRender();
    this.submitAttempted = false;
  }

  editStep(step: 1 | 2): void {
    this.currentStep = step;
    this.queueBusinessIconPreviewRender();
  }

  submit(): void {
    if (this.launchingBusiness) {
      return;
    }

    const countryId = this.businessForm.controls.countryId.value;
    const businessJobPercentage = this.businessForm.controls.businessJobPercentage.value;
    const isProductBased = this.isProductBasedControl.value;

    if (
      !countryId ||
      businessJobPercentage === null ||
      typeof isProductBased !== 'boolean' ||
      this.selectedServices.length === 0
    ) {
      this.snackBar.open('Business details are incomplete.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    this.launchingBusiness = true;
    this.businessService.createBusiness({
      name: (this.businessForm.controls.name.value ?? '').trim(),
      description: this.businessForm.controls.description.value?.trim() || null,
      businessEmail: this.businessForm.controls.businessEmail.value?.trim() || null,
      businessPhoneNumber: this.businessForm.controls.businessPhoneNumber.value?.trim() || null,
      address1: (this.businessForm.controls.address1.value ?? '').trim(),
      address2: this.businessForm.controls.address2.value?.trim() || null,
      city: (this.businessForm.controls.city.value ?? '').trim(),
      state: (this.businessForm.controls.state.value ?? '').trim(),
      zipCode: (this.businessForm.controls.zipCode.value ?? '').trim(),
      countryId,
      businessIconBase64: this.businessForm.controls.businessIconBase64.value?.trim() || null,
      businessJobPercentage,
      isProductBased,
      serviceIds: this.selectedServices.map((service) => service.serviceId),
    }).subscribe({
      next: (response) => {
        this.launchingBusiness = false;
        this.authSession.persistSession(response);

        this.snackBar.open(`Business "${response.name}" launched.`, 'Close', {
          duration: 3000,
          panelClass: ['snack-success'],
        });
        this.router.navigate(['/business'], { state: { toastMessage: `Business "${response.name}" launched.` } });
      },
      error: (err) => {
        this.launchingBusiness = false;
        const errors = err?.error?.errors as string[] | undefined;
        this.snackBar.open(errors?.[0] ?? 'Could not launch business.', 'Close', {
          duration: 4000,
          panelClass: ['snack-error'],
        });
      },
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

  private extractBase64(dataUrl: string): string {
    const marker = 'base64,';
    const markerIndex = dataUrl.indexOf(marker);
    if (markerIndex < 0) {
      return '';
    }

    return dataUrl.slice(markerIndex + marker.length).trim();
  }

  private extractImageMimeType(dataUrl: string): string {
    const match = /^data:([^;]+);base64,/i.exec(dataUrl);
    return match?.[1]?.trim() ?? '';
  }

  private buildImageDataUrl(base64: string | null, mimeType = 'image/png'): string {
    if (!base64 || !base64.trim()) {
      return '';
    }

    return `data:${mimeType};base64,${base64.trim()}`;
  }

  private queueBusinessIconPreviewRender(): void {
    if (!this.businessIconPreviewFile || !this.iconPreview) {
      return;
    }

    this.pendingIconPreviewRender = this.businessIconPreviewFile;
  }

  private drawBusinessIconPreview(file: File): void {
    const canvases = this.iconPreviewCanvases?.toArray().map((entry) => entry.nativeElement) ?? [];
    if (canvases.length === 0) {
      this.pendingIconPreviewRender = file;
      return;
    }

    createImageBitmap(file)
      .then((imageBitmap) => {
      const drawableCanvases = canvases
        .map((canvas) => ({ canvas, context: canvas.getContext('2d') }))
        .filter((entry): entry is { canvas: HTMLCanvasElement; context: CanvasRenderingContext2D } => !!entry.context);

      if (drawableCanvases.length === 0) {
        this.iconPreviewLoadFailed = true;
        imageBitmap.close();
        return;
      }

      const maxWidth = 720;
      const maxHeight = 240;
      const scale = Math.min(maxWidth / imageBitmap.width, maxHeight / imageBitmap.height, 1);
      const drawWidth = Math.max(1, Math.round(imageBitmap.width * scale));
      const drawHeight = Math.max(1, Math.round(imageBitmap.height * scale));

      for (const { canvas, context } of drawableCanvases) {
        canvas.width = drawWidth;
        canvas.height = drawHeight;
        context.clearRect(0, 0, drawWidth, drawHeight);
        context.drawImage(imageBitmap, 0, 0, drawWidth, drawHeight);
      }

      imageBitmap.close();
      this.iconPreviewLoadFailed = false;
    })
      .catch(() => {
        this.iconPreviewLoadFailed = true;
        this.clearBusinessIconCanvas();
      });
  }

  private clearBusinessIconCanvas(): void {
    const canvases = this.iconPreviewCanvases?.toArray().map((entry) => entry.nativeElement) ?? [];
    for (const canvas of canvases) {
      const context = canvas.getContext('2d');
      if (!context) {
        continue;
      }

      context.clearRect(0, 0, canvas.width, canvas.height);
      canvas.width = 0;
      canvas.height = 0;
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    const element = target instanceof Element ? target : target?.parentElement;
    if (!element?.closest('.auth-actions-logged-in')) {
      this.isUserMenuOpen = false;
    }
  }
}
