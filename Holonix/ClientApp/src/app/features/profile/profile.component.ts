import { Component, ElementRef, HostListener, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { AuthSessionService } from '../../core/services/auth-session.service';

interface JwtPayload {
  sub?: string;
  email?: string;
  name?: string;
  unique_name?: string;
  role?: string | string[];
  roles?: string[];
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[];
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
  isBusinessOwner = false;
  isUserMenuOpen = false;
  profileImageDataUrl = '';
  uploadingImage = false;
  savingProfile = false;
  deletingAccount = false;
  isEditProfileMode = false;
  isDeleteAccountModalOpen = false;
  savedDateOfBirth = '';
  editableFirstName = '';
  editableLastName = '';
  editableDateOfBirth = '';
  editableDateOfBirthDisplay = '';
  isProfileImageModalOpen = false;
  pendingProfileImageBase64 = '';
  pendingProfileImageDataUrl = '';
  previewSize = 220;
  pendingImageNaturalWidth = 0;
  pendingImageNaturalHeight = 0;
  pendingImageRenderedWidth = 0;
  pendingImageRenderedHeight = 0;
  pendingImageOffsetX = 0;
  pendingImageOffsetY = 0;
  maxImageOffsetX = 0;
  maxImageOffsetY = 0;
  isDraggingPreviewImage = false;
  dragStartX = 0;
  dragStartY = 0;
  dragOriginX = 0;
  dragOriginY = 0;

  constructor(
    private router: Router,
    private elementRef: ElementRef<HTMLElement>,
    private auth: AuthService,
    private snackBar: MatSnackBar,
    private authSession: AuthSessionService
  ) {}

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
    this.isBusinessOwner = this.hasOwnerRole(payload);
    this.editableFirstName = this.firstName;
    this.editableLastName = this.lastName;

    const savedProfileImage = localStorage.getItem('holonix_profile_image_base64');
    this.profileImageDataUrl = this.buildImageDataUrl(savedProfileImage);

    this.auth.getProfile().subscribe({
      next: (profile) => {
        const profileAny = profile as unknown as { dateOfBirth?: unknown; DateOfBirth?: unknown };
        const incomingDob = profileAny.dateOfBirth ?? profileAny.DateOfBirth;
        const firstName = profile.firstName?.trim() || this.firstName;
        const lastName = profile.lastName?.trim() || this.lastName;
        this.firstName = firstName;
        this.lastName = lastName;
        this.displayName = `${firstName} ${lastName}`.trim();
        this.editableFirstName = firstName;
        this.editableLastName = lastName;
        this.savedDateOfBirth = this.normalizeIncomingDateOfBirth(incomingDob);
        this.setEditableDateOfBirth(this.savedDateOfBirth);
        if (profile.profileImageBase64) {
          localStorage.setItem('holonix_profile_image_base64', profile.profileImageBase64);
          this.profileImageDataUrl = this.buildImageDataUrl(profile.profileImageBase64);
        }
        localStorage.setItem('holonix_display_name', this.displayName);
      },
      error: () => {
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        // Keep local fallback values if profile fetch fails.
      },
    });
  }

  goHome(): void {
    this.router.navigate(['/home']);
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
    this.authSession.clearSession();
    this.router.navigate(['/login']);
  }

  openDeleteAccountModal(): void {
    if (this.deletingAccount) {
      return;
    }

    this.isDeleteAccountModalOpen = true;
  }

  closeDeleteAccountModal(): void {
    if (this.deletingAccount) {
      return;
    }

    this.isDeleteAccountModalOpen = false;
  }

  confirmDeleteAccount(): void {
    if (this.deletingAccount) {
      return;
    }

    this.deletingAccount = true;
    this.auth.deleteProfile().subscribe({
      next: () => {
        this.deletingAccount = false;
        this.isDeleteAccountModalOpen = false;
        this.authSession.clearSession();
        this.router.navigate(['/home'], {
          state: { toastMessage: 'Your account was deleted successfully.' },
        });
      },
      error: (err) => {
        this.deletingAccount = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = err?.error?.errors as string[] | undefined;
        this.snackBar.open(errors?.[0] ?? 'Failed to delete account.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  onCreateBusinessProfile(): void {
    this.router.navigate(['/business/create']);
  }

  onProfileImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const dataUrl = typeof reader.result === 'string' ? reader.result : '';
        const base64 = this.extractBase64(dataUrl);
        if (!base64) {
          this.snackBar.open('Could not read selected image.', 'Close', {
            duration: 3000,
            panelClass: ['snack-error'],
          });
          return;
        }

      this.pendingProfileImageBase64 = base64;
      this.pendingProfileImageDataUrl = this.buildImageDataUrl(base64);
      this.initializePreviewGeometry(this.pendingProfileImageDataUrl);
      this.isProfileImageModalOpen = true;
    };

    reader.readAsDataURL(file);
    input.value = '';
  }

  cancelProfileImagePreview(): void {
    if (this.uploadingImage) {
      return;
    }

    this.isProfileImageModalOpen = false;
    this.pendingProfileImageBase64 = '';
    this.pendingProfileImageDataUrl = '';
  }

  submitProfileImagePreview(): void {
    if (!this.pendingProfileImageBase64 || this.uploadingImage) {
      return;
    }

    this.uploadingImage = true;
    this.buildAdjustedProfileImageBase64(this.pendingProfileImageDataUrl)
      .then((adjustedBase64) => {
        this.auth.updateProfileImage(adjustedBase64).subscribe({
          next: (res) => {
            this.uploadingImage = false;
            const normalized = res.profileImageBase64 ?? '';
            if (normalized) {
              localStorage.setItem('holonix_profile_image_base64', normalized);
            } else {
              localStorage.removeItem('holonix_profile_image_base64');
            }
            this.profileImageDataUrl = this.buildImageDataUrl(normalized);
            this.cancelProfileImagePreview();
            this.snackBar.open('Profile picture updated.', 'Close', {
              duration: 2500,
              panelClass: ['snack-success'],
            });
          },
          error: () => {
            this.uploadingImage = false;
            if (this.authSession.hasSessionExpiredFlag()) {
              return;
            }

            this.snackBar.open('Failed to update profile picture.', 'Close', {
              duration: 3000,
              panelClass: ['snack-error'],
            });
          },
        });
      })
      .catch(() => {
        this.uploadingImage = false;
        this.snackBar.open('Could not process selected image.', 'Close', {
          duration: 3000,
          panelClass: ['snack-error'],
        });
      });
  }

  enterEditProfileMode(): void {
    this.editableFirstName = this.firstName;
    this.editableLastName = this.lastName;
    this.setEditableDateOfBirth(this.savedDateOfBirth);
    this.isEditProfileMode = true;
  }

  cancelEditProfile(): void {
    this.editableFirstName = this.firstName;
    this.editableLastName = this.lastName;
    this.setEditableDateOfBirth(this.savedDateOfBirth);
    this.isEditProfileMode = false;
  }

  saveProfileChanges(): void {
    const firstName = this.editableFirstName.trim();
    const lastName = this.editableLastName.trim();

    if (!firstName || !lastName) {
      this.snackBar.open('First and last name are required.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    if (this.editableDateOfBirthDisplay && !this.editableDateOfBirth) {
      this.snackBar.open('Enter a valid birthday.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    this.savingProfile = true;
    this.auth.updateProfile({
      firstName,
      lastName,
      dateOfBirth: this.editableDateOfBirth || null,
    }).subscribe({
      next: (profile) => {
        const profileAny = profile as unknown as { dateOfBirth?: unknown; DateOfBirth?: unknown };
        const incomingDob = profileAny.dateOfBirth ?? profileAny.DateOfBirth;
        this.savingProfile = false;
        this.isEditProfileMode = false;
        this.firstName = profile.firstName.trim();
        this.lastName = profile.lastName.trim();
        this.displayName = `${this.firstName} ${this.lastName}`.trim();
        this.editableFirstName = this.firstName;
        this.editableLastName = this.lastName;
        this.savedDateOfBirth = this.normalizeIncomingDateOfBirth(incomingDob);
        this.setEditableDateOfBirth(this.savedDateOfBirth);
        localStorage.setItem('holonix_display_name', this.displayName);
        this.initials = `${this.firstName.charAt(0)}${this.lastName.charAt(0)}`.toUpperCase();
        this.snackBar.open('Profile updated.', 'Close', {
          duration: 2500,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.savingProfile = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = err?.error?.errors as string[] | undefined;
        this.snackBar.open(errors?.[0] ?? 'Failed to update profile.', 'Close', {
          duration: 3000,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  onDateOfBirthInput(event: Event): void {
    if (!this.isEditProfileMode) {
      const input = event.target as HTMLInputElement;
      input.value = this.editableDateOfBirthDisplay;
      return;
    }

    const input = event.target as HTMLInputElement;
    const raw = input.value;
    const selectionStart = input.selectionStart ?? raw.length;
    const digitsBeforeCursor = raw.slice(0, selectionStart).replace(/\D/g, '').length;
    const digits = raw.replace(/\D/g, '').slice(0, 8);

    let formatted = '';
    if (digits.length <= 2) {
      formatted = digits;
    } else if (digits.length <= 4) {
      formatted = `${digits.slice(0, 2)}/${digits.slice(2)}`;
    } else {
      formatted = `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}`;
    }

    if (formatted !== raw) {
      input.value = formatted;
      let caret = digitsBeforeCursor;
      if (digitsBeforeCursor > 2 && digitsBeforeCursor <= 4) {
        caret += 1;
      } else if (digitsBeforeCursor > 4) {
        caret += 2;
      }
      input.setSelectionRange(caret, caret);
    }

    this.editableDateOfBirthDisplay = formatted;
    this.editableDateOfBirth = this.parseDateOfBirthDisplay(formatted);
  }

  onFirstNameInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!this.isEditProfileMode) {
      input.value = this.editableFirstName;
      return;
    }

    this.editableFirstName = input.value;
  }

  onLastNameInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!this.isEditProfileMode) {
      input.value = this.editableLastName;
      return;
    }

    this.editableLastName = input.value;
  }

  startPreviewImageDrag(event: MouseEvent | TouchEvent): void {
    if (this.uploadingImage) {
      return;
    }

    event.preventDefault();
    const point = this.getPointer(event);
    this.isDraggingPreviewImage = true;
    this.dragStartX = point.x;
    this.dragStartY = point.y;
    this.dragOriginX = this.pendingImageOffsetX;
    this.dragOriginY = this.pendingImageOffsetY;
  }

  onPreviewImageDrag(event: MouseEvent | TouchEvent): void {
    if (!this.isDraggingPreviewImage) {
      return;
    }

    event.preventDefault();
    const point = this.getPointer(event);
    const deltaX = point.x - this.dragStartX;
    const deltaY = point.y - this.dragStartY;

    this.pendingImageOffsetX = this.clamp(deltaX + this.dragOriginX, -this.maxImageOffsetX, this.maxImageOffsetX);
    this.pendingImageOffsetY = this.clamp(deltaY + this.dragOriginY, -this.maxImageOffsetY, this.maxImageOffsetY);
  }

  endPreviewImageDrag(): void {
    this.isDraggingPreviewImage = false;
  }

  @HostListener('document:mouseup')
  onGlobalMouseUp(): void {
    this.endPreviewImageDrag();
  }

  @HostListener('document:touchend')
  onGlobalTouchEnd(): void {
    this.endPreviewImageDrag();
  }

  @HostListener('document:touchcancel')
  onGlobalTouchCancel(): void {
    this.endPreviewImageDrag();
  }

  getPreviewImageTransform(): string {
    return `translate(-50%, -50%) translate(${this.pendingImageOffsetX}px, ${this.pendingImageOffsetY}px)`;
  }

  getPreviewHint(): string {
    return this.isDraggingPreviewImage
      ? 'Release to set position'
      : 'Drag image to reposition';
  }

  private initializePreviewGeometry(dataUrl: string): void {
    const image = new Image();
    image.onload = () => {
      this.pendingImageNaturalWidth = image.naturalWidth;
      this.pendingImageNaturalHeight = image.naturalHeight;

      const baseScale = Math.max(
        this.previewSize / this.pendingImageNaturalWidth,
        this.previewSize / this.pendingImageNaturalHeight
      );

      this.pendingImageRenderedWidth = this.pendingImageNaturalWidth * baseScale;
      this.pendingImageRenderedHeight = this.pendingImageNaturalHeight * baseScale;
      this.maxImageOffsetX = Math.max(0, (this.pendingImageRenderedWidth - this.previewSize) / 2);
      this.maxImageOffsetY = Math.max(0, (this.pendingImageRenderedHeight - this.previewSize) / 2);
      this.pendingImageOffsetX = 0;
      this.pendingImageOffsetY = 0;
    };
    image.src = dataUrl;
  }

  private async buildAdjustedProfileImageBase64(dataUrl: string): Promise<string> {
    const sourceImage = await this.loadImage(dataUrl);
    const outputSize = 256;
    const canvas = document.createElement('canvas');
    canvas.width = outputSize;
    canvas.height = outputSize;
    const context = canvas.getContext('2d');

    if (!context) {
      throw new Error('Canvas unavailable');
    }

    const drawScale = outputSize / this.previewSize;
    const drawWidth = this.pendingImageRenderedWidth * drawScale;
    const drawHeight = this.pendingImageRenderedHeight * drawScale;
    const drawX = ((this.previewSize - this.pendingImageRenderedWidth) / 2 + this.pendingImageOffsetX) * drawScale;
    const drawY = ((this.previewSize - this.pendingImageRenderedHeight) / 2 + this.pendingImageOffsetY) * drawScale;

    context.clearRect(0, 0, outputSize, outputSize);
    context.drawImage(sourceImage, drawX, drawY, drawWidth, drawHeight);

    const outputDataUrl = canvas.toDataURL('image/png');
    const outputBase64 = this.extractBase64(outputDataUrl);
    if (!outputBase64) {
      throw new Error('Image encoding failed');
    }

    return outputBase64;
  }

  private loadImage(dataUrl: string): Promise<HTMLImageElement> {
    return new Promise((resolve, reject) => {
      const image = new Image();
      image.onload = () => resolve(image);
      image.onerror = () => reject(new Error('Image load failed'));
      image.src = dataUrl;
    });
  }

  private getPointer(event: MouseEvent | TouchEvent): { x: number; y: number } {
    if ('touches' in event) {
      const touch = event.touches[0] ?? event.changedTouches[0];
      return { x: touch?.clientX ?? 0, y: touch?.clientY ?? 0 };
    }

    return { x: event.clientX, y: event.clientY };
  }

  private clamp(value: number, min: number, max: number): number {
    return Math.min(Math.max(value, min), max);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    if (target && !this.elementRef.nativeElement.contains(target)) {
      this.isUserMenuOpen = false;
    }
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

  private extractBase64(dataUrl: string): string {
    const marker = 'base64,';
    const markerIndex = dataUrl.indexOf(marker);
    if (markerIndex < 0) {
      return '';
    }

    return dataUrl.slice(markerIndex + marker.length).trim();
  }

  private buildImageDataUrl(base64: string | null): string {
    if (!base64 || !base64.trim()) {
      return '';
    }

    return `data:image/*;base64,${base64.trim()}`;
  }

  private setEditableDateOfBirth(isoValue: string): void {
    this.editableDateOfBirth = isoValue;
    this.editableDateOfBirthDisplay = this.formatDateOfBirthDisplay(isoValue);
  }

  private formatDateOfBirthDisplay(isoValue: string): string {
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(isoValue);
    if (!match) {
      return '';
    }

    return `${match[2]}/${match[3]}/${match[1]}`;
  }

  private parseDateOfBirthDisplay(displayValue: string): string {
    const trimmed = displayValue.trim();
    if (!trimmed) {
      return '';
    }

    const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(trimmed);
    if (!match) {
      return '';
    }

    const month = Number(match[1]);
    const day = Number(match[2]);
    const year = Number(match[3]);

    const candidate = new Date(year, month - 1, day);
    if (
      Number.isNaN(candidate.getTime()) ||
      candidate.getFullYear() !== year ||
      candidate.getMonth() !== month - 1 ||
      candidate.getDate() !== day
    ) {
      return '';
    }

    const today = new Date();
    if (candidate > today) {
      return '';
    }

    const minAgeCutoff = new Date(today.getFullYear() - 13, today.getMonth(), today.getDate());
    if (candidate > minAgeCutoff) {
      return '';
    }

    const monthPart = String(month).padStart(2, '0');
    const dayPart = String(day).padStart(2, '0');
    return `${year}-${monthPart}-${dayPart}`;
  }

  private normalizeIncomingDateOfBirth(value: unknown): string {
    if (typeof value !== 'string') {
      return '';
    }

    const raw = value.trim();
    if (!raw) {
      return '';
    }

    const isoMatch = /^(\d{4})-(\d{1,2})-(\d{1,2})/.exec(raw);
    if (isoMatch) {
      const year = Number(isoMatch[1]);
      const month = Number(isoMatch[2]);
      const day = Number(isoMatch[3]);
      if (this.isValidCalendarDate(year, month, day)) {
        return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
      }
    }

    const slashMatch = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/.exec(raw);
    if (slashMatch) {
      const month = Number(slashMatch[1]);
      const day = Number(slashMatch[2]);
      const year = Number(slashMatch[3]);
      if (this.isValidCalendarDate(year, month, day)) {
        return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
      }
    }

    return '';
  }

  private isValidCalendarDate(year: number, month: number, day: number): boolean {
    const candidate = new Date(year, month - 1, day);
    return (
      !Number.isNaN(candidate.getTime()) &&
      candidate.getFullYear() === year &&
      candidate.getMonth() === month - 1 &&
      candidate.getDate() === day
    );
  }
}
