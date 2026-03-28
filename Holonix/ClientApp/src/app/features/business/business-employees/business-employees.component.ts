import { Component, ElementRef, HostListener, OnInit } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import {
  BusinessEmployeeInvite,
  BusinessService,
  BusinessWorkspace,
  BusinessWorkspaceEmployee,
} from '../../../core/services/business.service';

@Component({
  selector: 'app-business-employees',
  templateUrl: './business-employees.component.html',
  styleUrls: ['./business-employees.component.css'],
})
export class BusinessEmployeesComponent implements OnInit {
  displayName = 'User';
  initials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;
  loadingWorkspace = true;
  loadingInvites = true;
  inviteModalOpen = false;
  sendingInvite = false;
  businessWorkspace: BusinessWorkspace | null = null;
  employeeInvites: BusinessEmployeeInvite[] = [];
  readonly inviteEmailControl = new FormControl('', {
    validators: [Validators.required, Validators.email, Validators.maxLength(256)],
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
        this.loadingWorkspace = false;
        this.loadEmployeeInvites(workspace.businessId);
      },
      error: (err) => {
        this.loadingWorkspace = false;
        this.loadingInvites = false;

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
    return this.inviteEmailControl.valid && !this.sendingInvite;
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

  openInviteModal(): void {
    if (!this.canInviteEmployees || this.sendingInvite) {
      return;
    }

    this.inviteEmailControl.reset('');
    this.inviteEmailControl.markAsPristine();
    this.inviteEmailControl.markAsUntouched();
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
    if (this.inviteEmailControl.invalid) {
      return;
    }

    this.sendingInvite = true;
    this.businessService.createEmployeeInvite(this.businessWorkspace.businessId, this.inviteEmailControl.value.trim()).subscribe({
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

  private extractErrors(err: unknown): string[] {
    const apiErrors = (err as { error?: { errors?: unknown } } | null | undefined)?.error?.errors;
    return Array.isArray(apiErrors) ? apiErrors.filter((value): value is string => typeof value === 'string') : [];
  }
}
