import { Component, ElementRef, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  BusinessService,
  BusinessServiceOption,
  BusinessWorkspace,
  BusinessWorkspaceJob,
  BusinessWorkspaceService,
} from '../../../core/services/business.service';
import { AuthSessionService } from '../../../core/services/auth-session.service';

@Component({
  selector: 'app-business-workspace',
  templateUrl: './business-workspace.component.html',
  styleUrls: ['./business-workspace.component.css'],
})
export class BusinessWorkspaceComponent implements OnInit {
  private static readonly MaxVisibleServiceChips = 4;
  displayName = 'User';
  initials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;
  loadingWorkspace = true;
  loadingAvailableServices = false;
  savingServices = false;
  savingProfile = false;
  savingGeneralInformation = false;
  servicesExpanded = false;
  serviceSearch = '';
  serviceDropdownOpen = false;
  editingProfile = false;
  editingGeneralInformation = false;
  editBusinessName = '';
  editBusinessDescription = '';
  editBusinessIconBase64: string | null = null;
  editAddress1 = '';
  editAddress2 = '';
  editCity = '';
  editState = '';
  editZipCode = '';
  editBusinessJobPercentage = 0;
  businessWorkspace: BusinessWorkspace | null = null;
  allServices: BusinessServiceOption[] = [];

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly elementRef: ElementRef<HTMLElement>,
    private readonly snackBar: MatSnackBar,
    private readonly businessService: BusinessService,
    private readonly authSession: AuthSessionService
  ) {}

  ngOnInit(): void {
    const token = localStorage.getItem('holonix_token');
    if (!token) {
      this.router.navigate(['/login']);
      return;
    }

    this.displayName = (localStorage.getItem('holonix_display_name') ?? '').trim() || 'User';
    const parts = this.displayName.split(' ').filter(Boolean);
    this.initials = `${parts[0]?.[0] ?? 'U'}${parts[1]?.[0] ?? ''}`.toUpperCase();
    this.profileImageDataUrl = this.buildImageDataUrl(localStorage.getItem('holonix_profile_image_base64'));

    const businessId = Number(this.route.snapshot.paramMap.get('businessId'));
    if (!Number.isFinite(businessId) || businessId <= 0) {
      this.router.navigate(['/business']);
      return;
    }

    this.businessService.getBusinessWorkspace(businessId).subscribe({
      next: (workspace) => {
        this.businessWorkspace = workspace;
        this.servicesExpanded = false;
        this.resetProfileEditor(workspace);
        this.resetGeneralInformationEditor(workspace);
        this.loadingWorkspace = false;
        this.loadAvailableServices();
      },
      error: (err) => {
        this.loadingWorkspace = false;

        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        if (err?.status === 403) {
          this.snackBar.open('You do not have access to that business.', 'Close', {
            duration: 3500,
            panelClass: ['snack-error'],
          });
          this.router.navigate(['/business']);
          return;
        }

        if (err?.status === 404) {
          this.snackBar.open('That business could not be found.', 'Close', {
            duration: 3500,
            panelClass: ['snack-error'],
          });
          this.router.navigate(['/business']);
          return;
        }

        this.snackBar.open('Could not load the business workspace.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  goHome(): void {
    this.router.navigate(['/home']);
  }

  goToBusinesses(): void {
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

  get businessIconDataUrl(): string {
    return this.buildImageDataUrl(this.businessWorkspace?.businessIconBase64 ?? null);
  }

  get editBusinessIconPreviewUrl(): string {
    return this.buildImageDataUrl(this.editBusinessIconBase64);
  }

  get locationLabel(): string {
    const workspace = this.businessWorkspace;
    if (!workspace) {
      return 'Location not provided';
    }

    const parts = [workspace.city, workspace.state, workspace.countryName]
      .filter((value): value is string => !!value && value.trim().length > 0);

    return parts.length > 0 ? parts.join(', ') : 'Location not provided';
  }

  get addressLabel(): string {
    const workspace = this.businessWorkspace;
    if (!workspace) {
      return 'Address not provided';
    }

    const parts = [workspace.address1, workspace.address2, workspace.city, workspace.state, workspace.zipCode, workspace.countryName]
      .filter((value): value is string => !!value && value.trim().length > 0);

    return parts.length > 0 ? parts.join(', ') : 'Address not provided';
  }

  get serviceSummary(): string {
    const names = this.businessWorkspace?.services.map((service) => service.name) ?? [];
    return names.length > 0 ? names.join(', ') : 'No services configured yet.';
  }

  get canManageServices(): boolean {
    return (this.businessWorkspace?.currentUserRoleName ?? '').toLowerCase() === 'owner';
  }

  get canEditBusinessProfile(): boolean {
    return this.canManageServices;
  }

  get canViewGeneralInformation(): boolean {
    return this.canManageServices;
  }

  get filteredAvailableServices(): BusinessServiceOption[] {
    const query = this.serviceSearch.trim().toLowerCase();
    const selectedIds = new Set((this.businessWorkspace?.services ?? []).map((service) => service.serviceId));

    return this.allServices
      .filter((service) => !selectedIds.has(service.serviceId))
      .filter((service) => !query || service.name.toLowerCase().includes(query));
  }

  get visibleServiceChips(): BusinessWorkspaceService[] {
    const services = this.businessWorkspace?.services ?? [];
    return this.servicesExpanded
      ? services
      : services.slice(0, BusinessWorkspaceComponent.MaxVisibleServiceChips);
  }

  get hiddenServiceChipCount(): number {
    const services = this.businessWorkspace?.services ?? [];
    return Math.max(services.length - BusinessWorkspaceComponent.MaxVisibleServiceChips, 0);
  }

  onServiceSearchInput(event: Event): void {
    this.serviceSearch = (event.target as HTMLInputElement).value;
    this.serviceDropdownOpen = true;
  }

  openServiceDropdown(): void {
    this.serviceDropdownOpen = true;
  }

  closeServiceDropdown(): void {
    window.setTimeout(() => {
      this.serviceDropdownOpen = false;
    }, 120);
  }

  addService(service: BusinessServiceOption): void {
    if (!this.businessWorkspace || this.savingServices) {
      return;
    }

    const nextServices = [...this.businessWorkspace.services, { serviceId: service.serviceId, name: service.name }];
    this.saveServices(nextServices);
    this.serviceSearch = '';
    this.serviceDropdownOpen = false;
  }

  removeService(serviceId: number): void {
    if (!this.businessWorkspace || this.savingServices) {
      return;
    }

    const nextServices = this.businessWorkspace.services.filter((service) => service.serviceId !== serviceId);
    this.saveServices(nextServices);
  }

  expandServiceChips(): void {
    this.servicesExpanded = true;
  }

  startProfileEdit(): void {
    if (!this.businessWorkspace || this.savingProfile) {
      return;
    }

    this.editingProfile = true;
    this.resetProfileEditor(this.businessWorkspace);
  }

  cancelProfileEdit(): void {
    if (!this.businessWorkspace) {
      return;
    }

    this.editingProfile = false;
    this.resetProfileEditor(this.businessWorkspace);
  }

  startGeneralInformationEdit(): void {
    if (!this.businessWorkspace || this.savingGeneralInformation) {
      return;
    }

    this.editingGeneralInformation = true;
    this.resetGeneralInformationEditor(this.businessWorkspace);
  }

  cancelGeneralInformationEdit(): void {
    if (!this.businessWorkspace) {
      return;
    }

    this.editingGeneralInformation = false;
    this.resetGeneralInformationEditor(this.businessWorkspace);
  }

  onBusinessNameInput(event: Event): void {
    this.editBusinessName = (event.target as HTMLInputElement).value;
  }

  onBusinessDescriptionInput(event: Event): void {
    this.editBusinessDescription = (event.target as HTMLTextAreaElement).value;
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
      const result = typeof reader.result === 'string' ? reader.result : '';
      this.editBusinessIconBase64 = result || null;
    };
    reader.onerror = () => {
      this.snackBar.open('Could not read that image file.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
    };

    reader.readAsDataURL(file);
    input.value = '';
  }

  removeBusinessIcon(): void {
    this.editBusinessIconBase64 = null;
  }

  onAddress1Input(event: Event): void {
    this.editAddress1 = (event.target as HTMLInputElement).value;
  }

  onAddress2Input(event: Event): void {
    this.editAddress2 = (event.target as HTMLInputElement).value;
  }

  onCityInput(event: Event): void {
    this.editCity = (event.target as HTMLInputElement).value;
  }

  onStateInput(event: Event): void {
    this.editState = (event.target as HTMLInputElement).value;
  }

  onZipCodeInput(event: Event): void {
    this.editZipCode = (event.target as HTMLInputElement).value;
  }

  onBusinessJobPercentageInput(event: Event): void {
    const rawValue = (event.target as HTMLInputElement).value;
    const parsedValue = Number(rawValue);
    this.editBusinessJobPercentage = Number.isFinite(parsedValue) ? parsedValue : 0;
  }

  saveBusinessProfile(): void {
    const workspace = this.businessWorkspace;
    if (!workspace || this.savingProfile) {
      return;
    }

    const name = this.editBusinessName.trim();
    if (!name) {
      this.snackBar.open('Business name is required.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    this.savingProfile = true;
    this.businessService.updateBusinessProfile(workspace.businessId, {
      name,
      description: this.editBusinessDescription.trim() || null,
      businessIconBase64: this.editBusinessIconBase64,
      address1: workspace.address1?.trim() || '',
      address2: workspace.address2?.trim() || null,
      city: workspace.city?.trim() || '',
      state: workspace.state?.trim() || '',
      zipCode: workspace.zipCode?.trim() || '',
      businessJobPercentage: workspace.businessJobPercentage,
    }).subscribe({
      next: (updatedProfile) => {
        if (!this.businessWorkspace) {
          return;
        }

        this.businessWorkspace = {
          ...this.businessWorkspace,
          name: updatedProfile.name,
          description: updatedProfile.description,
          businessIconBase64: updatedProfile.businessIconBase64,
          address1: updatedProfile.address1,
          address2: updatedProfile.address2,
          city: updatedProfile.city,
          state: updatedProfile.state,
          zipCode: updatedProfile.zipCode,
          countryName: updatedProfile.countryName,
          businessJobPercentage: updatedProfile.businessJobPercentage,
        };
        this.editingProfile = false;
        this.savingProfile = false;
        this.resetProfileEditor(this.businessWorkspace);
        this.resetGeneralInformationEditor(this.businessWorkspace);
        this.snackBar.open('Business profile updated.', 'Close', {
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
        this.snackBar.open(errors?.[0] ?? 'Could not update business profile.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  saveGeneralInformation(): void {
    const workspace = this.businessWorkspace;
    if (!workspace || this.savingGeneralInformation) {
      return;
    }

    if (!this.editAddress1.trim() || !this.editCity.trim() || !this.editState.trim() || !this.editZipCode.trim()) {
      this.snackBar.open('Business address is required.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    if (!Number.isFinite(this.editBusinessJobPercentage) || this.editBusinessJobPercentage < 0 || this.editBusinessJobPercentage > 100) {
      this.snackBar.open('Business earnings percentage must be between 0 and 100.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    this.savingGeneralInformation = true;
    this.businessService.updateBusinessProfile(workspace.businessId, {
      name: workspace.name,
      description: workspace.description ?? null,
      businessIconBase64: workspace.businessIconBase64 ?? null,
      address1: this.editAddress1.trim(),
      address2: this.editAddress2.trim() || null,
      city: this.editCity.trim(),
      state: this.editState.trim(),
      zipCode: this.editZipCode.trim(),
      businessJobPercentage: Math.round(this.editBusinessJobPercentage),
    }).subscribe({
      next: (updatedProfile) => {
        if (!this.businessWorkspace) {
          return;
        }

        this.businessWorkspace = {
          ...this.businessWorkspace,
          name: updatedProfile.name,
          description: updatedProfile.description,
          businessIconBase64: updatedProfile.businessIconBase64,
          address1: updatedProfile.address1,
          address2: updatedProfile.address2,
          city: updatedProfile.city,
          state: updatedProfile.state,
          zipCode: updatedProfile.zipCode,
          countryName: updatedProfile.countryName,
          businessJobPercentage: updatedProfile.businessJobPercentage,
        };
        this.editingGeneralInformation = false;
        this.savingGeneralInformation = false;
        this.resetProfileEditor(this.businessWorkspace);
        this.resetGeneralInformationEditor(this.businessWorkspace);
        this.snackBar.open('General information updated.', 'Close', {
          duration: 2500,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.savingGeneralInformation = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = err?.error?.errors as string[] | undefined;
        this.snackBar.open(errors?.[0] ?? 'Could not update general information.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  formatMoney(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      maximumFractionDigits: 0,
    }).format(value || 0);
  }

  formatJobWindow(job: BusinessWorkspaceJob): string {
    const start = new Date(job.startDateTime);
    const end = new Date(job.endDateTime);

    const dayLabel = new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(start);

    const timeFormatter = new Intl.DateTimeFormat('en-US', {
      hour: 'numeric',
      minute: '2-digit',
    });

    return `${dayLabel} · ${timeFormatter.format(start)} - ${timeFormatter.format(end)}`;
  }

  formatHiredDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return 'Unavailable';
    }

    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(date);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    if (target && !this.elementRef.nativeElement.contains(target)) {
      this.isUserMenuOpen = false;
    }
  }

  private buildImageDataUrl(base64: string | null): string {
    if (!base64 || !base64.trim()) {
      return '';
    }

    const trimmed = base64.trim();
    if (trimmed.startsWith('data:')) {
      return trimmed;
    }

    return `data:image/*;base64,${trimmed}`;
  }

  private loadAvailableServices(): void {
    this.loadingAvailableServices = true;
    this.businessService.getServices().subscribe({
      next: (services) => {
        this.allServices = services;
        this.loadingAvailableServices = false;
      },
      error: () => {
        this.loadingAvailableServices = false;
      },
    });
  }

  private resetProfileEditor(workspace: BusinessWorkspace): void {
    this.editBusinessName = workspace.name ?? '';
    this.editBusinessDescription = workspace.description ?? '';
    this.editBusinessIconBase64 = workspace.businessIconBase64 ?? null;
  }

  private resetGeneralInformationEditor(workspace: BusinessWorkspace): void {
    this.editAddress1 = workspace.address1 ?? '';
    this.editAddress2 = workspace.address2 ?? '';
    this.editCity = workspace.city ?? '';
    this.editState = workspace.state ?? '';
    this.editZipCode = workspace.zipCode ?? '';
    this.editBusinessJobPercentage = workspace.businessJobPercentage ?? 0;
  }

  private saveServices(services: BusinessWorkspaceService[]): void {
    const workspace = this.businessWorkspace;
    if (!workspace) {
      return;
    }

    if (services.length === 0) {
      this.snackBar.open('At least one service is required.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    this.savingServices = true;
    this.businessService.updateBusinessServices(
      workspace.businessId,
      services.map((service) => service.serviceId)
    ).subscribe({
      next: (updatedServices) => {
        if (!this.businessWorkspace) {
          return;
        }

        this.businessWorkspace = {
          ...this.businessWorkspace,
          services: updatedServices,
          serviceCount: updatedServices.length,
        };
        this.servicesExpanded = updatedServices.length <= BusinessWorkspaceComponent.MaxVisibleServiceChips;
        this.savingServices = false;
        this.snackBar.open('Business services updated.', 'Close', {
          duration: 2500,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.savingServices = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = err?.error?.errors as string[] | undefined;
        this.snackBar.open(errors?.[0] ?? 'Could not update business services.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }
}
