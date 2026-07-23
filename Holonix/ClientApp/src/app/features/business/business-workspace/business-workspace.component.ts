import { Component, ElementRef, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, catchError, debounceTime, distinctUntilChanged, finalize, forkJoin, of, switchMap } from 'rxjs';
import {
  BusinessDailyTeamAvailabilityMember,
  BusinessHoursDay,
  BusinessService,
  BusinessServiceOption,
  BusinessWorkspace,
  BusinessWorkspaceJob,
  BusinessWorkspaceJobLocation,
  BusinessWorkspaceJobRequest,
  BusinessWorkspaceService,
  BusinessWorkspaceSubService,
  JobRequestAvailabilityBooking,
  JobRequestAvailabilityResponse,
} from '../../../core/services/business.service';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { AddressSuggestion, GeocodingService } from '../../../core/services/geocoding.service';
import { normalizeImageToSquarePngBase64 } from '../../../core/utils/image-normalize';

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
  private static readonly MaxVisibleSubServices = 5;
  private static readonly DeleteBusinessConfirmation = 'DELETE';
  private static readonly MobileNavBreakpointPx = 900;
  displayName = 'User';
  initials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;
  isWorkspaceMenuOpen = false;
  isMobileNav = false;
  loadingWorkspace = true;
  loadingAvailableServices = false;
  savingServices = false;
  creatingSubServiceIds = new Set<number>();
  deletingSubServiceIds = new Set<number>();
  updatingJobRequestIds = new Set<number>();
  savingProfile = false;
  savingGeneralInformation = false;
  deletingBusiness = false;
  leavingBusiness = false;
  deleteBusinessModalOpen = false;
  leaveBusinessModalOpen = false;
  jobRequestAvailabilityModalOpen = false;
  costBreakdownRequest: BusinessWorkspaceJobRequest | null = null;
  loadingJobRequestAvailability = false;
  jobRequestAvailabilityError = '';
  availabilityRequest: BusinessWorkspaceJobRequest | null = null;
  jobRequestAvailability: JobRequestAvailabilityResponse | null = null;
  availabilityTeamMembers: BusinessDailyTeamAvailabilityMember[] = [];
  availabilityTimelineHours: number[] = [];
  private availabilityTimelineStartMinutes = 6 * 60;
  private availabilityTimelineEndMinutes = 20 * 60;
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
  editLatitude: number | null = null;
  editLongitude: number | null = null;
  editBusinessEmail = '';
  editBusinessPhoneNumber = '';
  editCity = '';
  editState = '';
  editZipCode = '';
  deleteBusinessConfirmation = '';
  editBusinessHoursDraft: BusinessHoursDay[] = BusinessWorkspaceComponent.createDefaultBusinessHours();
  private openHoursTimePicker: { dayOfWeek: number; field: 'open' | 'close' } | null = null;
  readonly timeOptions = BusinessWorkspaceComponent.buildTimeOptions(30);
  businessWorkspace: BusinessWorkspace | null = null;
  allServices: BusinessServiceOption[] = [];
  addressSuggestions: AddressSuggestion[] = [];
  addressDropdownOpen = false;
  highlightedAddressIndex = -1;
  loadingAddressSuggestions = false;
  addressAutocompleteError: string | null = null;
  confirmGeneralInfoAddressModalOpen = false;
  pendingGeneralInfoAddress: AddressSuggestion | null = null;
  resolvingGeneralInfoAddress = false;
  private originalGeneralInfoAddress: {
    address1: string;
    address2: string;
    city: string;
    state: string;
    zipCode: string;
  } | null = null;
  private readonly addressSearch$ = new Subject<string>();
  subServiceDrafts: Record<number, string> = {};
  subServiceEffectiveDateDrafts: Record<number, string> = {};

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly elementRef: ElementRef<HTMLElement>,
    private readonly snackBar: MatSnackBar,
    private readonly businessService: BusinessService,
    private readonly authSession: AuthSessionService,
    private readonly geocodingService: GeocodingService
  ) {}

  ngOnInit(): void {
    this.refreshMobileNav();
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

    this.addressSearch$
      .pipe(
        debounceTime(600),
        distinctUntilChanged(),
        switchMap((value) => {
          const term = value.trim();
          const startsWithDigit = term.length > 0 && /^\d/.test(term);
          const minimumLength = startsWithDigit ? 2 : 3;
          if (term.length < minimumLength || !this.editingGeneralInformation) {
            this.addressSuggestions = [];
            this.loadingAddressSuggestions = false;
            this.addressAutocompleteError = null;
            return of([]);
          }

          const city = this.editCity.trim();
          const state = this.editState.trim();
          const zipCode = this.editZipCode.trim();
          const contextualParts = [city, state, zipCode].filter((part) => part.length > 0);
          const contextualQuery = contextualParts.length > 0
            ? `${term}, ${contextualParts.join(' ')}`
            : term;

          this.loadingAddressSuggestions = true;
          this.addressAutocompleteError = null;
          return this.geocodingService.getAddressSuggestions(contextualQuery, null, 5).pipe(
            catchError((err) => {
              // eslint-disable-next-line no-console
              console.error('Failed to load address suggestions.', err);
              if (err?.status === 401) {
                this.addressAutocompleteError = 'Your session expired. Please sign in again.';
              } else {
                this.addressAutocompleteError = 'Could not load address suggestions.';
              }

              return of([]);
            }),
            finalize(() => {
              this.loadingAddressSuggestions = false;
            })
          );
        })
      )
      .subscribe((suggestions) => {
        this.addressSuggestions = suggestions;
        if (this.addressDropdownOpen && suggestions.length === 0) {
          this.highlightedAddressIndex = -1;
        }
      });

    this.loadWorkspace(businessCode);
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

    this.router.navigate(['/workspace', 'employees', businessCode]);
  }

  goToOverviewTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (!businessCode) {
      return;
    }

    this.router.navigate(['/workspace', 'overview', businessCode]);
  }

  goToEmployeesTab(): void {
    this.goToEmployees();
  }

  goToServiceManagerTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (!businessCode) {
      return;
    }

    this.router.navigate(['/workspace', 'services', businessCode]);
  }

  goToMyAvailabilityTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (!businessCode) {
      return;
    }

    this.router.navigate(['/workspace', 'availability', businessCode]);
  }

  goToJobsTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (!businessCode) {
      return;
    }

    this.router.navigate(['/workspace', 'overview', businessCode], { fragment: 'jobs' });
  }

  goToPublicPage(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (!businessCode) {
      return;
    }

    this.router.navigate(['/', businessCode], {
      state: {
        entry: 'business',
        returnUrl: this.router.url,
      },
    });
  }

  goToProfile(): void {
    this.router.navigate(['/profile']);
  }

  toggleUserMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.isUserMenuOpen = !this.isUserMenuOpen;
  }

  signOut(): void {
    this.isUserMenuOpen = false;
    this.authSession.logout();
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

  get canManageJobRequests(): boolean {
    return this.businessWorkspace?.canManageJobRequests ?? false;
  }

  get canViewJobRequestCostBreakdowns(): boolean {
    return this.businessWorkspace?.canViewJobRequestCostBreakdowns ?? false;
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

  get activeSubServiceCount(): number {
    const today = this.getTodayDateInputValue();
    return (this.businessWorkspace?.services ?? [])
      .flatMap((service) => service.subServices)
      .filter((subService) => {
        const effectiveDate = (subService.effectiveDate ?? '').trim();
        return effectiveDate.length > 0 && effectiveDate <= today;
      })
      .length;
  }

  hasVisibleSubServices(): boolean {
    return this.visibleSubServices.length > 0;
  }

  hasActiveWorkspaceEmployees(): boolean {
    return this.activeWorkspaceEmployees.length > 0;
  }

  isUpdatingJobRequest(workloadId: number): boolean {
    return this.updatingJobRequestIds.has(workloadId);
  }

  acceptJobRequest(workloadId: number): void {
    this.updateJobRequest(workloadId, 'accept');
  }

  denyJobRequest(workloadId: number): void {
    this.updateJobRequest(workloadId, 'deny');
  }

  openCostBreakdown(request: BusinessWorkspaceJobRequest): void {
    if (!this.canViewJobRequestCostBreakdowns || !request.costBreakdown) {
      return;
    }
    this.costBreakdownRequest = request;
  }

  closeCostBreakdown(): void {
    this.costBreakdownRequest = null;
  }

  formatMoneyExact(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value || 0);
  }

  openJobRequestAvailability(request: BusinessWorkspaceJobRequest): void {
    const businessCode = (this.businessWorkspace?.businessCode ?? '').trim();
    if (!businessCode || !request?.workloadId) {
      return;
    }

    this.availabilityRequest = request;
    this.jobRequestAvailabilityModalOpen = true;
    this.loadingJobRequestAvailability = true;
    this.jobRequestAvailabilityError = '';
    this.jobRequestAvailability = null;
    this.availabilityTeamMembers = [];

    const dateIso = this.toLocalIsoDate(this.parseBusinessScheduleDate(request.startDateTime));
    forkJoin({
      schedule: this.businessService.getJobRequestAvailability(businessCode, request.workloadId),
      team: this.businessService.getBusinessTeamDailyAvailability(businessCode, dateIso),
    }).subscribe({
      next: ({ schedule, team }) => {
        this.loadingJobRequestAvailability = false;
        this.jobRequestAvailability = schedule;
        this.availabilityTeamMembers = (team?.members ?? []).slice().sort((a, b) =>
          `${a.lastName} ${a.firstName}`.localeCompare(`${b.lastName} ${b.firstName}`)
        );
        this.configureAvailabilityTimeline();
      },
      error: (err) => {
        this.loadingJobRequestAvailability = false;
        const errors = err?.error?.errors as string[] | undefined;
        this.jobRequestAvailabilityError = errors?.[0] ?? 'Could not load availability for this request.';
      },
    });
  }

  closeJobRequestAvailability(): void {
    if (this.loadingJobRequestAvailability) {
      return;
    }

    this.jobRequestAvailabilityModalOpen = false;
    this.availabilityRequest = null;
    this.jobRequestAvailability = null;
    this.availabilityTeamMembers = [];
    this.jobRequestAvailabilityError = '';
  }

  availabilityBlockStyle(start: string, end: string, timeOnly = false): Record<string, string> {
    const startMinutes = timeOnly ? this.timeOnlyToMinutes(start) : this.dateTimeToMinutes(start);
    const endMinutes = timeOnly ? this.timeOnlyToMinutes(end) : this.dateTimeToMinutes(end);
    const total = Math.max(1, this.availabilityTimelineEndMinutes - this.availabilityTimelineStartMinutes);
    const left = Math.max(0, Math.min(100, ((startMinutes - this.availabilityTimelineStartMinutes) / total) * 100));
    const right = Math.max(left, Math.min(100, ((endMinutes - this.availabilityTimelineStartMinutes) / total) * 100));
    return { left: `${left}%`, width: `${Math.max(1.5, right - left)}%` };
  }

  availabilityHourLabel(hour: number): string {
    const suffix = hour >= 12 ? 'PM' : 'AM';
    const displayHour = hour % 12 || 12;
    return `${displayHour} ${suffix}`;
  }

  formatAvailabilityDate(value: string): string {
    const date = new Date(`${value}T12:00:00`);
    return new Intl.DateTimeFormat('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' }).format(date);
  }

  formatAvailabilityTimeRange(item: Pick<JobRequestAvailabilityBooking, 'startDateTime' | 'endDateTime'>): string {
    const formatter = new Intl.DateTimeFormat('en-US', { hour: 'numeric', minute: '2-digit' });
    return `${formatter.format(this.parseBusinessScheduleDate(item.startDateTime))} – ${formatter.format(this.parseBusinessScheduleDate(item.endDateTime))}`;
  }

  requestConflictsWith(job: JobRequestAvailabilityBooking): boolean {
    const request = this.jobRequestAvailability?.request;
    if (!request) {
      return false;
    }

    return new Date(job.startDateTime).getTime() < new Date(request.endDateTime).getTime()
      && new Date(job.endDateTime).getTime() > new Date(request.startDateTime).getTime();
  }

  teamMemberAvailabilityLabel(member: BusinessDailyTeamAvailabilityMember): string {
    if (member.isDayOff) {
      return 'Day off';
    }
    if (!member.isAvailable || member.timeFrames.length === 0) {
      return 'Not available';
    }
    return member.timeFrames.map((frame) => `${this.formatTimeOnly(frame.startTime)}–${this.formatTimeOnly(frame.endTime)}`).join(', ');
  }

  formatServiceLocation(location?: BusinessWorkspaceJobLocation | null): string {
    if (!location) {
      return 'Location unavailable';
    }

    return [location.address1, location.address2, `${location.city}, ${location.state} ${location.zipCode}`]
      .map((part) => (part ?? '').trim())
      .filter((part) => part.length > 0)
      .join(', ');
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
    this.businessService.createBusinessSubService(businessCode, service.serviceId, {
      name,
      requiresServiceLocation: false,
      consultationNeeded: true,
      durationMinutes: 15,
      price: 0,
      employeeCount: 1,
      effectiveDate,
    }).subscribe({
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
        this.snackBar.open('Service added.', 'Close', {
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
        this.snackBar.open(errors?.[0] ?? 'Could not add service.', 'Close', {
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
        this.snackBar.open('Service removed.', 'Close', {
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
        this.snackBar.open(errors?.[0] ?? 'Could not remove service.', 'Close', {
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

    normalizeImageToSquarePngBase64(file, 256)
      .then(({ base64 }) => {
        this.editBusinessIconBase64 = base64;
      })
      .catch(() => {
        this.snackBar.open('Could not read that image file.', 'Close', {
          duration: 3000,
          panelClass: ['snack-error'],
        });
      });
    input.value = '';
  }

  removeBusinessIcon(): void {
    this.editBusinessIconBase64 = null;
  }

  onAddress1Input(event: Event): void {
    this.editAddress1 = (event.target as HTMLInputElement).value;
    this.editLatitude = null;
    this.editLongitude = null;
    this.addressDropdownOpen = true;
    this.highlightedAddressIndex = this.addressSuggestions.length > 0 ? 0 : -1;
    this.addressSearch$.next(this.editAddress1);
  }

  openAddressDropdown(): void {
    this.addressDropdownOpen = true;
    this.highlightedAddressIndex = this.addressSuggestions.length > 0 ? 0 : -1;
    this.addressSearch$.next(this.editAddress1);
  }

  closeAddressDropdown(): void {
    window.setTimeout(() => {
      this.addressDropdownOpen = false;
      this.highlightedAddressIndex = -1;
    }, 120);
  }

  onAddressSearchKeydown(event: KeyboardEvent): void {
    const options = this.addressSuggestions;
    if (options.length === 0 && event.key !== 'Escape') {
      return;
    }

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.addressDropdownOpen = true;
        this.highlightedAddressIndex = options.length === 0
          ? -1
          : (this.highlightedAddressIndex + 1 + options.length) % options.length;
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.addressDropdownOpen = true;
        this.highlightedAddressIndex = options.length === 0
          ? -1
          : (this.highlightedAddressIndex - 1 + options.length) % options.length;
        break;
      case 'Enter':
        if (!this.addressDropdownOpen || this.highlightedAddressIndex < 0 || this.highlightedAddressIndex >= options.length) {
          return;
        }

        event.preventDefault();
        this.selectAddressSuggestion(options[this.highlightedAddressIndex]);
        break;
      case 'Escape':
        this.addressDropdownOpen = false;
        this.highlightedAddressIndex = -1;
        break;
    }
  }

  selectAddressSuggestion(suggestion: AddressSuggestion): void {
    this.editAddress1 = suggestion.address1;
    this.editCity = suggestion.city ?? this.editCity;
    this.editState = suggestion.state ?? this.editState;
    this.editZipCode = suggestion.zipCode ?? this.editZipCode;
    this.editLatitude = suggestion.latitude;
    this.editLongitude = suggestion.longitude;

    this.addressDropdownOpen = false;
    this.highlightedAddressIndex = -1;
  }

  onAddress2Input(event: Event): void {
    this.editAddress2 = (event.target as HTMLInputElement).value;
    this.editLatitude = null;
    this.editLongitude = null;
  }

  onBusinessEmailInput(event: Event): void {
    this.editBusinessEmail = (event.target as HTMLInputElement).value;
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

    this.editBusinessPhoneNumber = formatted;
  }

  onCityInput(event: Event): void {
    this.editCity = (event.target as HTMLInputElement).value;
    this.editLatitude = null;
    this.editLongitude = null;
  }

  onStateInput(event: Event): void {
    this.editState = (event.target as HTMLInputElement).value;
    this.editLatitude = null;
    this.editLongitude = null;
  }

  onZipCodeInput(event: Event): void {
    this.editZipCode = (event.target as HTMLInputElement).value;
    this.editLatitude = null;
    this.editLongitude = null;
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
      businessEmail: workspace.businessEmail?.trim() || null,
      businessPhoneNumber: workspace.businessPhoneNumber?.trim() || null,
      address1: workspace.address1?.trim() || '',
      address2: workspace.address2?.trim() || null,
      city: workspace.city?.trim() || '',
      state: workspace.state?.trim() || '',
      zipCode: workspace.zipCode?.trim() || '',
      latitude: null,
      longitude: null,
      businessJobPercentage: 100,
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
          businessEmail: updatedProfile.businessEmail,
          businessPhoneNumber: updatedProfile.businessPhoneNumber,
          businessHours: updatedProfile.businessHours ?? null,
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

    const trimmedAddress1 = this.editAddress1.trim();
    const trimmedAddress2 = this.editAddress2.trim();
    const trimmedCity = this.editCity.trim();
    const trimmedState = this.editState.trim();
    const trimmedZip = this.editZipCode.trim();

    const original = this.originalGeneralInfoAddress ?? {
      address1: '',
      address2: '',
      city: '',
      state: '',
      zipCode: '',
    };

    const addressChanged =
      trimmedAddress1 !== original.address1
      || trimmedAddress2 !== original.address2
      || trimmedCity !== original.city
      || trimmedState !== original.state
      || trimmedZip !== original.zipCode;

    const shouldResolveCoordinates =
      addressChanged
      && this.editLatitude === null
      && this.editLongitude === null
      && trimmedAddress1.length > 0
      && trimmedCity.length > 0
      && trimmedState.length > 0
      && trimmedZip.length > 0;

    if (shouldResolveCoordinates) {
      const suggestionFallback = !this.loadingAddressSuggestions
        && !this.addressAutocompleteError
        && this.addressSuggestions.length > 0
        ? this.addressSuggestions[0]
        : null;

      if (suggestionFallback) {
        this.pendingGeneralInfoAddress = suggestionFallback;
        this.confirmGeneralInfoAddressModalOpen = true;
        this.addressDropdownOpen = false;
        this.highlightedAddressIndex = -1;
        return;
      }

      this.resolvingGeneralInfoAddress = true;
      this.geocodingService.resolveAddress({
        address1: trimmedAddress1,
        address2: trimmedAddress2 || null,
        city: trimmedCity,
        state: trimmedState,
        zipCode: trimmedZip,
        countryCode: null,
      }).pipe(
        catchError((err) => {
          // eslint-disable-next-line no-console
          console.error('Failed to resolve address coordinates.', err);
          return of(null);
        }),
        finalize(() => {
          this.resolvingGeneralInfoAddress = false;
        })
      ).subscribe((result) => {
        if (!result) {
          this.snackBar.open('Could not confirm the address location. Please select an address suggestion.', 'Close', {
            duration: 4000,
            panelClass: ['snack-error'],
          });
          this.addressDropdownOpen = true;
          this.highlightedAddressIndex = this.addressSuggestions.length > 0 ? 0 : -1;
          return;
        }

        this.pendingGeneralInfoAddress = result;
        this.confirmGeneralInfoAddressModalOpen = true;
      });

      return;
    }

    this.performGeneralInformationSave();
  }

  confirmGeneralInfoAddress(useRecommended: boolean): void {
    if (!useRecommended) {
      this.confirmGeneralInfoAddressModalOpen = false;
      this.pendingGeneralInfoAddress = null;
      this.addressDropdownOpen = true;
      this.highlightedAddressIndex = this.addressSuggestions.length > 0 ? 0 : -1;
      return;
    }

    if (this.pendingGeneralInfoAddress) {
      this.editAddress1 = this.pendingGeneralInfoAddress.address1;
      this.editCity = this.pendingGeneralInfoAddress.city ?? this.editCity;
      this.editState = this.pendingGeneralInfoAddress.state ?? this.editState;
      this.editZipCode = this.pendingGeneralInfoAddress.zipCode ?? this.editZipCode;
      this.editLatitude = this.pendingGeneralInfoAddress.latitude;
      this.editLongitude = this.pendingGeneralInfoAddress.longitude;
    }

    this.confirmGeneralInfoAddressModalOpen = false;
    this.pendingGeneralInfoAddress = null;
    this.performGeneralInformationSave();
  }

  private performGeneralInformationSave(): void {
    const workspace = this.businessWorkspace;
    if (!workspace || this.savingGeneralInformation) {
      return;
    }

    this.savingGeneralInformation = true;
    this.businessService.updateBusinessProfile(workspace.businessCode, {
      name: workspace.name,
      description: workspace.description ?? null,
      businessIconBase64: workspace.businessIconBase64 ?? null,
      businessEmail: this.editBusinessEmail.trim() || null,
      businessPhoneNumber: this.editBusinessPhoneNumber.trim() || null,
      businessHours: this.normalizeBusinessHoursDraft(),
      address1: this.editAddress1.trim(),
      address2: this.editAddress2.trim() || null,
      city: this.editCity.trim(),
      state: this.editState.trim(),
      zipCode: this.editZipCode.trim(),
      latitude: this.editLatitude,
      longitude: this.editLongitude,
      businessJobPercentage: 100,
    }).pipe(
      finalize(() => {
        this.savingGeneralInformation = false;
      })
    ).subscribe({
      next: (updatedProfile) => {
        if (!this.businessWorkspace) {
          return;
        }

        this.businessWorkspace = {
          ...this.businessWorkspace,
          name: updatedProfile.name,
          description: updatedProfile.description,
          businessIconBase64: updatedProfile.businessIconBase64,
          businessEmail: updatedProfile.businessEmail,
          businessPhoneNumber: updatedProfile.businessPhoneNumber,
          businessHours: updatedProfile.businessHours ?? null,
          address1: updatedProfile.address1,
          address2: updatedProfile.address2,
          city: updatedProfile.city,
          state: updatedProfile.state,
          zipCode: updatedProfile.zipCode,
          countryName: updatedProfile.countryName,
          businessJobPercentage: updatedProfile.businessJobPercentage,
        };
        this.editingGeneralInformation = false;
        this.resetProfileEditor(this.businessWorkspace);
        this.resetGeneralInformationEditor(this.businessWorkspace);
        this.snackBar.open('General information updated.', 'Close', {
          duration: 2500,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
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

  toggleWorkspaceMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.isWorkspaceMenuOpen = !this.isWorkspaceMenuOpen;
  }

  closeWorkspaceMenu(): void {
    this.isWorkspaceMenuOpen = false;
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

  formatJobWindow(job: Pick<BusinessWorkspaceJob | BusinessWorkspaceJobRequest, 'startDateTime' | 'endDateTime'>): string {
    const start = this.parseBusinessScheduleDate(job.startDateTime);
    const end = this.parseBusinessScheduleDate(job.endDateTime);

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

  formatSubServicePrice(subService: VisibleBusinessSubService): string {
    const price = Number(subService.price ?? 0);
    if (Number.isFinite(price) && price === 0) {
      return 'Consult';
    }

    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(Number.isFinite(price) ? price : 0);
  }
  formatPhoneNumberForDisplay(value?: string | null): string {
    const trimmed = value?.trim() ?? '';
    if (!trimmed) {
      return '';
    }

    const digits = trimmed.replace(/\D/g, '');
    return this.formatPhoneNumberValue(digits, trimmed);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    this.openHoursTimePicker = null;
    const target = event.target as Node | null;
    const element = target instanceof Element ? target : target?.parentElement;
    if (!element?.closest('.auth-actions-logged-in')) {
      this.isUserMenuOpen = false;
    }

    if (!element?.closest('.workspace-mobile-menu')) {
      this.isWorkspaceMenuOpen = false;
    }
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.refreshMobileNav();
  }

  private refreshMobileNav(): void {
    if (typeof window === 'undefined') {
      this.isMobileNav = false;
      return;
    }

    this.isMobileNav = window.innerWidth <= BusinessWorkspaceComponent.MobileNavBreakpointPx;
    if (!this.isMobileNav) {
      this.isWorkspaceMenuOpen = false;
    }
  }

  private loadWorkspace(businessCode: string): void {
    this.loadingWorkspace = true;
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

  private updateJobRequest(workloadId: number, action: 'accept' | 'deny'): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (!businessCode || !this.canManageJobRequests || this.isUpdatingJobRequest(workloadId)) {
      return;
    }

    this.updatingJobRequestIds.add(workloadId);
    const request$ = action === 'accept'
      ? this.businessService.acceptJobRequest(businessCode, workloadId)
      : this.businessService.denyJobRequest(businessCode, workloadId);

    request$
      .pipe(finalize(() => this.updatingJobRequestIds.delete(workloadId)))
      .subscribe({
        next: () => {
          this.snackBar.open(action === 'accept' ? 'Job request accepted.' : 'Job request denied.', 'Close', {
            duration: 3200,
            panelClass: ['snack-success'],
          });
          this.loadWorkspace(businessCode);
        },
        error: (err) => {
          const errors = err?.error?.errors as string[] | undefined;
          this.snackBar.open(errors?.[0] ?? 'Could not update the job request.', 'Close', {
            duration: 3600,
            panelClass: ['snack-error'],
          });
        },
      });
  }

  private configureAvailabilityTimeline(): void {
    const schedule = this.jobRequestAvailability;
    const points: number[] = [];
    if (schedule) {
      [schedule.request, ...(schedule.existingJobs ?? [])].forEach((item) => {
        points.push(this.dateTimeToMinutes(item.startDateTime), this.dateTimeToMinutes(item.endDateTime));
      });
    }
    this.availabilityTeamMembers.forEach((member) => member.timeFrames.forEach((frame) => {
      points.push(this.timeOnlyToMinutes(frame.startTime), this.timeOnlyToMinutes(frame.endTime));
    }));

    const min = points.length > 0 ? Math.min(...points) : 8 * 60;
    const max = points.length > 0 ? Math.max(...points) : 18 * 60;
    this.availabilityTimelineStartMinutes = Math.max(0, Math.floor((min - 60) / 60) * 60);
    this.availabilityTimelineEndMinutes = Math.min(24 * 60, Math.ceil((max + 60) / 60) * 60);
    const startHour = this.availabilityTimelineStartMinutes / 60;
    const endHour = this.availabilityTimelineEndMinutes / 60;
    this.availabilityTimelineHours = Array.from({ length: endHour - startHour + 1 }, (_, index) => startHour + index);
  }

  private dateTimeToMinutes(value: string): number {
    const date = this.parseBusinessScheduleDate(value);
    return date.getHours() * 60 + date.getMinutes();
  }

  private parseBusinessScheduleDate(value: string): Date {
    const match = (value ?? '').match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2}))?/);
    if (!match) {
      return new Date(value);
    }

    return new Date(
      Number(match[1]),
      Number(match[2]) - 1,
      Number(match[3]),
      Number(match[4]),
      Number(match[5]),
      Number(match[6] ?? 0)
    );
  }

  private timeOnlyToMinutes(value: string): number {
    const [hours, minutes] = (value ?? '').split(':').map((part) => Number(part));
    return (Number.isFinite(hours) ? hours : 0) * 60 + (Number.isFinite(minutes) ? minutes : 0);
  }

  private formatTimeOnly(value: string): string {
    const minutes = this.timeOnlyToMinutes(value);
    const date = new Date(2000, 0, 1, Math.floor(minutes / 60), minutes % 60);
    return new Intl.DateTimeFormat('en-US', { hour: 'numeric', minute: '2-digit' }).format(date);
  }

  private toLocalIsoDate(value: Date): string {
    const year = value.getFullYear();
    const month = `${value.getMonth() + 1}`.padStart(2, '0');
    const day = `${value.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.costBreakdownRequest) {
      this.closeCostBreakdown();
      return;
    }

    if (this.jobRequestAvailabilityModalOpen && !this.loadingJobRequestAvailability) {
      this.closeJobRequestAvailability();
      return;
    }

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

    return `data:image/png;base64,${trimmed}`;
  }

  private getTodayDateInputValue(): string {
    const now = new Date();
    const month = `${now.getMonth() + 1}`.padStart(2, '0');
    const day = `${now.getDate()}`.padStart(2, '0');
    return `${now.getFullYear()}-${month}-${day}`;
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
    this.editLatitude = null;
    this.editLongitude = null;
    this.addressSuggestions = [];
    this.addressDropdownOpen = false;
    this.highlightedAddressIndex = -1;
    this.editBusinessEmail = workspace.businessEmail ?? '';
    this.editBusinessPhoneNumber = this.formatPhoneNumberForDisplay(workspace.businessPhoneNumber);
    this.editCity = workspace.city ?? '';
    this.editState = workspace.state ?? '';
    this.editZipCode = workspace.zipCode ?? '';
    this.editBusinessHoursDraft = this.normalizeIncomingBusinessHours(workspace.businessHours);
    this.originalGeneralInfoAddress = {
      address1: (this.editAddress1 ?? '').trim(),
      address2: (this.editAddress2 ?? '').trim(),
      city: (this.editCity ?? '').trim(),
      state: (this.editState ?? '').trim(),
      zipCode: (this.editZipCode ?? '').trim(),
    };
  }

  get dayLabels(): string[] {
    return ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
  }

  get businessHoursSummary(): string {
    const normalized = this.normalizeBusinessHoursDraft();
    if (!normalized || normalized.length === 0) {
      return 'Hours not set';
    }

    const labels = this.dayLabels;
    const parts = normalized
      .filter((x) => !x.isClosed && !!x.openTime && !!x.closeTime)
      .map((x) => `${labels[x.dayOfWeek] ?? 'Day'} ${x.openTime}-${x.closeTime}`);

    return parts.length > 0 ? parts.join(' | ') : 'Hours not set';
  }

  get businessHoursDisplay(): { dayLabel: string; value: string }[] {
    const labels = this.dayLabels;
    const incoming = this.businessWorkspace?.businessHours ?? null;

    const byDay = new Map<number, { openTime: string | null; closeTime: string | null; isClosed: boolean }>();
    for (const entry of incoming ?? []) {
      const day = Number(entry?.dayOfWeek);
      if (!Number.isFinite(day) || day < 0 || day > 6) {
        continue;
      }
      byDay.set(Math.trunc(day), {
        openTime: (entry.openTime ?? '').trim() || null,
        closeTime: (entry.closeTime ?? '').trim() || null,
        isClosed: !!entry.isClosed,
      });
    }

    const result: { dayLabel: string; value: string }[] = [];
    for (let day = 0; day < 7; day++) {
      const label = labels[day] ?? 'Day';
      const entry = byDay.get(day);
      if (!entry) {
        result.push({ dayLabel: label, value: 'Hours not set' });
        continue;
      }

      if (entry.isClosed) {
        result.push({ dayLabel: label, value: 'Closed' });
        continue;
      }

      const open = this.formatTime(entry.openTime);
      const close = this.formatTime(entry.closeTime);
      if (!open || !close) {
        result.push({ dayLabel: label, value: 'Hours not set' });
      } else {
        result.push({ dayLabel: label, value: `${open} – ${close}` });
      }
    }

    return result;
  }

  toggleDayClosed(dayOfWeek: number): void {
    const entry = this.editBusinessHoursDraft.find((x) => x.dayOfWeek === dayOfWeek);
    if (!entry) {
      return;
    }
    entry.isClosed = !entry.isClosed;
  }

  isHoursTimePickerOpen(dayOfWeek: number, field: 'open' | 'close'): boolean {
    return !!this.openHoursTimePicker
      && this.openHoursTimePicker.dayOfWeek === dayOfWeek
      && this.openHoursTimePicker.field === field;
  }

  toggleHoursPicker(dayOfWeek: number, field: 'open' | 'close', event: MouseEvent): void {
    event.stopPropagation();
    if (this.isHoursTimePickerOpen(dayOfWeek, field)) {
      this.openHoursTimePicker = null;
      return;
    }

    this.openHoursTimePicker = { dayOfWeek, field };
  }

  selectHoursTime(dayOfWeek: number, field: 'open' | 'close', value: string): void {
    const entry = this.editBusinessHoursDraft.find((x) => x.dayOfWeek === dayOfWeek);
    if (!entry) {
      return;
    }

    if (field === 'open') {
      entry.openTime = value;
    } else {
      entry.closeTime = value;
    }

    this.openHoursTimePicker = null;
  }

  timeLabel(value: string | null | undefined): string {
    const trimmed = (value ?? '').trim();
    if (!trimmed) {
      return 'Select';
    }

    const match = /^(\d{2}):(\d{2})$/.exec(trimmed);
    if (!match) {
      return trimmed;
    }

    const hours = Number(match[1]);
    const minutes = Number(match[2]);
    if (!Number.isFinite(hours) || !Number.isFinite(minutes)) {
      return trimmed;
    }

    const date = new Date(2000, 0, 1, hours, minutes, 0);
    return new Intl.DateTimeFormat('en-US', { hour: 'numeric', minute: '2-digit' }).format(date);
  }

  private static createDefaultBusinessHours(): BusinessHoursDay[] {
    const days: BusinessHoursDay[] = [];
    for (let dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++) {
      days.push({ dayOfWeek, openTime: '09:00', closeTime: '17:00', isClosed: true });
    }
    return days;
  }

  private static buildTimeOptions(stepMinutes: number): string[] {
    const step = Number.isFinite(stepMinutes) ? Math.max(5, Math.min(60, Math.trunc(stepMinutes))) : 30;
    const options: string[] = [];
    for (let minutes = 0; minutes < 24 * 60; minutes += step) {
      const hh = `${Math.floor(minutes / 60)}`.padStart(2, '0');
      const mm = `${minutes % 60}`.padStart(2, '0');
      options.push(`${hh}:${mm}`);
    }
    return options;
  }

  private normalizeIncomingBusinessHours(value: BusinessHoursDay[] | null | undefined): BusinessHoursDay[] {
    const byDay = new Map<number, BusinessHoursDay>();
    for (const entry of value ?? []) {
      const day = Number(entry?.dayOfWeek);
      if (!Number.isFinite(day) || day < 0 || day > 6) {
        continue;
      }
      byDay.set(Math.trunc(day), {
        dayOfWeek: Math.trunc(day),
        openTime: (entry.openTime ?? '').trim() || '09:00',
        closeTime: (entry.closeTime ?? '').trim() || '17:00',
        isClosed: !!entry.isClosed,
      });
    }

    const result: BusinessHoursDay[] = [];
    for (let dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++) {
      result.push(byDay.get(dayOfWeek) ?? { dayOfWeek, openTime: '09:00', closeTime: '17:00', isClosed: true });
    }
    return result;
  }

  private normalizeBusinessHoursDraft(): BusinessHoursDay[] | null {
    const cleaned = (this.editBusinessHoursDraft ?? [])
      .filter((x) => Number.isFinite(x.dayOfWeek) && x.dayOfWeek >= 0 && x.dayOfWeek <= 6)
      .map((x) => ({
        dayOfWeek: Math.trunc(x.dayOfWeek),
        openTime: (x.openTime ?? '').trim() || null,
        closeTime: (x.closeTime ?? '').trim() || null,
        isClosed: !!x.isClosed,
      }));

    return cleaned.length > 0 ? cleaned : null;
  }

  private formatTime(value: string | null): string {
    const trimmed = (value ?? '').trim();
    if (!trimmed) {
      return '';
    }

    const match = /^(\d{2}):(\d{2})$/.exec(trimmed);
    if (!match) {
      return '';
    }

    const hours = Number(match[1]);
    const minutes = Number(match[2]);
    const date = new Date(2000, 0, 1, hours, minutes, 0);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    return new Intl.DateTimeFormat('en-US', { hour: 'numeric', minute: '2-digit' }).format(date);
  }

  private saveServices(services: BusinessWorkspaceService[]): void {
    const workspace = this.businessWorkspace;
    if (!workspace) {
      return;
    }

    if (services.length === 0) {
      this.snackBar.open('At least one category is required.', 'Close', {
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
        this.snackBar.open('Business categories updated.', 'Close', {
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
        this.snackBar.open(errors?.[0] ?? 'Could not update business categories.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }
}
