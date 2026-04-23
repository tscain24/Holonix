import { Component, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  BusinessService,
  BusinessWorkspaceEmployee,
  BusinessWorkspace,
  BusinessWorkspaceSubService,
} from '../../../core/services/business.service';
import { AuthSessionService } from '../../../core/services/auth-session.service';

interface VisibleBusinessSubService extends BusinessWorkspaceSubService {
  parentServiceName: string;
}

type SubServiceFilter = {
  field: 'subService' | 'parentService' | 'assignedUser' | 'pricing';
  operator: 'contains' | 'is';
  value: string;
};

@Component({
  selector: 'app-business-service-manager',
  templateUrl: './business-service-manager.component.html',
  styleUrls: ['./business-service-manager.component.css'],
})
export class BusinessServiceManagerComponent implements OnInit {
  private static readonly MaxVisibleAssignedUsers = 5;
  private static readonly SubServicePageSize = 10;
  readonly subServicePricingOptions = ['Consult', 'Priced'];
  readonly durationMinuteOptions = ['0', '15', '30', '45'];
  displayName = 'User';
  initials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;
  loadingWorkspace = true;
  creatingSubService = false;
  savingSubService = false;
  deletingSubServiceIds = new Set<number>();
  createSubServiceOpen = false;
  openSubServiceMenuId: number | null = null;
  subServiceMenuTop = 0;
  subServiceMenuLeft = 0;
  selectedSubServiceForMenu: VisibleBusinessSubService | null = null;
  filterMenuOpen = false;
  editingSubServiceId: number | null = null;
  businessWorkspace: BusinessWorkspace | null = null;
  currentSubServicePage = 1;
  activeFilters: SubServiceFilter[] = [];
  draftFilters: SubServiceFilter[] = [{ field: 'subService', operator: 'contains', value: '' }];
  createSubServiceName = '';
  createParentServiceId: number | null = null;
  createSubServicePrice = '0';
  createConsultationNeeded = false;
  createDurationHours = '0';
  createDurationMinutes = '15';
  createEmployeeCount = '1';
  createEffectiveDate = '';
  createSubServiceDescription = '';
  selectedAssignedUserIds = new Set<number>();
  editSubServiceName = '';
  editParentServiceId: number | null = null;
  editSubServicePrice = '0';
  editConsultationNeeded = false;
  editDurationHours = '0';
  editDurationMinutes = '15';
  editEmployeeCount = '1';
  editEffectiveDate = '';
  editOriginalEffectiveDate = '';
  editSubServiceDescription = '';
  selectedEditAssignedUserIds = new Set<number>();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
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
        this.applyWorkspace(workspace);
        this.loadingWorkspace = false;
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

        this.snackBar.open('Could not load the service manager.', 'Close', {
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

  goToWorkspace(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (businessCode) {
      this.router.navigate(['/business', businessCode]);
    }
  }

  goToOverviewTab(): void {
    this.goToWorkspace();
  }

  goToEmployeesTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (businessCode) {
      this.router.navigate(['/business', businessCode, 'employees']);
    }
  }

  goToServiceManagerTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (businessCode) {
      this.router.navigate(['/business', businessCode, 'services']);
    }
  }

  goToMyAvailabilityTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (businessCode) {
      this.router.navigate(['/business', businessCode, 'availability']);
    }
  }

  goToJobsTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (businessCode) {
      this.router.navigate(['/business', businessCode], { fragment: 'jobs' });
    }
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

  get canManageSubServices(): boolean {
    const role = (this.businessWorkspace?.currentUserRoleName ?? '').trim().toLowerCase();
    return role === 'owner' || role === 'administrator';
  }

  get activeAssignableEmployees(): BusinessWorkspaceEmployee[] {
    return (this.businessWorkspace?.employees ?? []).filter((employee) => employee.isActive);
  }

  get canCreateSubService(): boolean {
    const trimmedName = this.createSubServiceName.trim();
    const parsedPrice = Number(this.createSubServicePrice);
    const durationMinutes = this.getCreateDurationMinutes();
    const parsedEmployeeCount = Number(this.createEmployeeCount);
    const today = this.todayDateInputValue;
    const effectiveDate = this.createEffectiveDate.trim();
    return !!this.createParentServiceId
      && trimmedName.length > 0
      && effectiveDate.length > 0
      && effectiveDate >= today
      && Number.isFinite(parsedPrice)
      && parsedPrice >= 0
      && (parsedPrice > 0 || this.createConsultationNeeded)
      && durationMinutes > 0
      && durationMinutes % 15 === 0
      && Number.isInteger(parsedEmployeeCount)
      && parsedEmployeeCount > 0
      && !this.creatingSubService;
  }

  get canSaveEditedSubService(): boolean {
    const trimmedName = this.editSubServiceName.trim();
    const parsedPrice = Number(this.editSubServicePrice);
    const durationMinutes = this.getEditDurationMinutes();
    const parsedEmployeeCount = Number(this.editEmployeeCount);
    const effectiveDate = this.editEffectiveDate.trim();
    const canUseEffectiveDate = effectiveDate.length > 0
      && (effectiveDate >= this.todayDateInputValue || effectiveDate === this.editOriginalEffectiveDate);
    return !!this.editingSubServiceId
      && !!this.editParentServiceId
      && trimmedName.length > 0
      && canUseEffectiveDate
      && Number.isFinite(parsedPrice)
      && parsedPrice >= 0
      && (parsedPrice > 0 || this.editConsultationNeeded)
      && durationMinutes > 0
      && durationMinutes % 15 === 0
      && Number.isInteger(parsedEmployeeCount)
      && parsedEmployeeCount > 0
      && !this.savingSubService;
  }

  get allVisibleSubServices(): VisibleBusinessSubService[] {
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
      });
  }

  get filteredSubServices(): VisibleBusinessSubService[] {
    return this.allVisibleSubServices.filter((subService) =>
      this.activeFilters.every((filter) => this.matchesFilter(subService, filter)));
  }

  get pagedSubServices(): VisibleBusinessSubService[] {
    const start = (this.currentSubServicePage - 1) * BusinessServiceManagerComponent.SubServicePageSize;
    return this.filteredSubServices.slice(start, start + BusinessServiceManagerComponent.SubServicePageSize);
  }

  get subServicePageStart(): number {
    if (this.filteredSubServices.length === 0) {
      return 0;
    }

    return (this.currentSubServicePage - 1) * BusinessServiceManagerComponent.SubServicePageSize + 1;
  }

  get subServicePageEnd(): number {
    return Math.min(
      this.currentSubServicePage * BusinessServiceManagerComponent.SubServicePageSize,
      this.filteredSubServices.length
    );
  }

  get hasPreviousSubServicePage(): boolean {
    return this.currentSubServicePage > 1;
  }

  get hasNextSubServicePage(): boolean {
    return this.subServicePageEnd < this.filteredSubServices.length;
  }

  get hasActiveFilters(): boolean {
    return this.activeFilters.length > 0;
  }

  get canApplyDraftFilters(): boolean {
    return this.draftFilters.some((filter) => filter.value.trim().length > 0);
  }

  toggleCreateSubService(): void {
    this.createSubServiceOpen = !this.createSubServiceOpen;
  }

  toggleFilterMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.filterMenuOpen = !this.filterMenuOpen;
  }

  addDraftFilter(): void {
    this.draftFilters = [...this.draftFilters, { field: 'subService', operator: 'contains', value: '' }];
  }

  updateDraftFilterField(index: number, field: SubServiceFilter['field']): void {
    this.draftFilters = this.draftFilters.map((filter, filterIndex) =>
      filterIndex === index
        ? {
            ...filter,
            field,
            operator: field === 'pricing' ? 'is' : filter.operator,
            value: field === 'pricing' && !this.subServicePricingOptions.includes(filter.value) ? '' : filter.value,
          }
        : filter
    );
  }

  updateDraftFilterOperator(index: number, operator: SubServiceFilter['operator']): void {
    this.draftFilters = this.draftFilters.map((filter, filterIndex) =>
      filterIndex === index ? { ...filter, operator } : filter
    );
  }

  updateDraftFilterValue(index: number, value: string): void {
    this.draftFilters = this.draftFilters.map((filter, filterIndex) =>
      filterIndex === index ? { ...filter, value } : filter
    );
  }

  removeDraftFilter(index: number): void {
    this.draftFilters = this.draftFilters.filter((_, filterIndex) => filterIndex !== index);
    if (this.draftFilters.length === 0) {
      this.draftFilters = [{ field: 'subService', operator: 'contains', value: '' }];
    }
  }

  applyFilters(): void {
    this.activeFilters = this.draftFilters
      .map((filter) => ({ ...filter, value: filter.value.trim() }))
      .filter((filter) => filter.value.length > 0);
    this.filterMenuOpen = false;
    this.currentSubServicePage = 1;
    this.closeSubServiceMenu();
  }

  clearAllFilters(): void {
    this.activeFilters = [];
    this.draftFilters = [{ field: 'subService', operator: 'contains', value: '' }];
    this.filterMenuOpen = false;
    this.currentSubServicePage = 1;
  }

  removeFilter(index: number): void {
    this.activeFilters = this.activeFilters.filter((_, filterIndex) => filterIndex !== index);
    this.draftFilters = this.activeFilters.length > 0
      ? this.activeFilters.map((filter) => ({ ...filter }))
      : [{ field: 'subService', operator: 'contains', value: '' }];
    this.currentSubServicePage = 1;
  }

  goToPreviousSubServicePage(): void {
    if (!this.hasPreviousSubServicePage) {
      return;
    }

    this.currentSubServicePage -= 1;
    this.closeSubServiceMenu();
  }

  goToNextSubServicePage(): void {
    if (!this.hasNextSubServicePage) {
      return;
    }

    this.currentSubServicePage += 1;
    this.closeSubServiceMenu();
  }

  toggleSubServiceMenu(event: MouseEvent, subService: VisibleBusinessSubService): void {
    event.stopPropagation();
    if (this.openSubServiceMenuId === subService.businessSubServiceId) {
      this.closeSubServiceMenu();
      return;
    }

    const trigger = event.currentTarget as HTMLElement | null;
    const rect = trigger?.getBoundingClientRect();
    if (!rect) {
      return;
    }

    this.openSubServiceMenuId = subService.businessSubServiceId;
    this.selectedSubServiceForMenu = subService;
    this.subServiceMenuTop = rect.bottom + 8;
    this.subServiceMenuLeft = Math.max(16, rect.right - 160);
  }

  isSubServiceMenuOpen(businessSubServiceId: number): boolean {
    return this.openSubServiceMenuId === businessSubServiceId;
  }

  closeSubServiceMenu(): void {
    this.openSubServiceMenuId = null;
    this.selectedSubServiceForMenu = null;
  }

  onSubServiceNameInput(event: Event): void {
    this.createSubServiceName = (event.target as HTMLInputElement).value;
  }

  onParentServiceChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.createParentServiceId = Number.isFinite(value) && value > 0 ? value : null;
  }

  onSubServicePriceInput(event: Event): void {
    this.createSubServicePrice = (event.target as HTMLInputElement).value;
  }

  onCreateConsultationNeededChange(event: Event): void {
    this.createConsultationNeeded = (event.target as HTMLInputElement).checked;
    if (this.createConsultationNeeded) {
      this.createSubServicePrice = '0.00';
    }
  }

  onEmployeeCountInput(event: Event): void {
    this.createEmployeeCount = (event.target as HTMLInputElement).value;
  }

  onCreateDurationHoursInput(event: Event): void {
    this.createDurationHours = (event.target as HTMLInputElement).value;
  }

  onCreateDurationMinutesChange(event: Event): void {
    this.createDurationMinutes = (event.target as HTMLSelectElement).value;
  }

  preventNumberScroll(event: WheelEvent): void {
    event.preventDefault();
    (event.target as HTMLInputElement | null)?.blur();
  }

  onEffectiveDateInput(event: Event): void {
    this.createEffectiveDate = (event.target as HTMLInputElement).value;
  }

  onSubServiceDescriptionInput(event: Event): void {
    this.createSubServiceDescription = (event.target as HTMLTextAreaElement).value;
  }

  isAssignedUserSelected(businessUserId: number): boolean {
    return this.selectedAssignedUserIds.has(businessUserId);
  }

  toggleAssignedUser(businessUserId: number): void {
    if (this.selectedAssignedUserIds.has(businessUserId)) {
      this.selectedAssignedUserIds.delete(businessUserId);
      return;
    }

    this.selectedAssignedUserIds.add(businessUserId);
  }

  isEditAssignedUserSelected(businessUserId: number): boolean {
    return this.selectedEditAssignedUserIds.has(businessUserId);
  }

  toggleEditAssignedUser(businessUserId: number): void {
    if (this.selectedEditAssignedUserIds.has(businessUserId)) {
      this.selectedEditAssignedUserIds.delete(businessUserId);
      return;
    }

    this.selectedEditAssignedUserIds.add(businessUserId);
  }

  createSubService(): void {
    const workspace = this.businessWorkspace;
    const businessCode = workspace?.businessCode?.trim();
    const serviceId = this.createParentServiceId;
    const name = this.createSubServiceName.trim();
    const description = this.createSubServiceDescription.trim();
    const price = Number(this.createSubServicePrice);
    const durationMinutes = this.getCreateDurationMinutes();
    const employeeCount = Number(this.createEmployeeCount);
    const effectiveDate = this.createEffectiveDate.trim();

    if (!workspace || !businessCode || !serviceId || !this.canManageSubServices || this.creatingSubService) {
      return;
    }

    if (!name) {
      this.snackBar.open('Service name is required.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    if (!Number.isFinite(price) || price < 0) {
      this.snackBar.open('Price must be zero or greater.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    if (price === 0 && !this.createConsultationNeeded) {
      this.snackBar.open('Price can only be $0 when Consultation Needed is checked.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    if (!Number.isInteger(durationMinutes) || durationMinutes <= 0 || durationMinutes % 15 !== 0) {
      this.snackBar.open('Duration must be greater than zero and use 15-minute intervals.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    if (!Number.isInteger(employeeCount) || employeeCount <= 0) {
      this.snackBar.open('Employees needed must be a whole number greater than zero.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    if (!effectiveDate) {
      this.snackBar.open('Effective date is required.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    if (effectiveDate < this.todayDateInputValue) {
      this.snackBar.open('Effective date cannot be in the past.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    this.creatingSubService = true;
    this.businessService.createBusinessSubService(businessCode, serviceId, {
      name,
      description: description || null,
      consultationNeeded: this.createConsultationNeeded,
      durationMinutes,
      price,
      employeeCount,
      assignedBusinessUserIds: Array.from(this.selectedAssignedUserIds),
      effectiveDate,
    }).subscribe({
      next: () => {
        this.createSubServiceName = '';
        this.createSubServicePrice = '0';
        this.createConsultationNeeded = false;
        this.createDurationHours = '0';
        this.createDurationMinutes = '15';
        this.createEmployeeCount = '1';
        this.createEffectiveDate = this.todayDateInputValue;
        this.createSubServiceDescription = '';
        this.selectedAssignedUserIds.clear();
        this.createSubServiceOpen = false;
        this.reloadWorkspace(businessCode, 'Service created.');
      },
      error: (err) => {
        this.creatingSubService = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = (err as { error?: { errors?: unknown } } | null | undefined)?.error?.errors;
        const messages = Array.isArray(errors) ? errors.filter((value): value is string => typeof value === 'string') : [];
        this.snackBar.open(messages[0] ?? 'Could not create service.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  isDeletingSubService(businessSubServiceId: number): boolean {
    return this.deletingSubServiceIds.has(businessSubServiceId);
  }

  startEditSubService(subService: VisibleBusinessSubService): void {
    this.closeSubServiceMenu();
    this.editingSubServiceId = subService.businessSubServiceId;
    this.editSubServiceName = subService.name;
    this.editParentServiceId = this.findParentServiceId(subService.businessSubServiceId);
    this.editSubServicePrice = `${subService.price ?? 0}`;
    this.editConsultationNeeded = !!subService.consultationNeeded;
    this.editDurationHours = `${Math.floor((subService.durationMinutes ?? 0) / 60)}`;
    this.editDurationMinutes = `${(subService.durationMinutes ?? 0) % 60}`;
    this.editEmployeeCount = `${subService.employeeCount ?? 1}`;
    this.editEffectiveDate = subService.effectiveDate ?? '';
    this.editOriginalEffectiveDate = subService.effectiveDate ?? '';
    this.editSubServiceDescription = subService.description ?? '';
    this.selectedEditAssignedUserIds = new Set((subService.assignedUsers ?? []).map((user) => user.businessUserId));
  }

  cancelEditSubService(): void {
    this.editingSubServiceId = null;
    this.editSubServiceName = '';
    this.editParentServiceId = null;
    this.editSubServicePrice = '0';
    this.editConsultationNeeded = false;
    this.editDurationHours = '0';
    this.editDurationMinutes = '15';
    this.editEmployeeCount = '1';
    this.editEffectiveDate = '';
    this.editOriginalEffectiveDate = '';
    this.editSubServiceDescription = '';
    this.selectedEditAssignedUserIds.clear();
    this.savingSubService = false;
  }

  onEditSubServiceNameInput(event: Event): void {
    this.editSubServiceName = (event.target as HTMLInputElement).value;
  }

  onEditParentServiceChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.editParentServiceId = Number.isFinite(value) && value > 0 ? value : null;
  }

  onEditSubServicePriceInput(event: Event): void {
    this.editSubServicePrice = (event.target as HTMLInputElement).value;
  }

  onEditConsultationNeededChange(event: Event): void {
    this.editConsultationNeeded = (event.target as HTMLInputElement).checked;
    if (this.editConsultationNeeded) {
      this.editSubServicePrice = '0.00';
    }
  }

  onEditEmployeeCountInput(event: Event): void {
    this.editEmployeeCount = (event.target as HTMLInputElement).value;
  }

  onEditDurationHoursInput(event: Event): void {
    this.editDurationHours = (event.target as HTMLInputElement).value;
  }

  onEditDurationMinutesChange(event: Event): void {
    this.editDurationMinutes = (event.target as HTMLSelectElement).value;
  }

  onEditEffectiveDateInput(event: Event): void {
    this.editEffectiveDate = (event.target as HTMLInputElement).value;
  }

  onEditSubServiceDescriptionInput(event: Event): void {
    this.editSubServiceDescription = (event.target as HTMLTextAreaElement).value;
  }

  saveEditedSubService(): void {
    const workspace = this.businessWorkspace;
    const businessCode = workspace?.businessCode?.trim();
    const businessSubServiceId = this.editingSubServiceId;
    const serviceId = this.editParentServiceId;
    const name = this.editSubServiceName.trim();
    const description = this.editSubServiceDescription.trim();
    const price = Number(this.editSubServicePrice);
    const durationMinutes = this.getEditDurationMinutes();
    const employeeCount = Number(this.editEmployeeCount);
    const effectiveDate = this.editEffectiveDate.trim();
    const canUseEffectiveDate = effectiveDate.length > 0
      && (effectiveDate >= this.todayDateInputValue || effectiveDate === this.editOriginalEffectiveDate);

    if (!workspace || !businessCode || !businessSubServiceId || !serviceId || this.savingSubService) {
      return;
    }

    if (!name
      || !effectiveDate
      || !Number.isFinite(price)
      || price < 0
      || (price === 0 && !this.editConsultationNeeded)
      || !Number.isInteger(durationMinutes)
      || durationMinutes <= 0
      || durationMinutes % 15 !== 0
      || !Number.isInteger(employeeCount)
      || employeeCount <= 0
      || !canUseEffectiveDate) {
      return;
    }

    this.savingSubService = true;
    this.businessService.updateBusinessSubService(businessCode, businessSubServiceId, {
      name,
      serviceId,
      description: description || null,
      consultationNeeded: this.editConsultationNeeded,
      durationMinutes,
      price,
      employeeCount,
      assignedBusinessUserIds: Array.from(this.selectedEditAssignedUserIds),
      effectiveDate,
    }).subscribe({
      next: () => {
        this.cancelEditSubService();
        this.reloadWorkspace(businessCode, 'Service updated.');
      },
      error: (err) => {
        this.savingSubService = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = (err as { error?: { errors?: unknown } } | null | undefined)?.error?.errors;
        const messages = Array.isArray(errors) ? errors.filter((value): value is string => typeof value === 'string') : [];
        this.snackBar.open(messages[0] ?? 'Could not update service.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  removeSubService(subService: VisibleBusinessSubService): void {
    const workspace = this.businessWorkspace;
    const businessCode = workspace?.businessCode?.trim();
    if (!workspace || !businessCode || !this.canManageSubServices || this.isDeletingSubService(subService.businessSubServiceId)) {
      return;
    }

    this.deletingSubServiceIds.add(subService.businessSubServiceId);
    this.closeSubServiceMenu();
    this.businessService.deleteBusinessSubService(businessCode, subService.businessSubServiceId).subscribe({
      next: () => {
        if (!this.businessWorkspace) {
          return;
        }

        this.businessWorkspace = {
          ...this.businessWorkspace,
          services: this.businessWorkspace.services.map((service) => ({
            ...service,
            subServices: service.subServices.filter((entry) => entry.businessSubServiceId !== subService.businessSubServiceId),
          })),
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

        const errors = (err as { error?: { errors?: unknown } } | null | undefined)?.error?.errors;
        const messages = Array.isArray(errors) ? errors.filter((value): value is string => typeof value === 'string') : [];
        this.snackBar.open(messages[0] ?? 'Could not remove service.', 'Close', {
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
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value || 0);
  }

  formatSubServicePrice(subService: VisibleBusinessSubService): string {
    const candidate = subService as VisibleBusinessSubService & {
      Price?: number | string;
    };
    const rawPrice = candidate.price ?? candidate.Price ?? 0;
    const price = typeof rawPrice === 'number' ? rawPrice : Number(rawPrice);

    if (Number.isFinite(price) && price === 0) {
      return 'Consult';
    }

    return this.formatMoney(price);
  }

  formatAssignedUsers(subService: VisibleBusinessSubService): string {
    const candidate = subService as VisibleBusinessSubService & {
      AssignedUsers?: Array<{ displayName?: string; DisplayName?: string }>;
      Description?: string | null;
      EffectiveDate?: string;
    };
    const assignedUsers = (candidate.assignedUsers ?? candidate.AssignedUsers ?? []) as Array<{ displayName?: string; DisplayName?: string }>;
    if (assignedUsers.length === 0) {
      return 'Unassigned';
    }

    const names = assignedUsers
      .map((user) => user.displayName ?? user.DisplayName ?? '')
      .filter((value) => value.trim().length > 0)
;

    if (names.length === 0) {
      return 'Unassigned';
    }

    const visibleNames = names.slice(0, BusinessServiceManagerComponent.MaxVisibleAssignedUsers);
    const remainingCount = names.length - visibleNames.length;

    return remainingCount > 0
      ? `${visibleNames.join(', ')} +${remainingCount} more`
      : visibleNames.join(', ');
  }

  formatSubServiceDescription(subService: VisibleBusinessSubService): string {
    const candidate = subService as VisibleBusinessSubService & { Description?: string | null };
    const value = ((candidate.description ?? candidate.Description ?? '') as string).trim();
    return value || 'Not provided';
  }

  formatEmployeeCount(subService: VisibleBusinessSubService): string {
    const candidate = subService as VisibleBusinessSubService & { EmployeeCount?: number };
    const value = candidate.employeeCount ?? candidate.EmployeeCount ?? 0;
    return Number.isFinite(value) && value > 0 ? `${value}` : 'Not set';
  }

  formatDuration(subService: VisibleBusinessSubService): string {
    const candidate = subService as VisibleBusinessSubService & { DurationMinutes?: number };
    const totalMinutes = candidate.durationMinutes ?? candidate.DurationMinutes ?? 0;
    if (!Number.isFinite(totalMinutes) || totalMinutes <= 0) {
      return 'Not set';
    }

    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;
    const parts: string[] = [];

    if (hours > 0) {
      parts.push(`${hours}h`);
    }

    if (minutes > 0) {
      parts.push(`${minutes}m`);
    }

    return parts.join(' ') || 'Not set';
  }

  isConsultationNeeded(subService: VisibleBusinessSubService): boolean {
    const candidate = subService as VisibleBusinessSubService & { ConsultationNeeded?: boolean };
    return !!(candidate.consultationNeeded ?? candidate.ConsultationNeeded);
  }

  formatSubServiceEffectiveDate(subService: VisibleBusinessSubService): string {
    const candidate = subService as VisibleBusinessSubService & { EffectiveDate?: string };
    const value = ((candidate.effectiveDate ?? candidate.EffectiveDate ?? '') as string).trim();
    return this.formatEffectiveDate(value);
  }

  get todayDateInputValue(): string {
    return this.buildTodayDateInputValue();
  }

  hasVisibleSubServices(): boolean {
    return this.filteredSubServices.length > 0;
  }

  filterFieldLabel(field: SubServiceFilter['field']): string {
    if (field === 'subService') {
      return 'Service';
    }

    if (field === 'parentService') {
      return 'Category';
    }

    if (field === 'assignedUser') {
      return 'Assigned User';
    }

    return 'Pricing';
  }

  filterOperatorLabel(operator: SubServiceFilter['operator']): string {
    return operator === 'is' ? 'is' : 'contains';
  }

  filterPillLabel(filter: SubServiceFilter): string {
    return `${this.filterFieldLabel(filter.field)} ${this.filterOperatorLabel(filter.operator)} ${filter.value}`;
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

    if (!element?.closest('.sub-service-actions') && !element?.closest('.sub-service-menu-overlay')) {
      this.closeSubServiceMenu();
    }

    if (!element?.closest('.table-toolbar-menu') && !element?.closest('.toolbar-btn')) {
      this.filterMenuOpen = false;
    }
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.closeSubServiceMenu();
  }

  @HostListener('document:scroll')
  onDocumentScroll(): void {
    this.closeSubServiceMenu();
  }

  @HostListener('document:wheel')
  onDocumentWheel(): void {
    this.closeSubServiceMenu();
  }

  @HostListener('document:touchmove')
  onDocumentTouchMove(): void {
    this.closeSubServiceMenu();
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.closeSubServiceMenu();
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

  private applyWorkspace(workspace: BusinessWorkspace): void {
    this.businessWorkspace = workspace;
    this.createParentServiceId = workspace.services[0]?.serviceId ?? null;
    this.createEffectiveDate = this.todayDateInputValue;
    const maxPage = Math.max(1, Math.ceil(this.filteredSubServices.length / BusinessServiceManagerComponent.SubServicePageSize));
    this.currentSubServicePage = Math.min(this.currentSubServicePage, maxPage);
  }

  private reloadWorkspace(businessCode: string, successMessage?: string): void {
    this.businessService.getBusinessWorkspace(businessCode).subscribe({
      next: (workspace) => {
        this.applyWorkspace(workspace);
        this.creatingSubService = false;
        if (successMessage) {
          this.snackBar.open(successMessage, undefined, {
            duration: 2500,
            panelClass: ['snack-success'],
          });
        }
      },
      error: (err) => {
        this.creatingSubService = false;
        this.savingSubService = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = (err as { error?: { errors?: unknown } } | null | undefined)?.error?.errors;
        const messages = Array.isArray(errors) ? errors.filter((value): value is string => typeof value === 'string') : [];
        this.snackBar.open(messages[0] ?? 'Service saved, but the page could not refresh.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  private findParentServiceId(businessSubServiceId: number): number | null {
    const service = (this.businessWorkspace?.services ?? []).find((item) =>
      item.subServices.some((subService) => subService.businessSubServiceId === businessSubServiceId));
    return service?.serviceId ?? null;
  }

  private buildTodayDateInputValue(): string {
    const now = new Date();
    const month = `${now.getMonth() + 1}`.padStart(2, '0');
    const day = `${now.getDate()}`.padStart(2, '0');
    return `${now.getFullYear()}-${month}-${day}`;
  }

  private getCreateDurationMinutes(): number {
    const hours = Number(this.createDurationHours);
    const minutes = Number(this.createDurationMinutes);
    return (Number.isFinite(hours) ? Math.max(0, Math.floor(hours)) : 0)
      * 60
      + (Number.isFinite(minutes) ? minutes : 0);
  }

  private getEditDurationMinutes(): number {
    const hours = Number(this.editDurationHours);
    const minutes = Number(this.editDurationMinutes);
    return (Number.isFinite(hours) ? Math.max(0, Math.floor(hours)) : 0)
      * 60
      + (Number.isFinite(minutes) ? minutes : 0);
  }

  private matchesFilter(subService: VisibleBusinessSubService, filter: SubServiceFilter): boolean {
    const normalizedValue = filter.value.trim().toLowerCase();
    if (!normalizedValue) {
      return true;
    }

    let candidate = '';
    if (filter.field === 'subService') {
      candidate = subService.name;
    } else if (filter.field === 'parentService') {
      candidate = subService.parentServiceName;
    } else if (filter.field === 'assignedUser') {
      candidate = this.formatAssignedUsers(subService);
    } else {
      candidate = this.formatSubServicePrice(subService);
    }

    const normalizedCandidate = candidate.trim().toLowerCase();
    return filter.operator === 'is'
      ? normalizedCandidate === normalizedValue
      : normalizedCandidate.includes(normalizedValue);
  }
}
