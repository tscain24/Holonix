import { Component, ElementRef, HostListener } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';

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
  displayName = '';
  firstName = 'User';
  userInitials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;

  constructor(
    private router: Router,
    private elementRef: ElementRef<HTMLElement>,
    private snackBar: MatSnackBar
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
    const savedProfileImage = localStorage.getItem('holonix_profile_image_base64') ?? '';
    this.isLoggedIn = !!token;
    this.hasBusiness = this.hasOwnerRole(this.decodeJwtPayload(token));
    this.displayName = savedDisplayName.trim() || 'User';
    this.firstName = this.displayName.split(' ')[0] || 'User';
    const parts = this.displayName.split(' ').filter(Boolean);
    const firstInitial = parts[0]?.charAt(0) ?? 'U';
    const lastInitial = parts[1]?.charAt(0) ?? '';
    this.userInitials = `${firstInitial}${lastInitial}`.toUpperCase();
    this.profileImageDataUrl = savedProfileImage
      ? `data:image/*;base64,${savedProfileImage.trim()}`
      : '';
  }

  toggleUserMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.isUserMenuOpen = !this.isUserMenuOpen;
  }

  viewProfile(): void {
    this.isUserMenuOpen = false;
    this.router.navigate(['/profile']);
  }

  signOut(): void {
    localStorage.removeItem('holonix_token');
    localStorage.removeItem('holonix_display_name');
    localStorage.removeItem('holonix_profile_image_base64');
    this.isLoggedIn = false;
    this.displayName = '';
    this.isUserMenuOpen = false;
    this.router.navigate(['/home']);
  }

  goToBusinessEntry(): void {
    if (!this.isLoggedIn) {
      this.router.navigate(['/register']);
      return;
    }

    this.router.navigate([this.hasBusiness ? '/business' : '/business/create']);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    if (target && !this.elementRef.nativeElement.contains(target)) {
      this.isUserMenuOpen = false;
    }
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
