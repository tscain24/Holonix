import { Component, ElementRef, HostListener, OnInit } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import {
  BusinessEmployeeInvite,
  BusinessService,
  BusinessWorkspace,
  BusinessWorkspaceEmployeePage,
  BusinessWorkspaceEmployee,
} from '../../../core/services/business.service';

@Component({
  selector: 'app-business-employees',
  templateUrl: './business-employees.component.html',
  styleUrls: ['./business-employees.component.css'],
})
export class BusinessEmployeesComponent implements OnInit {
  private static readonly EmployeePageSize = 10;
  displayName = 'User';
  initials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;
  loadingWorkspace = true;
  loadingInvites = true;
  loadingEmployees = true;
  inviteModalOpen = false;
  roleModalOpen = false;
  sendingInvite = false;
  updatingEmployeeRole = false;
  closingInviteIds = new Set<number>();
  deactivatingEmployeeIds = new Set<number>();
  openEmployeeMenuId: number | null = null;
  selectedEmployeeForMenu: BusinessWorkspaceEmployee | null = null;
  employeeMenuTop = 0;
  employeeMenuLeft = 0;
  selectedEmployeeForRoleUpdate: BusinessWorkspaceEmployee | null = null;
  businessWorkspace: BusinessWorkspace | null = null;
  employees: BusinessWorkspaceEmployee[] = [];
  employeeInvites: BusinessEmployeeInvite[] = [];
  currentEmployeePage = 1;
  totalEmployeeCount = 0;
  readonly inviteEmailControl = new FormControl('', {
    validators: [Validators.required, Validators.email, Validators.maxLength(256)],
    nonNullable: true,
  });
  readonly inviteRoleControl = new FormControl('', {
    validators: [Validators.required],
    nonNullable: true,
  });
  readonly employeeRoleControl = new FormControl('', {
    validators: [Validators.required],
    nonNullable: true,
  });

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
        this.setDefaultInviteRole(workspace);
        this.loadingWorkspace = false;
        this.loadEmployeeInvites(workspace.businessId);
        this.loadEmployeesPage(workspace.businessId, 1);
      },
      error: (err) => {
        this.loadingWorkspace = false;
        this.loadingInvites = false;
        this.loadingEmployees = false;

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

        this.snackBar.open('Could not load employees for this business.', 'Close', {
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
    const businessId = this.businessWorkspace?.businessId;
    if (businessId) {
      this.router.navigate(['/business', businessId]);
    }
  }

  goToProfile(): void {
    this.router.navigate(['/profile']);
  }

  get canInviteEmployees(): boolean {
    const role = (this.businessWorkspace?.currentUserRoleName ?? '').trim().toLowerCase();
    return role === 'owner' || role === 'administrator';
  }

  get canSubmitInvite(): boolean {
    return this.inviteEmailControl.valid && this.inviteRoleControl.valid && !this.sendingInvite;
  }

  toggleUserMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.isUserMenuOpen = !this.isUserMenuOpen;
  }

  signOut(): void {
    this.authSession.clearSession();
    this.router.navigate(['/login']);
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

  trackEmployee(_: number, employee: BusinessWorkspaceEmployee): number {
    return employee.businessUserId;
  }

  trackInvite(_: number, invite: BusinessEmployeeInvite): number {
    return invite.workloadId;
  }

  inviteStatusClass(status: string): string {
    const normalizedStatus = status.trim().toLowerCase();
    if (normalizedStatus === 'active') {
      return 'status-chip-active';
    }

    if (normalizedStatus === 'denied') {
      return 'status-chip-denied';
    }

    return 'status-chip-pending';
  }

  canCloseInvite(invite: BusinessEmployeeInvite): boolean {
    const normalizedStatus = invite.status.trim().toLowerCase();
    return this.canInviteEmployees && (normalizedStatus === 'pending invite' || normalizedStatus === 'denied');
  }

  isClosingInvite(workloadId: number): boolean {
    return this.closingInviteIds.has(workloadId);
  }

  canDeactivateEmployee(employee: BusinessWorkspaceEmployee): boolean {
    return employee.canDeactivate && employee.isActive;
  }

  canUpdateEmployeeRole(employee: BusinessWorkspaceEmployee): boolean {
    return employee.canUpdateRole && employee.isActive;
  }

  hasEmployeeActions(employee: BusinessWorkspaceEmployee): boolean {
    return this.canUpdateEmployeeRole(employee) || this.canDeactivateEmployee(employee);
  }

  isDeactivatingEmployee(businessUserId: number): boolean {
    return this.deactivatingEmployeeIds.has(businessUserId);
  }

  get canSubmitEmployeeRoleUpdate(): boolean {
    return this.employeeRoleControl.valid && !this.updatingEmployeeRole;
  }

  get employeePageStart(): number {
    if (this.totalEmployeeCount === 0) {
      return 0;
    }

    return (this.currentEmployeePage - 1) * BusinessEmployeesComponent.EmployeePageSize + 1;
  }

  get employeePageEnd(): number {
    return Math.min(this.currentEmployeePage * BusinessEmployeesComponent.EmployeePageSize, this.totalEmployeeCount);
  }

  get hasPreviousEmployeePage(): boolean {
    return this.currentEmployeePage > 1;
  }

  get hasNextEmployeePage(): boolean {
    return this.employeePageEnd < this.totalEmployeeCount;
  }

  toggleEmployeeMenu(event: MouseEvent, employee: BusinessWorkspaceEmployee): void {
    event.stopPropagation();
    if (this.openEmployeeMenuId === employee.businessUserId) {
      this.closeEmployeeMenu();
      return;
    }

    const trigger = event.currentTarget as HTMLElement | null;
    const rect = trigger?.getBoundingClientRect();
    if (!rect) {
      return;
    }

    this.openEmployeeMenuId = employee.businessUserId;
    this.selectedEmployeeForMenu = employee;
    this.employeeMenuTop = rect.bottom + 8;
    this.employeeMenuLeft = Math.max(16, rect.right - 160);
  }

  openUpdateRoleModal(employee: BusinessWorkspaceEmployee): void {
    if (!this.canUpdateEmployeeRole(employee) || this.updatingEmployeeRole) {
      return;
    }

    this.openEmployeeMenuId = null;
    this.selectedEmployeeForMenu = null;
    this.selectedEmployeeForRoleUpdate = employee;
    this.employeeRoleControl.setValue(employee.roleName?.trim() || '');
    this.employeeRoleControl.markAsPristine();
    this.employeeRoleControl.markAsUntouched();
    this.roleModalOpen = true;
  }

  closeUpdateRoleModal(): void {
    if (this.updatingEmployeeRole) {
      return;
    }

    this.roleModalOpen = false;
    this.selectedEmployeeForRoleUpdate = null;
    this.employeeRoleControl.setValue('');
  }

  closeEmployeeMenu(): void {
    this.openEmployeeMenuId = null;
    this.selectedEmployeeForMenu = null;
  }

  goToPreviousEmployeePage(): void {
    const businessId = this.businessWorkspace?.businessId;
    if (!businessId || !this.hasPreviousEmployeePage || this.loadingEmployees) {
      return;
    }

    this.loadEmployeesPage(businessId, this.currentEmployeePage - 1);
  }

  goToNextEmployeePage(): void {
    const businessId = this.businessWorkspace?.businessId;
    if (!businessId || !this.hasNextEmployeePage || this.loadingEmployees) {
      return;
    }

    this.loadEmployeesPage(businessId, this.currentEmployeePage + 1);
  }

  openInviteModal(): void {
    if (!this.canInviteEmployees || this.sendingInvite) {
      return;
    }

    this.inviteEmailControl.reset('');
    this.setDefaultInviteRole(this.businessWorkspace);
    this.inviteEmailControl.markAsPristine();
    this.inviteEmailControl.markAsUntouched();
    this.inviteRoleControl.markAsPristine();
    this.inviteRoleControl.markAsUntouched();
    this.inviteModalOpen = true;
  }

  closeInviteModal(): void {
    if (this.sendingInvite) {
      return;
    }

    this.inviteModalOpen = false;
  }

  submitInvite(): void {
    if (!this.businessWorkspace || this.sendingInvite) {
      return;
    }

    this.inviteEmailControl.markAsTouched();
    this.inviteRoleControl.markAsTouched();
    if (this.inviteEmailControl.invalid || this.inviteRoleControl.invalid) {
      return;
    }

    this.sendingInvite = true;
    this.businessService
      .createEmployeeInvite(
        this.businessWorkspace.businessId,
        this.inviteEmailControl.value.trim(),
        this.inviteRoleControl.value.trim()
      )
      .subscribe({
      next: (invite) => {
        this.sendingInvite = false;
        this.inviteModalOpen = false;
        this.employeeInvites = [invite, ...this.employeeInvites];
        this.snackBar.open('Employee invite sent.', 'Close', {
          duration: 3000,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.sendingInvite = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = this.extractErrors(err);
        this.snackBar.open(errors[0] ?? 'Could not send employee invite.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
      });
  }

  formatInviteDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return 'Unavailable';
    }

    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    }).format(date);
  }

  formatInvitedBy(invite: BusinessEmployeeInvite): string {
    const displayName = invite.invitedByDisplayName?.trim();
    const email = invite.invitedByEmail?.trim();

    if (displayName && email) {
      return `${displayName} (${email})`;
    }

    return displayName || email || 'Workspace User';
  }

  closeInvite(invite: BusinessEmployeeInvite): void {
    const businessId = this.businessWorkspace?.businessId;
    if (!businessId || !this.canCloseInvite(invite) || this.isClosingInvite(invite.workloadId)) {
      return;
    }

    this.closingInviteIds.add(invite.workloadId);
    this.businessService.closeEmployeeInvite(businessId, invite.workloadId).subscribe({
      next: () => {
        this.closingInviteIds.delete(invite.workloadId);
        this.employeeInvites = this.employeeInvites.filter((item) => item.workloadId !== invite.workloadId);
        this.snackBar.open('Employee invite closed.', 'Close', {
          duration: 3000,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.closingInviteIds.delete(invite.workloadId);
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = this.extractErrors(err);
        this.snackBar.open(errors[0] ?? 'Could not close employee invite.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  deactivateEmployee(employee: BusinessWorkspaceEmployee): void {
    const businessId = this.businessWorkspace?.businessId;
    if (!businessId || !this.canDeactivateEmployee(employee) || this.isDeactivatingEmployee(employee.businessUserId)) {
      return;
    }

    this.closeEmployeeMenu();
    this.deactivatingEmployeeIds.add(employee.businessUserId);
    this.businessService.deactivateEmployee(businessId, employee.businessUserId).subscribe({
      next: () => {
        this.deactivatingEmployeeIds.delete(employee.businessUserId);
        if (!this.businessWorkspace) {
          return;
        }

        const updatedEmployees = this.businessWorkspace.employees.map((item) =>
          item.businessUserId === employee.businessUserId
            ? { ...item, isActive: false, canDeactivate: false }
            : item
        );

        this.businessWorkspace = {
          ...this.businessWorkspace,
          employees: updatedEmployees,
          activeEmployeeCount: Math.max(0, this.businessWorkspace.activeEmployeeCount - 1),
        };

        this.employees = this.employees.map((item) =>
          item.businessUserId === employee.businessUserId
            ? { ...item, isActive: false, canDeactivate: false, canUpdateRole: false }
            : item
        );

        this.snackBar.open('Employee deactivated.', 'Close', {
          duration: 3000,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.deactivatingEmployeeIds.delete(employee.businessUserId);
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = this.extractErrors(err);
        this.snackBar.open(errors[0] ?? 'Could not deactivate employee.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  submitEmployeeRoleUpdate(): void {
    const businessId = this.businessWorkspace?.businessId;
    const employee = this.selectedEmployeeForRoleUpdate;
    if (!businessId || !employee || this.updatingEmployeeRole) {
      return;
    }

    this.employeeRoleControl.markAsTouched();
    if (this.employeeRoleControl.invalid) {
      return;
    }

    this.updatingEmployeeRole = true;
    this.businessService.updateEmployeeRole(businessId, employee.businessUserId, this.employeeRoleControl.value.trim()).subscribe({
      next: () => {
        this.updatingEmployeeRole = false;
        this.roleModalOpen = false;
        this.selectedEmployeeForRoleUpdate = null;
        this.employeeRoleControl.setValue('');
        this.reloadWorkspace(businessId);
        this.loadEmployeesPage(businessId, this.currentEmployeePage);
        this.snackBar.open('Employee role updated.', 'Close', {
          duration: 3000,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.updatingEmployeeRole = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = this.extractErrors(err);
        this.snackBar.open(errors[0] ?? 'Could not update employee role.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    const element = target instanceof Element ? target : target?.parentElement;
    if (!element?.closest('.auth-actions-logged-in')) {
      this.isUserMenuOpen = false;
    }

    if (!element?.closest('.row-action-menu') && !element?.closest('.row-action-popover')) {
      this.closeEmployeeMenu();
    }
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.closeEmployeeMenu();
  }

  @HostListener('document:scroll')
  onDocumentScroll(): void {
    this.closeEmployeeMenu();
  }

  @HostListener('document:wheel')
  onDocumentWheel(): void {
    this.closeEmployeeMenu();
  }

  @HostListener('document:touchmove')
  onDocumentTouchMove(): void {
    this.closeEmployeeMenu();
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.closeEmployeeMenu();
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

  private setDefaultInviteRole(workspace: BusinessWorkspace | null): void {
    const firstRole = workspace?.availableEmployeeRoles?.[0] ?? '';
    this.inviteRoleControl.setValue(firstRole);
  }

  private loadEmployeeInvites(businessId: number): void {
    this.loadingInvites = true;
    this.businessService.getEmployeeInvites(businessId).subscribe({
      next: (invites) => {
        this.employeeInvites = invites;
        this.loadingInvites = false;
      },
      error: (err) => {
        this.loadingInvites = false;

        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = this.extractErrors(err);
        this.snackBar.open(errors[0] ?? 'Could not load employee invites.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  private loadEmployeesPage(businessId: number, pageNumber: number): void {
    this.loadingEmployees = true;
    this.openEmployeeMenuId = null;
    this.businessService.getEmployees(businessId, pageNumber, BusinessEmployeesComponent.EmployeePageSize).subscribe({
      next: (employeePage: BusinessWorkspaceEmployeePage) => {
        this.employees = employeePage.employees;
        this.currentEmployeePage = employeePage.pageNumber;
        this.totalEmployeeCount = employeePage.totalCount;
        this.loadingEmployees = false;
      },
      error: (err) => {
        this.loadingEmployees = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = this.extractErrors(err);
        this.snackBar.open(errors[0] ?? 'Could not load employees.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  private reloadWorkspace(businessId: number): void {
    this.businessService.getBusinessWorkspace(businessId).subscribe({
      next: (workspace) => {
        this.businessWorkspace = workspace;
        this.setDefaultInviteRole(workspace);
      },
      error: (err) => {
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = this.extractErrors(err);
        this.snackBar.open(errors[0] ?? 'Could not refresh employees.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  private extractErrors(err: unknown): string[] {
    const apiErrors = (err as { error?: { errors?: unknown } } | null | undefined)?.error?.errors;
    return Array.isArray(apiErrors) ? apiErrors.filter((value): value is string => typeof value === 'string') : [];
  }
}
