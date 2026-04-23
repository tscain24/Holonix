import { Component } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { AuthSessionService } from '../../core/services/auth-session.service';
import { BusinessService } from '../../core/services/business.service';

interface JwtPayload {
  role?: string | string[];
  roles?: string[];
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[];
}

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'],
})
export class HomeComponent {
  isLoggedIn = false;
  hasBusiness = false;
  firstName = 'User';
  guestCategories = [
    { label: 'Cleaning', icon: 'cleaning_services' },
    { label: 'Plumbing', icon: 'plumbing' },
    { label: 'Electrical', icon: 'electrical_services' },
    { label: 'Moving', icon: 'local_shipping' },
    { label: 'Lawn Care', icon: 'grass' },
    { label: 'Painting', icon: 'format_paint' },
    { label: 'Handyman', icon: 'handyman' },
    { label: 'More', icon: 'more_horiz' },
  ];

  constructor(
    private router: Router,
    private snackBar: MatSnackBar,
    private authSession: AuthSessionService,
    private businessService: BusinessService
  ) {}

  ngOnInit(): void {
    const message = (history.state && history.state.toastMessage) as string | undefined;
    if (message) {
      this.snackBar.open(message, 'Close', {
        duration: 3500,
        panelClass: ['snack-success'],
      });
      history.replaceState({}, '');
    }

    const token = localStorage.getItem('holonix_token');
    const savedDisplayName = localStorage.getItem('holonix_display_name') ?? '';
    this.isLoggedIn = !!token;
    this.hasBusiness = this.hasOwnerRole(this.decodeJwtPayload(token));
    this.firstName = (savedDisplayName.trim() || 'User').split(' ')[0] || 'User';

    if (this.isLoggedIn) {
      this.businessService.getMyBusinesses().subscribe({
        next: (businesses) => {
          this.hasBusiness = businesses.length > 0;
        },
        error: () => {
          if (this.authSession.hasSessionExpiredFlag()) {
            return;
          }
        },
      });
    }
  }

  goToBusinessEntry(): void {
    if (!this.isLoggedIn) {
      this.router.navigate(['/register']);
      return;
    }
    this.router.navigate([this.hasBusiness ? '/business' : '/business/create']);
  }

  private decodeJwtPayload(token: string | null): JwtPayload | null {
    try {
      if (!token) {
        return null;
      }

      const tokenParts = token.split('.');
      if (tokenParts.length < 2) {
        return null;
      }

      const base64 = tokenParts[1].replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
      return JSON.parse(atob(padded)) as JwtPayload;
    } catch {
      return null;
    }
  }

  private hasOwnerRole(payload: JwtPayload | null): boolean {
    if (!payload) {
      return false;
    }

    const roleValues = [
      payload.role,
      payload.roles,
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
    ].flatMap((value) => Array.isArray(value) ? value : value ? [value] : []);

    return roleValues.some((role) => role.toLowerCase() === 'owner');
  }
}
