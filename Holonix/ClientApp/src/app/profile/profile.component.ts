import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

interface JwtPayload {
  sub?: string;
  email?: string;
  name?: string;
  unique_name?: string;
  exp?: number;
  iat?: number;
}

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css'],
})
export class ProfileComponent implements OnInit {
  displayName = 'User';
  firstName = 'User';
  lastName = '';
  email = '';
  userId = '';
  initials = 'U';
  tokenExpiresAt = '';

  constructor(private router: Router) {}

  ngOnInit(): void {
    const token = localStorage.getItem('holonix_token');
    const savedDisplayName = (localStorage.getItem('holonix_display_name') ?? '').trim();

    if (!token) {
      this.router.navigate(['/login']);
      return;
    }

    const payload = this.decodeJwtPayload(token);
    const nameFromToken = (payload?.name ?? payload?.unique_name ?? '').trim();
    this.displayName = savedDisplayName || nameFromToken || 'User';

    const parts = this.displayName.split(' ').filter(Boolean);
    this.firstName = parts[0] ?? 'User';
    this.lastName = parts.slice(1).join(' ');
    this.initials = (this.firstName[0] ?? 'U') + (this.lastName[0] ?? '');
    this.initials = this.initials.toUpperCase();

    this.email = payload?.email ?? '';
    this.userId = payload?.sub ?? '';
    this.tokenExpiresAt = payload?.exp ? new Date(payload.exp * 1000).toLocaleString() : '';
  }

  goHome(): void {
    this.router.navigate(['/home']);
  }

  signOut(): void {
    localStorage.removeItem('holonix_token');
    localStorage.removeItem('holonix_display_name');
    this.router.navigate(['/login']);
  }

  private decodeJwtPayload(token: string): JwtPayload | null {
    try {
      const tokenParts = token.split('.');
      if (tokenParts.length < 2) {
        return null;
      }

      const base64 = tokenParts[1].replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
      const json = atob(padded);
      return JSON.parse(json) as JwtPayload;
    } catch {
      return null;
    }
  }
}
