import { Component, ElementRef, HostListener, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { BusinessService, MyAssignedJob, UserBusinessSummary } from '../../../core/services/business.service';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { catchError, forkJoin, of } from 'rxjs';

type EmployeeScheduleRow = {
  businessName: string;
  businessCode: string;
  hours: string[];
};

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
  isMobileMenuOpen = false;
  loadingBusinesses = true;
  businessLoadError: string | null = null;
  loadingEmployeeSchedule = false;
  businesses: UserBusinessSummary[] = [];
  employeeScheduleDays: string[] = [];
  private employeeScheduleDateIsos: string[] = [];
  employeeScheduleRows: EmployeeScheduleRow[] = [];
  selectedAssignedJob: MyAssignedJob | null = null;
  private employeeScheduleJobsByBusinessDate = new Map<string, MyAssignedJob[]>();

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

    this.loadBusinesses();
  }

  loadBusinesses(): void {
    this.loadingBusinesses = true;
    this.businessLoadError = null;

    this.businessService.getMyBusinesses().subscribe({
      next: (businesses) => {
        this.businesses = businesses;
        this.loadingBusinesses = false;
        this.loadEmployeeSchedule(businesses);
      },
      error: (error) => {
        this.loadingBusinesses = false;
        this.businesses = [];
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        const errorMessage = typeof error?.message === 'string' ? error.message.toLowerCase() : '';
        const isApiUnavailable = error?.status === 0 || errorMessage.includes('configured local origins');
        this.businessLoadError = isApiUnavailable
          ? 'The Holonix API could not be reached. Check that the local backend is running, then try again.'
          : 'Your businesses could not be loaded. Please try again.';
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

  goToBusinesses(): void {
    this.router.navigate(['/business']);
  }

  goToProfile(): void {
    this.router.navigate(['/profile']);
  }

  goToCreateBusiness(): void {
    this.router.navigate(['/business/create']);
  }

  goToBusinessWorkspace(businessCode: string): void {
    this.router.navigate(['/workspace', 'overview', businessCode]);
  }

  openBusinessWorkspace(business: UserBusinessSummary): void {
    this.goToBusinessWorkspace(business.businessCode);
  }

  toggleUserMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.isUserMenuOpen = !this.isUserMenuOpen;
  }

  toggleMobileMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
  }

  closeMobileMenu(): void {
    this.isMobileMenuOpen = false;
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

  private loadEmployeeSchedule(businesses: UserBusinessSummary[]): void {
    const weekStart = new Date();
    weekStart.setHours(0, 0, 0, 0);
    const days = Array.from({ length: 7 }, (_, index) => {
      const date = new Date(weekStart);
      date.setDate(weekStart.getDate() + index);
      return date;
    });

    this.employeeScheduleDays = days.map(day => new Intl.DateTimeFormat('en-US', { weekday: 'short', month: 'numeric', day: 'numeric' }).format(day));
    this.employeeScheduleDateIsos = days.map(day => this.toIsoDate(day));
    this.loadingEmployeeSchedule = businesses.length > 0;
    this.employeeScheduleRows = [];

    if (businesses.length === 0) {
      return;
    }

    forkJoin({
      availability: forkJoin(businesses.map(business => this.businessService.getMyBusinessAvailability(business.businessCode).pipe(catchError(() => of(null))))),
      jobs: this.businessService.getMyAssignedJobs(this.employeeScheduleDateIsos[0], this.employeeScheduleDateIsos[6]).pipe(catchError(() => of([]))),
    })
      .subscribe(({ availability: responses, jobs }) => {
        this.employeeScheduleJobsByBusinessDate.clear();
        jobs.forEach(job => {
          const key = `${job.businessCode}:${this.getJobWallClockDateIso(job.startDateTime)}`;
          const existing = this.employeeScheduleJobsByBusinessDate.get(key) ?? [];
          this.employeeScheduleJobsByBusinessDate.set(key, [...existing, job]);
        });
        this.employeeScheduleRows = businesses.map((business, index) => ({
          businessName: business.name,
          businessCode: business.businessCode,
          hours: days.map(day => this.getAvailabilityLabel(responses[index], day)),
        }));
        this.loadingEmployeeSchedule = false;
      });
  }

  employeeScheduleJobs(businessCode: string, dayIndex: number): MyAssignedJob[] {
    return this.employeeScheduleJobsByBusinessDate.get(`${businessCode}:${this.employeeScheduleDateIsos[dayIndex]}`) ?? [];
  }

  formatAssignedJob(job: MyAssignedJob): string {
    const start = this.parseJobWallClock(job.startDateTime);
    const time = Number.isNaN(start.getTime()) ? '' : new Intl.DateTimeFormat('en-US', { hour: 'numeric', minute: '2-digit' }).format(start);
    return time ? `${time} ${job.jobName}` : job.jobName;
  }

  openAssignedJob(job: MyAssignedJob): void {
    this.selectedAssignedJob = job;
  }

  closeAssignedJob(): void {
    this.selectedAssignedJob = null;
  }

  formatAssignedJobWindow(job: MyAssignedJob): string {
    const formatter = new Intl.DateTimeFormat('en-US', { weekday: 'long', month: 'long', day: 'numeric', hour: 'numeric', minute: '2-digit' });
    const start = this.parseJobWallClock(job.startDateTime);
    const end = this.parseJobWallClock(job.endDateTime);
    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) {
      return 'Schedule unavailable';
    }
    return `${formatter.format(start)} – ${new Intl.DateTimeFormat('en-US', { hour: 'numeric', minute: '2-digit' }).format(end)}`;
  }

  formatServiceLocation(job: MyAssignedJob): string {
    const location = job.serviceLocation;
    if (!location) {
      return 'Location unavailable';
    }
    return [location.address1, location.address2, `${location.city}, ${location.state} ${location.zipCode}`]
      .filter((part): part is string => !!part && part.trim().length > 0)
      .join(', ');
  }

  assignedJobMapsUrl(job: MyAssignedJob): string | null {
    return job.serviceLocation?.address1
      ? `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(this.formatServiceLocation(job))}`
      : null;
  }

  private getAvailabilityLabel(availability: import('../../../core/services/business.service').MyBusinessAvailability | null, date: Date): string {
    if (!availability) {
      return 'Unavailable';
    }

    const isoDate = this.toIsoDate(date);
    const dayOff = (availability.daysOff ?? []).some(item =>
      item.effectiveStartDate <= isoDate && item.effectiveEndDate >= isoDate && item.startTime === item.endTime
    );
    if (dayOff) {
      return 'Time off';
    }

    const frames = (availability.availability ?? []).filter(item => item.dayOfWeek === date.getDay());
    if (frames.length === 0) {
      return 'Unavailable';
    }

    return frames.map(frame => `${this.formatTime(frame.startTime)}–${this.formatTime(frame.endTime)}`).join(', ');
  }

  private toIsoDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private getJobWallClockDateIso(value: string): string {
    const match = (value ?? '').match(/^(\d{4}-\d{2}-\d{2})T/);
    return match?.[1] ?? this.toIsoDate(this.parseJobWallClock(value));
  }

  private parseJobWallClock(value: string): Date {
    const match = (value ?? '').match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2}))?/);
    if (!match) {
      return new Date(value);
    }

    return new Date(
      Number(match[1]),
      Number(match[2]) - 1,
      Number(match[3]),
      Number(match[4]),
      Number(match[5]),
      Number(match[6] ?? 0)
    );
  }

  private formatTime(value: string | null | undefined): string {
    const [hours, minutes] = (value ?? '').split(':').map(Number);
    if (!Number.isFinite(hours) || !Number.isFinite(minutes)) {
      return value ?? '';
    }
    return new Intl.DateTimeFormat('en-US', { hour: 'numeric', minute: '2-digit' }).format(new Date(2000, 0, 1, hours, minutes));
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    const element = target instanceof Element ? target : target?.parentElement;
    if (!element?.closest('.auth-actions-logged-in')) {
      this.isUserMenuOpen = false;
    }

    if (!element?.closest('.nav-left')) {
      this.isMobileMenuOpen = false;
    }
  }

  private buildImageDataUrl(base64: string | null): string {
    if (!base64 || !base64.trim()) {
      return '';
    }

    return `data:image/*;base64,${base64.trim()}`;
  }
}
