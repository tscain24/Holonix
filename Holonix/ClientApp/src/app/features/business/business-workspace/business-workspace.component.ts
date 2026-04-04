import { Component, ElementRef, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  BusinessService,
  BusinessServiceOption,
  BusinessWorkspace,
  BusinessWorkspaceJob,
  BusinessWorkspaceService,
  BusinessWorkspaceSubService,
} from '../../../core/services/business.service';
import { AuthSessionService } from '../../../core/services/auth-session.service';

interface VisibleBusinessSubService extends BusinessWorkspaceSubService {
  parentServiceName: string;
}

@Component({
  selector: 'app-business-workspace',
  templateUrl: './business-workspace.component.html',
  styleUrls: ['./business-workspace.component.css'],
})
export class BusinessWorkspaceComponent implements OnInit {
  private static readonly MaxVisibleServiceChips = 4;
  private static readonly MaxVisibleSubServices = 10;
  private static readonly DeleteBusinessConfirmation = 'DELETE';
  displayName = 'User';
  initials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;
  loadingWorkspace = true;
  loadingAvailableServices = false;
  savingServices = false;
  creatingSubServiceIds = new Set<number>();
  deletingSubServiceIds = new Set<number>();
  savingProfile = false;
  savingGeneralInformation = false;
  deletingBusiness = false;
  leavingBusiness = false;
  deleteBusinessModalOpen = false;
  leaveBusinessModalOpen = false;
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
  deleteBusinessConfirmation = '';
  businessWorkspace: BusinessWorkspace | null = null;
  allServices: BusinessServiceOption[] = [];
  subServiceDrafts: Record<number, string> = {};
  subServiceEffectiveDateDrafts: Record<number, string> = {};

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

    const businessCode = (this.route.snapshot.paramMap.get('businessCode') ?? '').trim();
    if (!businessCode) {
      this.router.navigate(['/business']);
      return;
    }

    this.businessService.getBusinessWorkspace(businessCode).subscribe({
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

  goToEmployees(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (!businessCode) {
      return;
    }

    this.router.navigate(['/business', businessCode, 'employees']);
  }

  goToOverviewTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (!businessCode) {
      return;
    }

    this.router.navigate(['/business', businessCode]);
  }

  goToEmployeesTab(): void {
    this.goToEmployees();
  }

  goToServiceManagerTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (!businessCode) {
      return;
    }

    this.router.navigate(['/business', businessCode], { fragment: 'service-manager' });
  }

  goToJobsTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (!businessCode) {
      return;
    }

    this.router.navigate(['/business', businessCode], { fragment: 'jobs' });
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

  get canManageSubServices(): boolean {
    const role = (this.businessWorkspace?.currentUserRoleName ?? '').trim().toLowerCase();
    return role === 'owner' || role === 'administrator';
  }

  get canEditBusinessProfile(): boolean {
    return this.canManageServices;
  }

  get canViewGeneralInformation(): boolean {
    return this.canManageServices;
  }

  get canDeleteBusiness(): boolean {
    return this.canManageServices;
  }

  get canLeaveBusiness(): boolean {
    const role = (this.businessWorkspace?.currentUserRoleName ?? '').trim().toLowerCase();
    return role.length > 0 && role !== 'owner';
  }

