import { Component, ElementRef, HostListener, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { BusinessService, UserBusinessSummary } from '../../../core/services/business.service';
import { AuthSessionService } from '../../../core/services/auth-session.service';

@Component({
  selector: 'app-business-overview',
  templateUrl: './business-overview.component.html',
  styleUrls: ['./business-overview.component.css'],
})
export class BusinessOverviewComponent implements OnInit {
  displayName = 'User';
  initials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;
  loadingBusinesses = true;
  businesses: UserBusinessSummary[] = [];

  constructor(
    private readonly router: Router,
    private readonly elementRef: ElementRef<HTMLElement>,
    private readonly businessService: BusinessService,
    private readonly snackBar: MatSnackBar,
    private readonly authSession: AuthSessionService
  ) {}

  ngOnInit(): void {
    const token = localStorage.getItem('holonix_token');
    if (!token) {
      this.router.navigate(['/login']);
      return;
    }

    const message = (history.state && history.state.toastMessage) as string | undefined;
    if (message) {
      this.snackBar.open(message, 'Close', {
        duration: 3500,
        panelClass: ['snack-success'],
      });
      history.replaceState({}, '');
    }

    this.displayName = (localStorage.getItem('holonix_display_name') ?? '').trim() || 'User';
    const parts = this.displayName.split(' ').filter(Boolean);
    this.initials = `${parts[0]?.[0] ?? 'U'}${parts[1]?.[0] ?? ''}`.toUpperCase();
    this.profileImageDataUrl = this.buildImageDataUrl(localStorage.getItem('holonix_profile_image_base64'));

    this.businessService.getMyBusinesses().subscribe({
      next: (businesses) => {
        this.businesses = businesses;
        this.loadingBusinesses = false;
      },
      error: () => {
        this.loadingBusinesses = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        this.snackBar.open('Could not load your businesses.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  goHome(): void {
    this.router.navigate(['/home']);
  }

  goToProfile(): void {
    this.router.navigate(['/profile']);
  }

  goToCreateBusiness(): void {
    this.router.navigate(['/business/create']);
  }

  goToBusinessWorkspace(businessCode: string): void {
    this.router.navigate(['/business', businessCode]);
  }

  openBusinessWorkspace(business: UserBusinessSummary): void {
    this.goToBusinessWorkspace(business.businessCode);
  }

  toggleUserMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.isUserMenuOpen = !this.isUserMenuOpen;
  }

  signOut(): void {
    this.isUserMenuOpen = false;
    this.authSession.logout();
  }

  getBusinessIconDataUrl(base64: string | null | undefined): string {
    return this.buildImageDataUrl(base64 ?? null);
  }

  getBusinessLocationLabel(business: UserBusinessSummary): string {
    const parts = [business.city, business.state, business.countryName]
      .filter((value): value is string => !!value && value.trim().length > 0);

    return parts.length > 0 ? parts.join(', ') : 'Location not provided';
  }

  getBusinessSummary(business: UserBusinessSummary): string {
    return business.description?.trim() || 'No business description yet.';
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

    return `data:image/*;base64,${base64.trim()}`;
  }
}
