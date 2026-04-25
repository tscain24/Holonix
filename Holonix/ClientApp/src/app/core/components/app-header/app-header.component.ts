import { Component, HostListener } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthSessionService } from '../../services/auth-session.service';

@Component({
  selector: 'app-header',
  templateUrl: './app-header.component.html',
  styleUrls: ['./app-header.component.css'],
})
export class AppHeaderComponent {
  hideHeader = false;
  isLoggedIn = false;
  displayName = '';
  userInitials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;
  locationDisplay = 'Woodstock, GA 30188';

  constructor(
    private router: Router,
    private authSession: AuthSessionService
  ) {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.applyRouteVisibility(event.urlAfterRedirects);
        this.refreshSessionFromStorage();
        this.isUserMenuOpen = false;
      });
  }

  ngOnInit(): void {
    this.applyRouteVisibility(this.router.url);
    this.refreshSessionFromStorage();
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
    this.isUserMenuOpen = false;
    this.authSession.logout();
    this.refreshSessionFromStorage();
  }

  onSearchSubmit(event: Event): void {
    event.preventDefault();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    const element = target instanceof Element ? target : target?.parentElement;
    if (!element?.closest('.header-actions-logged-in')) {
      this.isUserMenuOpen = false;
    }
  }

  private refreshSessionFromStorage(): void {
    const token = localStorage.getItem('holonix_token');
    const savedDisplayName = localStorage.getItem('holonix_display_name') ?? '';
    const savedProfileImage = localStorage.getItem('holonix_profile_image_base64') ?? '';
    this.isLoggedIn = !!token;
    this.displayName = savedDisplayName.trim() || 'User';
    const parts = this.displayName.split(' ').filter(Boolean);
    const firstInitial = parts[0]?.charAt(0) ?? 'U';
    const lastInitial = parts[1]?.charAt(0) ?? '';
    this.userInitials = `${firstInitial}${lastInitial}`.toUpperCase();
    this.profileImageDataUrl = savedProfileImage
      ? `data:image/*;base64,${savedProfileImage.trim()}`
      : '';
  }

  private applyRouteVisibility(url: string): void {
    const route = url.split('?')[0].split('#')[0];
    this.hideHeader = route === '/login' || route === '/register';
  }
}
