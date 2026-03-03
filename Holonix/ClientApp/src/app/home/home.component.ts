import { Component, ElementRef, HostListener } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'],
})
export class HomeComponent {
  isLoggedIn = false;
  displayName = '';
  firstName = 'User';
  userInitials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;

  constructor(private router: Router, private elementRef: ElementRef<HTMLElement>) {}

  ngOnInit(): void {
    const token = localStorage.getItem('holonix_token');
    const savedDisplayName = localStorage.getItem('holonix_display_name') ?? '';
    const savedProfileImage = localStorage.getItem('holonix_profile_image_base64') ?? '';
    this.isLoggedIn = !!token;
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

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    if (target && !this.elementRef.nativeElement.contains(target)) {
      this.isUserMenuOpen = false;
    }
  }
}
