import { Component, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  BusinessService,
  BusinessWorkspace,
  MyBusinessAvailability,
  MyBusinessAvailabilityDay,
  UpdateMyBusinessAvailabilityRequest,
} from '../../../core/services/business.service';
import { AuthSessionService } from '../../../core/services/auth-session.service';

type AvailabilityDayEditor = {
  dayOfWeek: number;
  label: string;
  enabled: boolean;
  startTime: string;
  endTime: string;
};

@Component({
  selector: 'app-business-my-availability',
  templateUrl: './business-my-availability.component.html',
  styleUrls: ['./business-my-availability.component.css'],
})
export class BusinessMyAvailabilityComponent implements OnInit {
  readonly availabilityDays: AvailabilityDayEditor[] = [
    { dayOfWeek: 1, label: 'Monday', enabled: false, startTime: '09:00', endTime: '17:00' },
    { dayOfWeek: 2, label: 'Tuesday', enabled: false, startTime: '09:00', endTime: '17:00' },
    { dayOfWeek: 3, label: 'Wednesday', enabled: false, startTime: '09:00', endTime: '17:00' },
    { dayOfWeek: 4, label: 'Thursday', enabled: false, startTime: '09:00', endTime: '17:00' },
    { dayOfWeek: 5, label: 'Friday', enabled: false, startTime: '09:00', endTime: '17:00' },
    { dayOfWeek: 6, label: 'Saturday', enabled: false, startTime: '09:00', endTime: '17:00' },
    { dayOfWeek: 0, label: 'Sunday', enabled: false, startTime: '09:00', endTime: '17:00' },
  ];

  displayName = 'User';
  initials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;
  loadingWorkspace = true;
  loadingAvailability = true;
  savingAvailability = false;
  businessWorkspace: BusinessWorkspace | null = null;
  effectiveStartDate = '';

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
        this.loadAvailability(businessCode);
      },
      error: (err) => {
        this.loadingWorkspace = false;
        this.loadingAvailability = false;

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

        this.snackBar.open('Could not load your availability page.', 'Close', {
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

  signOut(): void {
    this.authSession.clearSession();
    this.router.navigate(['/login']);
  }

  toggleUserMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.isUserMenuOpen = !this.isUserMenuOpen;
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

  goToMyAvailabilityTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (businessCode) {
      this.router.navigate(['/business', businessCode, 'availability']);
    }
  }

  goToJobsTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (businessCode) {
      this.router.navigate(['/business', businessCode], { fragment: 'jobs' });
    }
  }

  get canSaveAvailability(): boolean {
    if (this.loadingAvailability || this.savingAvailability) {
      return false;
    }

    return this.availabilityDays.every((day) => {
      if (!day.enabled) {
        return true;
      }

      return !!day.startTime && !!day.endTime && day.startTime < day.endTime;
    });
  }

  onDayEnabledChange(day: AvailabilityDayEditor, event: Event): void {
    day.enabled = (event.target as HTMLInputElement).checked;
  }

  onDayStartTimeChange(day: AvailabilityDayEditor, event: Event): void {
    day.startTime = (event.target as HTMLInputElement).value;
  }

  onDayEndTimeChange(day: AvailabilityDayEditor, event: Event): void {
    day.endTime = (event.target as HTMLInputElement).value;
  }

  saveAvailability(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (!businessCode || !this.canSaveAvailability) {
      return;
    }

    const payload: UpdateMyBusinessAvailabilityRequest = {
      availability: this.availabilityDays
        .filter((day) => day.enabled)
        .map((day) => ({
          dayOfWeek: day.dayOfWeek,
          startTime: `${day.startTime}:00`,
          endTime: `${day.endTime}:00`,
        })),
    };

    this.savingAvailability = true;
    this.businessService.updateMyBusinessAvailability(businessCode, payload).subscribe({
      next: (response) => {
        this.applyAvailability(response);
        this.savingAvailability = false;
        this.snackBar.open('Availability updated.', undefined, {
          duration: 2500,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.savingAvailability = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = (err as { error?: { errors?: unknown } } | null | undefined)?.error?.errors;
        const messages = Array.isArray(errors) ? errors.filter((value): value is string => typeof value === 'string') : [];
        this.snackBar.open(messages[0] ?? 'Could not update availability.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  formatEffectiveDate(value: string): string {
    if (!value?.trim()) {
      return 'Today';
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

  private loadAvailability(businessCode: string): void {
    this.loadingAvailability = true;
    this.businessService.getMyBusinessAvailability(businessCode).subscribe({
      next: (response) => {
        this.applyAvailability(response);
        this.loadingAvailability = false;
      },
      error: (err) => {
        this.loadingAvailability = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errors = (err as { error?: { errors?: unknown } } | null | undefined)?.error?.errors;
        const messages = Array.isArray(errors) ? errors.filter((value): value is string => typeof value === 'string') : [];
        this.snackBar.open(messages[0] ?? 'Could not load your availability.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  private applyAvailability(response: MyBusinessAvailability): void {
    const byDay = new Map<number, MyBusinessAvailabilityDay>(
      (response.availability ?? []).map((day) => [day.dayOfWeek, day])
    );

    this.effectiveStartDate = response.effectiveStartDate ?? '';
    for (const day of this.availabilityDays) {
      const match = byDay.get(day.dayOfWeek);
      day.enabled = !!match;
      day.startTime = this.normalizeTime(match?.startTime) || '09:00';
      day.endTime = this.normalizeTime(match?.endTime) || '17:00';
    }
  }

  private normalizeTime(value?: string | null): string {
    const trimmed = (value ?? '').trim();
    if (!trimmed) {
      return '';
    }

    return trimmed.slice(0, 5);
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
