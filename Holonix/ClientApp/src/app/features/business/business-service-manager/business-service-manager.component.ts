import { Component, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  BusinessService,
  BusinessWorkspace,
  BusinessWorkspaceSubService,
} from '../../../core/services/business.service';
import { AuthSessionService } from '../../../core/services/auth-session.service';

interface VisibleBusinessSubService extends BusinessWorkspaceSubService {
  parentServiceName: string;
}

@Component({
  selector: 'app-business-service-manager',
  templateUrl: './business-service-manager.component.html',
  styleUrls: ['./business-service-manager.component.css'],
})
export class BusinessServiceManagerComponent implements OnInit {
  displayName = 'User';
  initials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;
  loadingWorkspace = true;
  businessWorkspace: BusinessWorkspace | null = null;

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
        this.businessWorkspace = workspace;
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
      });
  }

  hasVisibleSubServices(): boolean {
    return this.visibleSubServices.length > 0;
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
}