  get canConfirmDeleteBusiness(): boolean {
    return this.deleteBusinessConfirmation.trim().toUpperCase() === BusinessWorkspaceComponent.DeleteBusinessConfirmation;
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

  get activeWorkspaceEmployees(): BusinessWorkspace['employees'] {
    return (this.businessWorkspace?.employees ?? []).filter((employee) => employee.isActive);
  }

  get visibleSubServices(): VisibleBusinessSubService[] {
    return (this.businessWorkspace?.services ?? [])
      .flatMap((service) =>
        service.subServices.map((subService) => ({
          ...subService,
          parentServiceName: service.name,
        }))
      )
      .sort((a, b) => {
        const effectiveDateCompare = a.effectiveDate.localeCompare(b.effectiveDate);
        if (effectiveDateCompare !== 0) {
          return effectiveDateCompare;
        }

        const parentCompare = a.parentServiceName.localeCompare(b.parentServiceName);
        if (parentCompare !== 0) {
          return parentCompare;
        }

        return a.name.localeCompare(b.name);
      })
      .slice(0, BusinessWorkspaceComponent.MaxVisibleSubServices);
  }

  hasVisibleSubServices(): boolean {
    return this.visibleSubServices.length > 0;
  }

  hasActiveWorkspaceEmployees(): boolean {
    return this.activeWorkspaceEmployees.length > 0;
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

    const nextServices = [...this.businessWorkspace.services, { serviceId: service.serviceId, name: service.name, subServices: [] }];
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

  onSubServiceNameInput(serviceId: number, event: Event): void {
    this.subServiceDrafts[serviceId] = (event.target as HTMLInputElement).value;
  }

  onSubServiceEffectiveDateInput(serviceId: number, event: Event): void {
    this.subServiceEffectiveDateDrafts[serviceId] = (event.target as HTMLInputElement).value;
  }

  getSubServiceDraft(serviceId: number): string {
    return this.subServiceDrafts[serviceId] ?? '';
  }

  getSubServiceEffectiveDateDraft(serviceId: number): string {
    return this.subServiceEffectiveDateDrafts[serviceId] ?? this.getTodayDateInputValue();
  }

  isCreatingSubService(serviceId: number): boolean {
    return this.creatingSubServiceIds.has(serviceId);
  }

  isDeletingSubService(businessSubServiceId: number): boolean {
    return this.deletingSubServiceIds.has(businessSubServiceId);
  }

  addSubService(service: BusinessWorkspaceService): void {
    const workspace = this.businessWorkspace;
    const businessCode = workspace?.businessCode?.trim();
    const name = this.getSubServiceDraft(service.serviceId).trim();
    const effectiveDate = this.getSubServiceEffectiveDateDraft(service.serviceId).trim();
    if (!workspace || !businessCode || !this.canManageSubServices || !name || !effectiveDate || this.isCreatingSubService(service.serviceId)) {
      return;
    }

    this.creatingSubServiceIds.add(service.serviceId);
    this.businessService.createBusinessSubService(businessCode, service.serviceId, name, effectiveDate).subscribe({
      next: (subService) => {
        if (!this.businessWorkspace) {
          return;
        }

        this.businessWorkspace = {
          ...this.businessWorkspace,
          services: this.businessWorkspace.services.map((item) =>
            item.serviceId !== service.serviceId
              ? item
              : {
                  ...item,
                  subServices: [...item.subServices, subService].sort((a, b) => a.name.localeCompare(b.name)),
                })
        };
        this.subServiceDrafts[service.serviceId] = '';
        this.subServiceEffectiveDateDrafts[service.serviceId] = this.getTodayDateInputValue();
        this.creatingSubServiceIds.delete(service.serviceId);
        this.snackBar.open('Sub-service added.', 'Close', {
          duration: 2500,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.creatingSubServiceIds.delete(service.serviceId);
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = err?.error?.errors as string[] | undefined;
        this.snackBar.open(errors?.[0] ?? 'Could not add sub-service.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  removeSubService(service: BusinessWorkspaceService, subService: BusinessWorkspaceSubService): void {
    const workspace = this.businessWorkspace;
    const businessCode = workspace?.businessCode?.trim();
    if (!workspace || !businessCode || !this.canManageSubServices || this.isDeletingSubService(subService.businessSubServiceId)) {
      return;
    }

    this.deletingSubServiceIds.add(subService.businessSubServiceId);
    this.businessService.deleteBusinessSubService(businessCode, subService.businessSubServiceId).subscribe({
      next: () => {
        if (!this.businessWorkspace) {
          return;
        }

        this.businessWorkspace = {
          ...this.businessWorkspace,
          services: this.businessWorkspace.services.map((item) =>
            item.serviceId !== service.serviceId
              ? item
              : {
                  ...item,
                  subServices: item.subServices.filter((entry) => entry.businessSubServiceId !== subService.businessSubServiceId),
                })
        };
        this.deletingSubServiceIds.delete(subService.businessSubServiceId);
        this.snackBar.open('Sub-service removed.', 'Close', {
          duration: 2500,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.deletingSubServiceIds.delete(subService.businessSubServiceId);
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = err?.error?.errors as string[] | undefined;
        this.snackBar.open(errors?.[0] ?? 'Could not remove sub-service.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
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
    this.businessService.updateBusinessProfile(workspace.businessCode, {
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
    this.businessService.updateBusinessProfile(workspace.businessCode, {
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

  openDeleteBusinessModal(): void {
    if (!this.businessWorkspace || this.deletingBusiness || !this.canDeleteBusiness) {
      return;
    }

    this.deleteBusinessConfirmation = '';
    this.deleteBusinessModalOpen = true;
  }

  closeDeleteBusinessModal(): void {
    if (this.deletingBusiness) {
      return;
    }

    this.deleteBusinessModalOpen = false;
    this.deleteBusinessConfirmation = '';
  }

  openLeaveBusinessModal(): void {
    if (!this.businessWorkspace || this.leavingBusiness || !this.canLeaveBusiness) {
      return;
    }

    this.leaveBusinessModalOpen = true;
  }

  closeLeaveBusinessModal(): void {
    if (this.leavingBusiness) {
      return;
    }

    this.leaveBusinessModalOpen = false;
  }

  onDeleteBusinessConfirmationInput(event: Event): void {
    this.deleteBusinessConfirmation = (event.target as HTMLInputElement).value;
  }

  confirmDeleteBusiness(): void {
    const workspace = this.businessWorkspace;
    if (!workspace || this.deletingBusiness || !this.canDeleteBusiness || !this.canConfirmDeleteBusiness) {
      return;
    }

    this.deletingBusiness = true;
    this.businessService.deleteBusiness(workspace.businessCode).subscribe({
      next: () => {
        this.deletingBusiness = false;
        this.deleteBusinessModalOpen = false;
        this.deleteBusinessConfirmation = '';
        this.snackBar.open('Business Deleted.', 'Close', {
          duration: 3000,
          panelClass: ['snack-success'],
        });
        this.router.navigate(['/business']);
      },
      error: (err) => {
        this.deletingBusiness = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        if (err?.status === 403) {
          this.snackBar.open('Only business owners can delete a business.', 'Close', {
            duration: 3500,
            panelClass: ['snack-error'],
          });
          return;
        }

        if (err?.status === 404) {
          this.deleteBusinessModalOpen = false;
          this.deleteBusinessConfirmation = '';
          this.snackBar.open('That business could not be found.', 'Close', {
            duration: 3500,
            panelClass: ['snack-error'],
          });
          this.router.navigate(['/business']);
          return;
        }

        const errors = err?.error?.errors as string[] | undefined;
        this.snackBar.open(errors?.[0] ?? 'Could not delete the business.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  confirmLeaveBusiness(): void {
    const workspace = this.businessWorkspace;
    if (!workspace || this.leavingBusiness || !this.canLeaveBusiness) {
      return;
    }

    this.leavingBusiness = true;
    this.businessService.leaveBusiness(workspace.businessCode).subscribe({
      next: () => {
        this.leavingBusiness = false;
        this.leaveBusinessModalOpen = false;
        this.snackBar.open('You left the business.', 'Close', {
          duration: 3000,
          panelClass: ['snack-success'],
        });
        this.router.navigate(['/business']);
      },
      error: (err) => {
        this.leavingBusiness = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = err?.error?.errors as string[] | undefined;
        this.snackBar.open(errors?.[0] ?? 'Could not leave the business.', 'Close', {
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

  formatEffectiveDate(value: string): string {
    if (!value?.trim()) {
      return 'No effective date';
    }

    const date = new Date(`${value}T00:00:00`);
    if (Number.isNaN(date.getTime())) {
      return value;
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
    const element = target instanceof Element ? target : target?.parentElement;
    if (!element?.closest('.auth-actions-logged-in')) {
      this.isUserMenuOpen = false;
    }
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.deleteBusinessModalOpen && !this.deletingBusiness) {
      this.closeDeleteBusinessModal();
    }

    if (this.leaveBusinessModalOpen && !this.leavingBusiness) {
      this.closeLeaveBusinessModal();
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

  private getTodayDateInputValue(): string {
    const now = new Date();
    const month = `${now.getMonth() + 1}`.padStart(2, '0');
    const day = `${now.getDate()}`.padStart(2, '0');
    return `${now.getFullYear()}-${month}-${day}`;
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
      workspace.businessCode,
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
