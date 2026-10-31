import { Component, HostListener, OnInit, OnDestroy } from '@angular/core';
import { EMPTY, Subscription, expand, forkJoin, reduce, switchMap } from 'rxjs';
import { JobsService, OperationalJob } from '../../../core/services/jobs.service';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  BusinessService,
  BusinessHoursDay,
  BusinessWorkspace,
  BusinessDailyTeamAvailabilityMember,
  MyBusinessAvailability,
  MyBusinessAvailabilityDay,
  MyBusinessTimeOff,
  UpdateMyBusinessAvailabilityRequest,
} from '../../../core/services/business.service';
import { AuthSessionService } from '../../../core/services/auth-session.service';

type AvailabilityDayEditor = {
  dayOfWeek: number;
  label: string;
  enabled: boolean;
  timeFrames: AvailabilityTimeFrameEditor[];
};

type AvailabilityTimeFrameEditor = {
  startTime: string;
  endTime: string;
};

type OpenTimePickerState = {
  dayOfWeek: number;
  frameIndex: number;
  field: 'start' | 'end';
  direction: 'up' | 'down';
  anchorTop: number;
  anchorBottom: number;
  anchorLeft: number;
  left: number;
  top: number;
  width: number;
};

type OpenTimeOffPickerState = {
  index: number; // -1 = new row
  field: 'start' | 'end';
  direction: 'up' | 'down';
  anchorTop: number;
  anchorBottom: number;
  anchorLeft: number;
  left: number;
  top: number;
  width: number;
};

type DayOffEditor = {
  businessUserTimeOffId: number;
  startDate: string;
  endDate: string;
  startTime: string;
  endTime: string;
};

type CalendarDay = {
  date: Date;
  isoDate: string;
  dayNumber: number;
  inMonth: boolean;
  isPast: boolean;
  isToday: boolean;
  status: 'timeOff' | 'available' | 'partialAvailable' | 'unavailable';
  pillLabel: string;
  detailLabel: string;
};

type LegendInfoKey = 'available' | 'partialAvailable' | 'timeOff' | 'unavailable';

@Component({
  selector: 'app-business-availability',
  templateUrl: './business-availability.component.html',
  styleUrls: ['./business-availability.component.css'],
})
export class BusinessAvailabilityComponent implements OnInit, OnDestroy {
  view: 'month' | 'week' | 'team' = 'month';
  timeOffOpen = false;
  fullDayOff = true;
  businessHoursOpen = false;
  savingBusinessHours = false;
  businessHoursError = '';
  businessHoursDraft: BusinessHoursDay[] = [];
  employeeFilter = 0;
  scheduleJobs: OperationalJob[] = [];
  selectedJob: OperationalJob | null = null;
  loadingJobs = false;
  jobsError = '';
  teamError = '';
  private jobsRequest?: Subscription;
  private teamRequest?: Subscription;
  private absenceRequest?: Subscription;
  private absenceSnapshot: DayOffEditor[] = [];
  absenceJobs: OperationalJob[] = [];
  loadingAbsenceJobs = false;
  absenceJobsError = '';
  teamWeek: Array<{ date: string; members: BusinessDailyTeamAvailabilityMember[] }> = [];
  private static readonly MobileNavBreakpointPx = 900;
  private static readonly TimeStepMinutes = 30;
  private static readonly TimePopoverMaxHeight = 280;
  private static readonly TimePopoverPadding = 8;
  private static readonly Midnight = '00:00';

  readonly availabilityDays: AvailabilityDayEditor[] = [
    { dayOfWeek: 1, label: 'Monday', enabled: false, timeFrames: [{ startTime: '09:00', endTime: '17:00' }] },
    { dayOfWeek: 2, label: 'Tuesday', enabled: false, timeFrames: [{ startTime: '09:00', endTime: '17:00' }] },
    { dayOfWeek: 3, label: 'Wednesday', enabled: false, timeFrames: [{ startTime: '09:00', endTime: '17:00' }] },
    { dayOfWeek: 4, label: 'Thursday', enabled: false, timeFrames: [{ startTime: '09:00', endTime: '17:00' }] },
    { dayOfWeek: 5, label: 'Friday', enabled: false, timeFrames: [{ startTime: '09:00', endTime: '17:00' }] },
    { dayOfWeek: 6, label: 'Saturday', enabled: false, timeFrames: [{ startTime: '09:00', endTime: '17:00' }] },
    { dayOfWeek: 0, label: 'Sunday', enabled: false, timeFrames: [{ startTime: '09:00', endTime: '17:00' }] },
  ];

  displayName = 'User';
  initials = 'U';
  profileImageDataUrl = '';
  isUserMenuOpen = false;
  isWorkspaceMenuOpen = false;
  isMobileNav = false;
  loadingWorkspace = true;
  loadingAvailability = true;
  availabilityError = '';
  savingAvailability = false;
  isEditingAvailability = false;
  private availabilityDaysSnapshot: AvailabilityDayEditor[] | null = null;
  businessWorkspace: BusinessWorkspace | null = null;
  currentBusinessUserId = 0;
  effectiveStartDate = '';
  openTimePicker: OpenTimePickerState | null = null;
  openTimeOffPicker: OpenTimeOffPickerState | null = null;
  readonly timeOptions = this.buildTimeOptions();

  daysOff: DayOffEditor[] = [];
  newDayOffStartDate = '';
  newDayOffEndDate = '';
  newDayOffStartTime = '00:00';
  newDayOffEndTime = '00:00';
  private readonly todayIsoUtc = new Date().toISOString().slice(0, 10);

  readonly calendarDayLabels = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
  calendarMonthLabel = '';
  calendarDays: CalendarDay[] = [];
  selectedCalendarDay: CalendarDay | null = null;
  loadingTeamAvailability = false;
  teamAvailabilityMembers: BusinessDailyTeamAvailabilityMember[] = [];
  legendInfoOpen = false;
  readonly legendInfoItems: Array<{
    key: LegendInfoKey;
    label: string;
    swatchClass: string;
    title: string;
    description: string;
  }> = [
    {
      key: 'available',
      label: 'Available',
      swatchClass: 'legend-swatch-on',
      title: 'Available',
      description: 'You have weekly availability hours set for this day, and no time off is affecting the day.',
    },
    {
      key: 'partialAvailable',
      label: 'Part. Avail',
      swatchClass: 'legend-swatch-partial',
      title: 'Part. Avail',
      description:
        'You have weekly availability hours, but time off overlaps some of those hours so only part of the day is available.',
    },
    {
      key: 'timeOff',
      label: 'Time off',
      swatchClass: 'legend-swatch-off',
      title: 'Time off',
      description:
        'Time off is applied for this day. This can be all-day time off, or time off that fully covers your weekly availability.',
    },
    {
      key: 'unavailable',
      label: 'Unavailable',
      swatchClass: 'legend-swatch-unavailable',
      title: 'Unavailable',
      description: 'No weekly availability hours are set for this day (and no time off is applied).',
    },
  ];
  private calendarMonthStart: Date = new Date();
  private readonly minCalendarMonthKey: number;
  private readonly maxCalendarMonthKey: number;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
    private readonly businessService: BusinessService,
    private readonly authSession: AuthSessionService,
    private readonly jobsService: JobsService
  ) {
    const today = new Date();
    this.minCalendarMonthKey = today.getFullYear() * 12 + today.getMonth();
    this.maxCalendarMonthKey = this.minCalendarMonthKey + 5;
  }

  ngOnInit(): void {
    this.refreshMobileNav();
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

  @HostListener('document:keydown.escape')
  closeEditor(): void {
    if (this.selectedJob) { this.selectedJob = null; return; }
    if (this.openTimePicker || this.openTimeOffPicker) { this.closePicker(); this.openTimeOffPicker = null; return; }
    if (!this.savingAvailability) { this.cancelEditingAvailability(); this.closeTimeOff(); }
    if (!this.savingBusinessHours) this.businessHoursOpen = false;
  }
  ngOnDestroy(): void { this.jobsRequest?.unsubscribe(); this.teamRequest?.unsubscribe(); this.absenceRequest?.unsubscribe(); }

  get weekDays(): CalendarDay[] {
    const anchor = this.selectedCalendarDay?.date ?? new Date();
    const start = new Date(anchor); start.setDate(start.getDate() - start.getDay());
    const first = this.toIsoDate(start);
    const end = new Date(start); end.setDate(end.getDate() + 6);
    return this.calendarDays.filter(d => d.isoDate >= first && d.isoDate <= this.toIsoDate(end));
  }
  get visibleDays(): CalendarDay[] { return this.view === 'month' ? this.calendarDays : this.weekDays; }
  get canEditBusinessHours(): boolean { return this.businessWorkspace?.currentUserRoleName?.toLowerCase() === 'owner'; }
  get invalidBusinessHours(): boolean { return this.businessHoursDraft.some(d => !d.isClosed && (!d.openTime || !d.closeTime || d.openTime >= d.closeTime)); }
  editBusinessHours(): void {
    if (!this.canEditBusinessHours) return;
    this.businessHoursDraft = this.availabilityDays.map(d => {
      const hours = this.businessWorkspace?.businessHours?.find(h => h.dayOfWeek === d.dayOfWeek);
      return { dayOfWeek: d.dayOfWeek, isClosed: hours?.isClosed ?? true, openTime: this.normalizeTime(hours?.openTime) || '09:00', closeTime: this.normalizeTime(hours?.closeTime) || '17:00' };
    });
    this.businessHoursError = ''; this.businessHoursOpen = true;
  }
  saveBusinessHours(): void {
    const code = this.businessWorkspace?.businessCode;
    if (!code || !this.canEditBusinessHours || this.invalidBusinessHours || this.savingBusinessHours) return;
    this.savingBusinessHours = true; this.businessHoursError = '';
    // Refresh the profile first: the existing endpoint accepts the complete profile.
    this.businessService.getBusinessWorkspace(code).pipe(switchMap(b => this.businessService.updateBusinessProfile(code, {
      name:b.name, description:b.description, businessIconBase64:b.businessIconBase64, businessEmail:b.businessEmail, businessPhoneNumber:b.businessPhoneNumber,
      businessHours:this.businessHoursDraft.map(d => ({...d, openTime:d.isClosed ? null : d.openTime + ':00', closeTime:d.isClosed ? null : d.closeTime + ':00'})),
      address1:b.address1 ?? '', address2:b.address2, city:b.city ?? '', state:b.state ?? '', zipCode:b.zipCode ?? '', businessJobPercentage:b.businessJobPercentage
    }))).subscribe({ next: response => { if (this.businessWorkspace) this.businessWorkspace.businessHours = response.businessHours; this.savingBusinessHours = false; this.businessHoursOpen = false; this.snackBar.open('Business hours updated.',undefined,{duration:2500}); }, error: err => { this.savingBusinessHours = false; this.businessHoursError = err?.error?.errors?.[0] ?? 'Could not update business hours.'; } });
  }
  get weekRangeLabel(): string { const days = this.weekDays; return days.length ? `${this.formatEffectiveDate(days[0].isoDate)} – ${this.formatEffectiveDate(days[days.length-1].isoDate)}` : ''; }
  get visibleTeam(): BusinessDailyTeamAvailabilityMember[] {
    const members = new Map<number, BusinessDailyTeamAvailabilityMember>();
    this.teamWeek.forEach(day => day.members.forEach(m => members.set(m.businessUserId, m)));
    return [...members.values()].sort((a,b) => a.lastName.localeCompare(b.lastName));
  }
  get filteredTeam(): BusinessDailyTeamAvailabilityMember[] { return this.visibleTeam.filter(m => !this.employeeFilter || m.businessUserId === +this.employeeFilter); }
  get upcomingTimeOff(): DayOffEditor[] { return this.daysOff.filter(d => d.endDate >= this.todayIsoUtc); }
  get selectedPersonalHours(): string {
    if (this.loadingTeamAvailability) return 'Loading dated working hours…';
    const self = this.teamAvailabilityMembers.find(m => m.businessUserId === this.currentBusinessUserId);
    return self ? this.teamMemberDetailLabel(self) : 'No dated working hours visible.';
  }
  businessHoursLabel(day: CalendarDay): string {
    const hours = this.businessWorkspace?.businessHours?.find(h => h.dayOfWeek === day.date.getDay());
    return !hours ? 'Business hours not set' : hours.isClosed ? 'Business closed' : `${this.timeLabel(this.normalizeTime(hours.openTime))} – ${this.timeLabel(this.normalizeTime(hours.closeTime))}`;
  }
  jobsFor(date: string, employeeId = 0): OperationalJob[] {
    return this.scheduleJobs.filter(j => j.startDateTime.slice(0,10) <= date && j.endDateTime.slice(0,19) > date + 'T00:00:00' && (!employeeId || j.assignedEmployees.some(e => e.businessUserId === +employeeId)));
  }
  memberFor(date: string, id: number): BusinessDailyTeamAvailabilityMember | undefined { return this.teamWeek.find(d => d.date === date)?.members.find(m => m.businessUserId === id); }
  teamCount(date: string): number | null {
    const day = this.teamWeek.find(d => d.date === date);
    return day ? day.members.filter(m => m.isAvailable && !m.isDayOff).length : null;
  }
  jobTime(job: OperationalJob): string { return `${job.startDateTime.slice(0,10)} · ${this.timeLabel(job.startDateTime.slice(11,16))} – ${job.endDateTime.slice(0,10) !== job.startDateTime.slice(0,10) ? job.endDateTime.slice(0,10) + ' · ' : ''}${this.timeLabel(job.endDateTime.slice(11,16))}`; }
  openJob(job: OperationalJob): void { this.selectedJob = job; }
  jobTeam(job: OperationalJob): string { return job.assignedEmployees.map(member => member.displayName).join(', ') || 'Needs assignment'; }
  jobLocation(job: OperationalJob): string {
    const location = job.serviceLocation;
    return location ? [location.address1, location.address2, location.city, location.state, location.zipCode].filter(Boolean).join(', ') : 'No service location recorded';
  }
  jobMapsUrl(job: OperationalJob): string | null { return job.serviceLocation?.address1 ? 'https://www.google.com/maps/search/?api=1&query=' + encodeURIComponent(this.jobLocation(job)) : null; }
  hasConflict(date: string, id: number): boolean {
    const jobs = this.jobsFor(date, id);
    const member = this.memberFor(date, id);
    if (jobs.some(a => jobs.some(b => a.jobId !== b.jobId && a.startDateTime < b.endDateTime && b.startDateTime < a.endDateTime))) return true;
    if (!member) return false;
    return jobs.some(job => {
      const start = job.startDateTime.slice(0,10) < date ? '00:00' : job.startDateTime.slice(11,16);
      const end = job.endDateTime.slice(0,10) > date ? '24:00' : job.endDateTime.slice(11,16);
      return !member.isAvailable || member.isDayOff || !member.timeFrames.some(f => this.normalizeTime(f.startTime) <= start && this.normalizeTime(f.endTime) >= end);
    });
  }
  freeWindows(date: string, id: number): string {
    const member = this.memberFor(date,id);
    if (!member?.isAvailable || member.isDayOff) return 'No working windows';
    const ranges = member.timeFrames.map(f => ({ start: this.timeToMinutes(this.normalizeTime(f.startTime)), end: this.timeToMinutes(this.normalizeTime(f.endTime)) }));
    const booked = this.jobsFor(date,id).map(j => ({ start: j.startDateTime.slice(0,10) < date ? 0 : this.timeToMinutes(j.startDateTime.slice(11,16)), end: j.endDateTime.slice(0,10) > date ? 1440 : this.timeToMinutes(j.endDateTime.slice(11,16)) }));
    return this.formatRanges(this.subtractRanges(ranges, this.normalizeRanges(booked))) || 'No unbooked windows';
  }
  setView(view: 'month' | 'week' | 'team'): void { this.view = view; this.loadTeamAvailabilityForSelectedDay(); }
  calendarToday(): void {
    const today = new Date(); this.calendarMonthStart = new Date(today.getFullYear(),today.getMonth(),1);
    this.selectedCalendarDay = null; this.rebuildCalendar();
  }
  navigateCalendar(direction: number): void {
    if (this.view === 'month') { direction < 0 ? this.prevCalendarMonth() : this.nextCalendarMonth(); return; }
    const date = new Date(this.selectedCalendarDay?.date ?? new Date()); date.setDate(date.getDate() + direction * 7);
    const key = this.calendarMonthKey(date);
    if (key < this.minCalendarMonthKey || key > this.maxCalendarMonthKey) return;
    this.calendarMonthStart = new Date(date.getFullYear(),date.getMonth(),1); this.rebuildCalendar();
    const day = this.calendarDays.find(d => d.isoDate === this.toIsoDate(date)); if (day) this.selectCalendarDay(day);
  }
  chooseDate(iso: string): void {
    const date = new Date(iso + 'T00:00:00'); if (isNaN(date.getTime())) return;
    if (this.calendarMonthKey(date) < this.minCalendarMonthKey || this.calendarMonthKey(date) > this.maxCalendarMonthKey) return;
    this.calendarMonthStart = new Date(date.getFullYear(),date.getMonth(),1); this.rebuildCalendar();
    const day = this.calendarDays.find(d => d.isoDate === iso); if (day) this.selectCalendarDay(day);
  }
  openTimeOff(prefill = true): void {
    if (this.availabilityError) return;
    this.absenceSnapshot = this.daysOff.map(d => ({...d})); this.timeOffOpen = true;
    const tomorrow = new Date(); tomorrow.setDate(tomorrow.getDate()+1);
    const selected = this.selectedCalendarDay?.isoDate ?? '';
    this.newDayOffStartDate = prefill ? (selected > this.todayIsoUtc ? selected : this.toIsoDate(tomorrow)) : '';
    this.newDayOffEndDate = this.newDayOffStartDate; this.setFullDayOff(true); this.loadAbsenceJobs();
  }
  get canSubmitTimeOff(): boolean { return this.canPersistAvailabilityAndTimeOff && (!(this.newDayOffStartDate || this.newDayOffEndDate) || this.canAddDayOff); }
  setFullDayOff(full: boolean): void { this.fullDayOff = full; this.newDayOffStartTime = full ? '00:00' : '09:00'; this.newDayOffEndTime = full ? '00:00' : '17:00'; }
  closeTimeOff(): void { if (this.savingAvailability || !this.timeOffOpen) return; this.daysOff = this.absenceSnapshot.map(d => ({...d})); this.timeOffOpen = false; this.openTimeOffPicker = null; this.absenceRequest?.unsubscribe(); this.rebuildCalendar(); }
  changeAbsenceDates(): void { this.onNewDayOffChanged(); if (this.newDayOffStartDate !== this.newDayOffEndDate) this.setFullDayOff(true); this.loadAbsenceJobs(); }
  private loadAbsenceJobs(): void {
    this.absenceRequest?.unsubscribe(); this.absenceJobs = []; this.absenceJobsError = '';
    const code = this.businessWorkspace?.businessCode;
    if (!code || !this.newDayOffStartDate || !this.newDayOffEndDate || this.newDayOffStartDate > this.newDayOffEndDate) { this.loadingAbsenceJobs = false; return; }
    this.loadingAbsenceJobs = true;
    const start = this.newDayOffStartDate, end = this.newDayOffEndDate;
    this.absenceRequest = this.jobsService.schedule(code,start,end).pipe(
      expand(result => result.page * result.pageSize < result.total ? this.jobsService.schedule(code,start,end,result.page+1) : EMPTY),
      reduce((all,result) => [...all,...result.jobs], [] as OperationalJob[])
    ).subscribe({ next: jobs => { this.absenceJobs = jobs.filter(j => j.status !== 'Cancelled' && j.status !== 'Completed'); this.loadingAbsenceJobs = false; }, error: () => { this.loadingAbsenceJobs = false; this.absenceJobsError = 'Could not check job conflicts. Review assigned work in Jobs before saving.'; } });
  }
  get timeOffConflicts(): OperationalJob[] {
    if (!this.newDayOffStartDate || !this.newDayOffEndDate) return [];
    const first = this.newDayOffStartDate + 'T' + (this.fullDayOff ? '00:00' : this.newDayOffStartTime) + ':00';
    const end = new Date(this.newDayOffEndDate + 'T00:00:00');
    if (this.fullDayOff) end.setDate(end.getDate()+1);
    const last = this.fullDayOff ? this.toIsoDate(end) + 'T00:00:00' : this.newDayOffEndDate + 'T' + this.newDayOffEndTime + ':00';
    return this.absenceJobs.filter(j => j.assignedEmployees.some(m => m.businessUserId === this.currentBusinessUserId) && j.startDateTime.slice(0,19) < last && j.endDateTime.slice(0,19) > first);
  }
  loadScheduleJobs(): void {
    const code = this.businessWorkspace?.businessCode; if (!code || !this.calendarDays.length) return;
    this.jobsRequest?.unsubscribe(); this.loadingJobs = true; this.jobsError = ''; this.scheduleJobs = [];
    const start = this.calendarDays[0].isoDate, end = this.calendarDays[this.calendarDays.length-1].isoDate;
    this.jobsRequest = this.jobsService.schedule(code,start,end).pipe(
      expand(result => result.page * result.pageSize < result.total ? this.jobsService.schedule(code,start,end,result.page+1) : EMPTY),
      reduce((all, result) => [...all, ...result.jobs], [] as OperationalJob[])
    ).subscribe({ next: jobs => { this.scheduleJobs = jobs.filter(j => j.status !== 'Cancelled' && j.status !== 'Completed').sort((a,b) => a.startDateTime.localeCompare(b.startDateTime)); this.loadingJobs = false; }, error: () => { this.loadingJobs = false; this.jobsError = 'Scheduled jobs could not be loaded. Availability does not include booking conflicts.'; } });
  }

  toggleWorkspaceMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.isWorkspaceMenuOpen = !this.isWorkspaceMenuOpen;
  }

  closeWorkspaceMenu(): void {
    this.isWorkspaceMenuOpen = false;
  }

  goToProfile(): void {
    this.router.navigate(['/profile']);
  }

  signOut(): void {
    this.isUserMenuOpen = false;
    this.authSession.logout();
  }

  toggleUserMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.isUserMenuOpen = !this.isUserMenuOpen;
  }

  goToWorkspace(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (businessCode) {
      this.router.navigate(['/workspace', 'overview', businessCode]);
    }
  }

  goToOverviewTab(): void {
    this.goToWorkspace();
  }

  goToEmployeesTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (businessCode) {
      this.router.navigate(['/workspace', 'employees', businessCode]);
    }
  }

  goToServiceManagerTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (businessCode) {
      this.router.navigate(['/workspace', 'services', businessCode]);
    }
  }

  goToMyAvailabilityTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (businessCode) {
      this.router.navigate(['/workspace', 'availability', businessCode]);
    }
  }

  goToJobsTab(): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (businessCode) {
      this.router.navigate(['/workspace', 'jobs', businessCode]);
    }
  }

  get canSaveAvailability(): boolean {
    if (!this.isEditingAvailability) {
      return false;
    }

    return this.canPersistAvailabilityAndTimeOff;
  }

  get enabledDayCount(): number {
    return this.availabilityDays.filter((day) => day.enabled).length;
  }

  get workingWindowCount(): number {
    return this.availabilityDays
      .filter((day) => day.enabled)
      .reduce((count, day) => count + day.timeFrames.length, 0);
  }

  private get canPersistAvailabilityAndTimeOff(): boolean {
    if (this.loadingAvailability || this.savingAvailability) {
      return false;
    }

    if (this.daysOffValidationMessage) {
      return false;
    }

    return this.availabilityDays.every((day) => !this.dayValidationMessage(day));
  }

  get canAddDayOff(): boolean {
    if (this.loadingAvailability || this.savingAvailability) {
      return false;
    }

    const start = (this.newDayOffStartDate ?? '').trim();
    const end = (this.newDayOffEndDate ?? '').trim();
    if (!start || !end) {
      return false;
    }

    if (start <= this.todayIsoUtc) {
      return false;
    }

    const startTime = (this.newDayOffStartTime ?? '').trim();
    const endTime = (this.newDayOffEndTime ?? '').trim();
    if (!startTime || !endTime) {
      return false;
    }

    if (end < start) {
      return false;
    }

    if (start !== end) {
      return startTime === BusinessAvailabilityComponent.Midnight && endTime === BusinessAvailabilityComponent.Midnight;
    }

    if (startTime === endTime) {
      return startTime === BusinessAvailabilityComponent.Midnight;
    }

    return startTime < endTime;
  }

  get daysOffValidationMessage(): string | null {
    const normalized = (this.daysOff ?? [])
      .map((item) => ({
        startDate: (item.startDate ?? '').trim(),
        endDate: (item.endDate ?? '').trim(),
        startTime: (item.startTime ?? '').trim(),
        endTime: (item.endTime ?? '').trim(),
      }))
      .filter((item) => !!item.startDate || !!item.endDate || !!item.startTime || !!item.endTime);

    if (normalized.some((item) => !item.startDate || !item.endDate)) {
      return 'Time off ranges must include both start and end dates.';
    }

    if (normalized.some((item) => item.endDate < item.startDate)) {
      return 'Time off end date must be on or after the start date.';
    }

    if (normalized.some((item) => !item.startTime || !item.endTime)) {
      return 'Time off must include both start and end times.';
    }

    if (normalized.some((item) => item.startTime === item.endTime && item.startTime !== BusinessAvailabilityComponent.Midnight)) {
      return 'All day time off must be 12:00 AM to 12:00 AM.';
    }

    if (normalized.some((item) => item.startDate !== item.endDate && (item.startTime !== BusinessAvailabilityComponent.Midnight || item.endTime !== BusinessAvailabilityComponent.Midnight))) {
      return 'Multi-day time off must be 12:00 AM to 12:00 AM.';
    }

    if (normalized.some((item) => item.startDate === item.endDate && item.startTime !== item.endTime && item.startTime >= item.endTime)) {
      return 'Time off end time must be after the start time.';
    }

    const allDay = normalized
      .filter((item) => item.startTime === BusinessAvailabilityComponent.Midnight && item.endTime === BusinessAvailabilityComponent.Midnight)
      .sort((a, b) => a.startDate.localeCompare(b.startDate) || a.endDate.localeCompare(b.endDate));

    for (let index = 1; index < allDay.length; index += 1) {
      const previous = allDay[index - 1];
      const current = allDay[index];
      if (current.startDate <= previous.endDate) {
        return 'Time off ranges cannot overlap.';
      }
    }

    const hourly = normalized
      .filter((item) => item.startTime !== item.endTime)
      .map((item) => ({
        date: item.startDate,
        startTime: item.startTime,
        endTime: item.endTime,
      }));

    if (hourly.some((item) => !item.date)) {
      return 'Time off ranges must include both start and end dates.';
    }

    if (normalized.some((item) => item.startTime !== item.endTime && item.startDate !== item.endDate)) {
      return 'Hourly time off must be a single day.';
    }

    for (const entry of hourly) {
      if (allDay.some((range) => range.startDate <= entry.date && range.endDate >= entry.date)) {
        return 'Time off ranges cannot overlap.';
      }
    }

    const byDate = new Map<string, { startTime: string; endTime: string }[]>();
    for (const entry of hourly) {
      const existing = byDate.get(entry.date) ?? [];
      byDate.set(entry.date, [...existing, { startTime: entry.startTime, endTime: entry.endTime }]);
    }

    for (const frames of byDate.values()) {
      const ordered = [...frames].sort((a, b) => a.startTime.localeCompare(b.startTime) || a.endTime.localeCompare(b.endTime));
      for (let index = 1; index < ordered.length; index += 1) {
        const previous = ordered[index - 1];
        const current = ordered[index];
        if (current.startTime < previous.endTime) {
          return 'Time off ranges cannot overlap.';
        }
      }
    }

    return null;
  }

  isDayInvalid(day: AvailabilityDayEditor): boolean {
    return !!this.dayValidationMessage(day);
  }

  setDayEnabled(day: AvailabilityDayEditor, enabled: boolean, rebuild = true): void {
    if (!this.isEditingAvailability) {
      return;
    }

    day.enabled = enabled;
    if (enabled) {
      if (!day.timeFrames || day.timeFrames.length === 0) {
        day.timeFrames = [{ startTime: '09:00', endTime: '17:00' }];
      }

      this.normalizeDayTimeFrames(day);
    }

    if (rebuild) {
      this.rebuildCalendar();
    }
  }

  applyWeekdaysNineToFive(): void {
    if (!this.isEditingAvailability) {
      return;
    }

    for (const day of this.availabilityDays) {
      if (day.dayOfWeek >= 1 && day.dayOfWeek <= 5) {
        this.setDayEnabled(day, true, false);
        day.timeFrames = [{ startTime: '09:00', endTime: '17:00' }];
      } else {
        this.setDayEnabled(day, false, false);
      }
    }

    this.rebuildCalendar();
  }

  applyAllAvailableNineToFive(): void {
    if (!this.isEditingAvailability) {
      return;
    }

    for (const day of this.availabilityDays) {
      this.setDayEnabled(day, true, false);
      day.timeFrames = [{ startTime: '09:00', endTime: '17:00' }];
    }

    this.rebuildCalendar();
  }

  applyAllUnavailable(): void {
    if (!this.isEditingAvailability) {
      return;
    }

    for (const day of this.availabilityDays) {
      this.setDayEnabled(day, false, false);
    }

    this.rebuildCalendar();
  }

  addDayOff(): void {
    if (!this.canAddDayOff) {
      return;
    }

    const startDate = (this.newDayOffStartDate ?? '').trim();
    const endDate = (this.newDayOffEndDate ?? '').trim();
    if (!startDate || !endDate) {
      return;
    }

    let startTime = (this.newDayOffStartTime ?? '').trim() || '00:00';
    let endTime = (this.newDayOffEndTime ?? '').trim() || '00:00';
    if (startDate !== endDate) {
      startTime = BusinessAvailabilityComponent.Midnight;
      endTime = BusinessAvailabilityComponent.Midnight;
    }

    this.daysOff = [
      ...this.daysOff,
      {
        businessUserTimeOffId: 0,
        startDate,
        endDate,
        startTime,
        endTime,
      },
    ];

    this.newDayOffStartDate = '';
    this.newDayOffEndDate = '';
    this.newDayOffStartTime = BusinessAvailabilityComponent.Midnight;
    this.newDayOffEndTime = BusinessAvailabilityComponent.Midnight;

    this.normalizeDaysOff();
    this.rebuildCalendar();
  }

  removeDayOff(index: number): void {
    if (index < 0 || index >= this.daysOff.length) {
      return;
    }

    const target = this.daysOff[index] ?? null;
    if (target && this.isTimeOffLocked(target)) {
      return;
    }

    this.daysOff = this.daysOff.filter((_, itemIndex) => itemIndex !== index);
    this.normalizeDaysOff();
    this.rebuildCalendar();
  }

  onDaysOffChanged(): void {
    this.normalizeDaysOff();
    this.rebuildCalendar();
  }

  isTimeOffLocked(dayOff: DayOffEditor): boolean {
    const start = (dayOff?.startDate ?? '').trim();
    if (!start) {
      return false;
    }

    return start <= this.todayIsoUtc;
  }

  onNewDayOffChanged(): void {
    const start = (this.newDayOffStartDate ?? '').trim();
    const end = (this.newDayOffEndDate ?? '').trim();
    if (!start || !end || start !== end) {
      this.newDayOffStartTime = BusinessAvailabilityComponent.Midnight;
      this.newDayOffEndTime = BusinessAvailabilityComponent.Midnight;
      return;
    }

    this.newDayOffStartTime = (this.newDayOffStartTime ?? '').trim() || BusinessAvailabilityComponent.Midnight;
    this.newDayOffEndTime = (this.newDayOffEndTime ?? '').trim() || BusinessAvailabilityComponent.Midnight;
    if (
      this.newDayOffStartTime === this.newDayOffEndTime &&
      this.newDayOffStartTime !== BusinessAvailabilityComponent.Midnight
    ) {
      this.newDayOffStartTime = BusinessAvailabilityComponent.Midnight;
      this.newDayOffEndTime = BusinessAvailabilityComponent.Midnight;
    }
  }

  get canGoPrevCalendarMonth(): boolean {
    return this.calendarMonthKey(this.calendarMonthStart) > this.minCalendarMonthKey;
  }

  get canGoNextCalendarMonth(): boolean {
    return this.calendarMonthKey(this.calendarMonthStart) < this.maxCalendarMonthKey;
  }

  prevCalendarMonth(): void {
    if (!this.canGoPrevCalendarMonth) {
      return;
    }

    this.calendarMonthStart = new Date(this.calendarMonthStart.getFullYear(), this.calendarMonthStart.getMonth() - 1, 1);
    this.rebuildCalendar();
  }

  nextCalendarMonth(): void {
    if (!this.canGoNextCalendarMonth) {
      return;
    }

    this.calendarMonthStart = new Date(this.calendarMonthStart.getFullYear(), this.calendarMonthStart.getMonth() + 1, 1);
    this.rebuildCalendar();
  }

  selectCalendarDay(day: CalendarDay): void {
    this.selectedCalendarDay = day;
    this.loadTeamAvailabilityForSelectedDay();
  }

  toggleLegendInfo(event: MouseEvent): void {
    event.stopPropagation();
    this.legendInfoOpen = !this.legendInfoOpen;
  }

  get hasVisibleTeamAvailability(): boolean {
    return (this.teamAvailabilityMembers ?? []).length > 0;
  }

  teamMemberDetailLabel(member: BusinessDailyTeamAvailabilityMember): string {
    if (!member) {
      return '';
    }

    if (member.isDayOff) {
      return 'Time off';
    }

    if (!member.isAvailable) {
      return 'Unavailable';
    }

    const frames = (member.timeFrames ?? [])
      .map((frame) => {
        const start = this.normalizeTime(frame?.startTime);
        const end = this.normalizeTime(frame?.endTime);
        if (!start || !end) {
          return '';
        }
        return `${this.timeLabel(start)} - ${this.timeLabel(end)}`;
      })
      .filter(Boolean);

    return frames.length > 0 ? frames.join(', ') : 'Available';
  }

  saveAvailability(): void {
    if (!this.isEditingAvailability || !this.canSaveAvailability) {
      return;
    }

    this.persistAvailabilityAndTimeOff(true);
  }

  submitTimeOff(): void {
    if (!this.canSubmitTimeOff) return;
    if (this.canAddDayOff) {
      this.addDayOff();
    }

    this.persistAvailabilityAndTimeOff(false);
  }

  startEditingAvailability(): void {
    if (this.loadingAvailability || this.savingAvailability || this.availabilityError) {
      return;
    }

    this.availabilityDaysSnapshot = this.cloneAvailabilityDays();
    this.isEditingAvailability = true;
  }

  cancelEditingAvailability(): void {
    if (!this.isEditingAvailability) {
      return;
    }

    this.closePicker();

    if (this.availabilityDaysSnapshot) {
      this.applyAvailabilitySnapshot(this.availabilityDaysSnapshot);
    }

    this.availabilityDaysSnapshot = null;
    this.isEditingAvailability = false;
    this.rebuildCalendar();
  }

  private persistAvailabilityAndTimeOff(exitEditMode: boolean): void {
    const businessCode = this.businessWorkspace?.businessCode?.trim();
    if (!businessCode || !this.canPersistAvailabilityAndTimeOff) {
      return;
    }

    const payload: UpdateMyBusinessAvailabilityRequest = {
      availability: this.availabilityDays
        .filter((day) => day.enabled)
        .flatMap((day) =>
          (day.timeFrames ?? []).map((frame) => ({
            dayOfWeek: day.dayOfWeek,
            startTime: `${frame.startTime}:00`,
            endTime: `${frame.endTime}:00`,
          }))
        ),
      daysOff: this.daysOff.map((item) => ({
        effectiveStartDate: item.startDate,
        effectiveEndDate: item.endDate,
        startTime: `${(item.startTime ?? '00:00').trim() || '00:00'}:00`,
        endTime: `${(item.endTime ?? '00:00').trim() || '00:00'}:00`,
      })),
    };

    this.savingAvailability = true;
    this.businessService.updateMyBusinessAvailability(businessCode, payload).subscribe({
      next: (response) => {
        this.applyAvailability(response);
        if (exitEditMode) {
          this.isEditingAvailability = false;
          this.availabilityDaysSnapshot = null;
        }
        this.savingAvailability = false;
        this.timeOffOpen = false;
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

  isTimePickerOpen(day: AvailabilityDayEditor, frameIndex: number, field: 'start' | 'end'): boolean {
    return !!this.openTimePicker
      && this.openTimePicker.dayOfWeek === day.dayOfWeek
      && this.openTimePicker.frameIndex === frameIndex
      && this.openTimePicker.field === field;
  }

  timePopoverStyle(
    day: AvailabilityDayEditor,
    frameIndex: number,
    field: 'start' | 'end'
  ): Record<string, string> | null {
    if (!this.isTimePickerOpen(day, frameIndex, field) || !this.openTimePicker) {
      return null;
    }

    return {
      left: `${this.openTimePicker.left}px`,
      top: `${this.openTimePicker.top}px`,
      width: `${this.openTimePicker.width}px`,
    };
  }

  openPicker(day: AvailabilityDayEditor, frameIndex: number, field: 'start' | 'end', event: MouseEvent): void {
    event.stopPropagation();
    if (!this.isEditingAvailability || !day.enabled) {
      return;
    }

    if (frameIndex < 0 || frameIndex >= (day.timeFrames ?? []).length) {
      return;
    }

    const anchor = event.currentTarget instanceof HTMLElement ? event.currentTarget : null;
    const rect = anchor?.getBoundingClientRect() ?? null;
    const anchorTop = rect?.top ?? 0;
    const anchorBottom = rect?.bottom ?? 0;
    const anchorLeft = rect?.left ?? 0;

    const width = this.computePopoverWidth();
    const left = this.clamp(anchorLeft, BusinessAvailabilityComponent.TimePopoverPadding, window.innerWidth - width - BusinessAvailabilityComponent.TimePopoverPadding);

    const direction = this.computePopoverDirection(anchorTop, anchorBottom);
    const top =
      direction === 'down'
        ? anchorBottom + BusinessAvailabilityComponent.TimePopoverPadding
        : Math.max(
            BusinessAvailabilityComponent.TimePopoverPadding,
            anchorTop - BusinessAvailabilityComponent.TimePopoverPadding - BusinessAvailabilityComponent.TimePopoverMaxHeight
          );

    this.openTimePicker = {
      dayOfWeek: day.dayOfWeek,
      frameIndex,
      field,
      direction,
      anchorTop,
      anchorBottom,
      anchorLeft,
      left,
      top,
      width,
    };
    this.openTimeOffPicker = null;

    // Let Angular render the popover before attempting to scroll within it.
    setTimeout(() => {
      if (this.isTimePickerOpen(day, frameIndex, field)) {
        this.repositionOpenTimePicker(day.dayOfWeek, frameIndex, field);
        this.scrollOpenTimePickerToSelection(day.dayOfWeek, frameIndex, field);
      }
    }, 0);
  }

  closePicker(): void {
    this.openTimePicker = null;
    this.openTimeOffPicker = null;
  }

  selectTime(day: AvailabilityDayEditor, frameIndex: number, field: 'start' | 'end', value: string): void {
    if (!day.enabled) {
      return;
    }

    if (frameIndex < 0 || frameIndex >= (day.timeFrames ?? []).length) {
      return;
    }

    const frame = day.timeFrames[frameIndex];
    if (field === 'start') {
      frame.startTime = value;

      if (frame.endTime && frame.endTime <= frame.startTime) {
        const next = this.nextTimeOption(value);
        frame.endTime = next ?? frame.endTime;
      }
    } else {
      frame.endTime = value;
    }

    this.normalizeDayTimeFrames(day);
    this.closePicker();
    this.rebuildCalendar();
  }

  timeLabel(value: string): string {
    const match = /^(\d{2}):(\d{2})$/.exec((value ?? '').trim());
    if (!match) {
      return value;
    }

    const hours = Number(match[1]);
    const minutes = Number(match[2]);
    if (!Number.isFinite(hours) || !Number.isFinite(minutes)) {
      return value;
    }

    const isPm = hours >= 12;
    const displayHours = hours % 12 === 0 ? 12 : hours % 12;
    const minuteLabel = `${minutes}`.padStart(2, '0');
    return `${displayHours}:${minuteLabel} ${isPm ? 'PM' : 'AM'}`;
  }

  weeklyHoursLabel(day: AvailabilityDayEditor): string {
    if (!day.enabled) {
      return '—';
    }

    const frames = [...(day.timeFrames ?? [])]
      .map((frame) => ({
        startTime: (frame.startTime ?? '').trim(),
        endTime: (frame.endTime ?? '').trim(),
      }))
      .filter((frame) => !!frame.startTime && !!frame.endTime)
      .sort((a, b) => a.startTime.localeCompare(b.startTime) || a.endTime.localeCompare(b.endTime));

    if (frames.length === 0) {
      return '—';
    }

    return frames.map((frame) => `${this.timeLabel(frame.startTime)} to ${this.timeLabel(frame.endTime)}`).join(' • ');
  }

  endTimeOptions(day: AvailabilityDayEditor, frameIndex: number): string[] {
    if (!day.enabled || frameIndex < 0 || frameIndex >= (day.timeFrames ?? []).length) {
      return this.timeOptions;
    }

    const start = (day.timeFrames?.[frameIndex]?.startTime ?? '').trim();
    if (!/^\d{2}:\d{2}$/.test(start)) {
      return this.timeOptions;
    }

    return this.timeOptions.filter((option) => option > start);
  }

  canEditTimeOffHours(startDate: string, endDate: string): boolean {
    const start = (startDate ?? '').trim();
    const end = (endDate ?? '').trim();
    if (!start || !end || start !== end) {
      return false;
    }

    return start > this.todayIsoUtc;
  }

  isTimeOffPickerOpen(index: number, field: 'start' | 'end'): boolean {
    return !!this.openTimeOffPicker
      && this.openTimeOffPicker.index === index
      && this.openTimeOffPicker.field === field;
  }

  timeOffPopoverStyle(index: number, field: 'start' | 'end'): Record<string, string> | null {
    if (!this.isTimeOffPickerOpen(index, field) || !this.openTimeOffPicker) {
      return null;
    }

    return {
      left: `${this.openTimeOffPicker.left}px`,
      top: `${this.openTimeOffPicker.top}px`,
      width: `${this.openTimeOffPicker.width}px`,
    };
  }

  timeOffEndTimeOptions(index: number): string[] {
    const start =
      index === -1
        ? (this.newDayOffStartTime ?? '').trim()
        : (this.daysOff?.[index]?.startTime ?? '').trim();

    if (!/^\d{2}:\d{2}$/.test(start)) {
      return this.timeOptions;
    }

    return this.timeOptions.filter((option) => option > start);
  }

  openTimeOffPickerFor(index: number, field: 'start' | 'end', event: MouseEvent): void {
    event.stopPropagation();

    const range =
      index === -1
        ? { startDate: this.newDayOffStartDate, endDate: this.newDayOffEndDate }
        : (this.daysOff?.[index] ?? null);

    if (!range || !this.canEditTimeOffHours(range.startDate, range.endDate)) {
      return;
    }

    const anchor = event.currentTarget instanceof HTMLElement ? event.currentTarget : null;
    const rect = anchor?.getBoundingClientRect() ?? null;
    const anchorTop = rect?.top ?? 0;
    const anchorBottom = rect?.bottom ?? 0;
    const anchorLeft = rect?.left ?? 0;

    const width = this.computePopoverWidth();
    const left = this.clamp(
      anchorLeft,
      BusinessAvailabilityComponent.TimePopoverPadding,
      window.innerWidth - width - BusinessAvailabilityComponent.TimePopoverPadding
    );

    const direction = this.computePopoverDirection(anchorTop, anchorBottom);
    const top =
      direction === 'down'
        ? anchorBottom + BusinessAvailabilityComponent.TimePopoverPadding
        : Math.max(
            BusinessAvailabilityComponent.TimePopoverPadding,
            anchorTop -
              BusinessAvailabilityComponent.TimePopoverPadding -
              BusinessAvailabilityComponent.TimePopoverMaxHeight
          );

    this.openTimeOffPicker = {
      index,
      field,
      direction,
      anchorTop,
      anchorBottom,
      anchorLeft,
      left,
      top,
      width,
    };
    this.openTimePicker = null;

    setTimeout(() => {
      if (this.isTimeOffPickerOpen(index, field)) {
        this.repositionOpenTimeOffPicker(index, field);
        this.scrollOpenTimeOffPickerToSelection(index, field);
      }
    }, 0);
  }

  selectTimeOff(index: number, field: 'start' | 'end', value: string): void {
    if (index !== -1 && (index < 0 || index >= (this.daysOff ?? []).length)) {
      return;
    }

    if (index !== -1) {
      const dayOff = this.daysOff[index];
      if (field === 'start') {
        dayOff.startTime = value;
        if (dayOff.endTime && dayOff.endTime <= dayOff.startTime) {
          dayOff.endTime = this.nextTimeOption(value) ?? dayOff.endTime;
        }
      } else {
        dayOff.endTime = value;
      }

      this.onDaysOffChanged();
      this.closePicker();
      return;
    }

    // new row
    if (field === 'start') {
      this.newDayOffStartTime = value;
      if (this.newDayOffEndTime && this.newDayOffEndTime <= this.newDayOffStartTime) {
        this.newDayOffEndTime = this.nextTimeOption(value) ?? this.newDayOffEndTime;
      }
    } else {
      this.newDayOffEndTime = value;
    }

    this.onNewDayOffChanged();
    this.closePicker();
  }

  addTimeFrame(day: AvailabilityDayEditor): void {
    if (!this.isEditingAvailability || !day.enabled) {
      return;
    }

    const frames = [...(day.timeFrames ?? [])].sort((a, b) => a.startTime.localeCompare(b.startTime));
    const last = frames[frames.length - 1] ?? null;

    const proposedStart = (last?.endTime ?? '').trim() || '09:00';
    const proposedEnd = this.nextTimeOption(proposedStart) ?? '17:00';

    day.timeFrames = [...frames, { startTime: proposedStart, endTime: proposedEnd }];
    this.normalizeDayTimeFrames(day);
    this.rebuildCalendar();
  }

  removeTimeFrame(day: AvailabilityDayEditor, frameIndex: number): void {
    if (!this.isEditingAvailability || !day.enabled) {
      return;
    }

    if (frameIndex < 0 || frameIndex >= (day.timeFrames ?? []).length) {
      return;
    }

    if ((day.timeFrames ?? []).length <= 1) {
      return;
    }

    day.timeFrames = (day.timeFrames ?? []).filter((_, index) => index !== frameIndex);
    this.normalizeDayTimeFrames(day);
    this.rebuildCalendar();
  }

  dayValidationMessage(day: AvailabilityDayEditor): string | null {
    if (!day.enabled) {
      return null;
    }

    const frames = this.validatedFrames(day);
    if (frames.length === 0) {
      return 'Add at least one time frame.';
    }

    if (frames.some((frame) => !frame.startTime || !frame.endTime)) {
      return 'Time frames must include both start and end times.';
    }

    if (frames.some((frame) => frame.startTime >= frame.endTime)) {
      return 'End time must be after start time.';
    }

    const ordered = [...frames].sort((a, b) => a.startTime.localeCompare(b.startTime));
    for (let index = 1; index < ordered.length; index += 1) {
      const previous = ordered[index - 1];
      const current = ordered[index];
      if (current.startTime < previous.endTime) {
        return 'Time frames cannot overlap.';
      }
    }

    return null;
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

    if (!element?.closest('.workspace-mobile-menu')) {
      this.isWorkspaceMenuOpen = false;
    }

    if (!element?.closest('.time-picker-shell')) {
      this.closePicker();
    }

    if (!element?.closest('.legend-info-shell')) {
      this.legendInfoOpen = false;
    }
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.refreshMobileNav();
  }

  private refreshMobileNav(): void {
    if (typeof window === 'undefined') {
      this.isMobileNav = false;
      return;
    }

    this.isMobileNav = window.innerWidth <= BusinessAvailabilityComponent.MobileNavBreakpointPx;
    if (!this.isMobileNav) {
      this.isWorkspaceMenuOpen = false;
    }
  }

  loadAvailability(businessCode: string): void {
    this.loadingAvailability = true;
    this.availabilityError = '';
    this.businessService.getMyBusinessAvailability(businessCode).subscribe({
      next: (response) => {
        this.applyAvailability(response);
        this.loadingAvailability = false;
      },
      error: (err) => {
        this.loadingAvailability = false;
        this.availabilityError = 'Your working hours could not be loaded. Retry before making changes.';
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
    const byDay = new Map<number, MyBusinessAvailabilityDay[]>();
    for (const entry of response.availability ?? []) {
      const dayOfWeek = entry.dayOfWeek;
      const existing = byDay.get(dayOfWeek) ?? [];
      byDay.set(dayOfWeek, [...existing, entry]);
    }

    this.currentBusinessUserId = response.businessUserId ?? 0;
    this.effectiveStartDate = response.effectiveStartDate ?? '';
    const hasSavedAvailability = (response.availability ?? []).some((entry) =>
      !!this.normalizeTime(entry?.startTime) && !!this.normalizeTime(entry?.endTime));

    for (const day of this.availabilityDays) {
      const matches = byDay.get(day.dayOfWeek) ?? [];
      const frames = matches
        .map((match) => ({
          startTime: this.normalizeTime(match?.startTime) || '',
          endTime: this.normalizeTime(match?.endTime) || '',
        }))
        .filter((frame) => !!frame.startTime && !!frame.endTime)
        .sort((a, b) => a.startTime.localeCompare(b.startTime));

      if (!hasSavedAvailability) {

        day.enabled = false;
        day.timeFrames = [{ startTime: '09:00', endTime: '17:00' }];
      } else {
        day.enabled = frames.length > 0;
        day.timeFrames = frames.length > 0 ? frames : [{ startTime: '09:00', endTime: '17:00' }];
      }
    }

    this.daysOff = this.mapTimeOff(response.daysOff ?? []);
    this.normalizeDaysOff();

    const today = new Date();
    this.calendarMonthStart = new Date(today.getFullYear(), today.getMonth(), 1);
    this.availabilityDaysSnapshot = null;
    this.isEditingAvailability = false;
    this.closePicker();
    this.rebuildCalendar();
  }

  private loadTeamAvailabilityForSelectedDay(): void {
    const businessCode = (this.businessWorkspace?.businessCode ?? '').trim();
    const dateIso = (this.selectedCalendarDay?.isoDate ?? '').trim();
    if (!businessCode || !dateIso) {
      this.teamAvailabilityMembers = [];
      this.loadingTeamAvailability = false;
      return;
    }

    this.teamRequest?.unsubscribe();
    this.loadingTeamAvailability = true; this.teamError = ''; this.teamWeek = []; this.teamAvailabilityMembers = [];
    const dates = this.weekDays.map(d => d.isoDate);
    this.teamRequest = forkJoin(dates.map(date => this.businessService.getBusinessTeamDailyAvailability(businessCode,date))).subscribe({
      next: responses => {
        this.teamWeek = responses.map((response,i) => ({ date: dates[i], members: response.members ?? [] }));
        this.teamAvailabilityMembers = this.teamWeek.find(d => d.date === dateIso)?.members ?? [];
        this.loadingTeamAvailability = false;
      },
      error: () => { this.loadingTeamAvailability = false; this.teamError = 'Team availability could not be loaded. Try selecting the date again.'; }
    });
  }
  private mapTimeOff(rows: MyBusinessTimeOff[]): DayOffEditor[] {
    return (rows ?? []).map((row) => ({
      businessUserTimeOffId: row.businessUserTimeOffId ?? 0,
      startDate: (row.effectiveStartDate ?? '').trim(),
      endDate: (row.effectiveEndDate ?? '').trim(),
      startTime: this.normalizeTime(row.startTime) || '00:00',
      endTime: this.normalizeTime(row.endTime) || '00:00',
    }));
  }

  private normalizeDaysOff(): void {
    this.daysOff = (this.daysOff ?? [])
      .map((item) => ({
        ...item,
        startDate: (item.startDate ?? '').trim(),
        endDate: (item.endDate ?? '').trim(),
        startTime: (item.startTime ?? '').trim() || '00:00',
        endTime: (item.endTime ?? '').trim() || '00:00',
      }))
      .map((item) => {
        if (item.startDate && item.endDate && item.startDate !== item.endDate) {
          return { ...item, startTime: '00:00', endTime: '00:00' };
        }

        return item;
      })
      .filter((item) => !!item.startDate || !!item.endDate || !!item.startTime || !!item.endTime)
      .sort((a, b) => a.startDate.localeCompare(b.startDate) || a.startTime.localeCompare(b.startTime));
  }

  private rebuildCalendar(): void {
    const monthStart = new Date(this.calendarMonthStart);
    monthStart.setHours(0, 0, 0, 0);

    const key = this.calendarMonthKey(monthStart);
    if (key < this.minCalendarMonthKey) {
      const today = new Date();
      this.calendarMonthStart = new Date(today.getFullYear(), today.getMonth(), 1);
    } else if (key > this.maxCalendarMonthKey) {
      const maxYear = Math.floor(this.maxCalendarMonthKey / 12);
      const maxMonth = this.maxCalendarMonthKey % 12;
      this.calendarMonthStart = new Date(maxYear, maxMonth, 1);
    }

    const displayStart = new Date(this.calendarMonthStart);
    displayStart.setHours(0, 0, 0, 0);

    this.calendarMonthLabel = new Intl.DateTimeFormat('en-US', {
      month: 'long',
      year: 'numeric',
    }).format(displayStart);

    const firstDow = displayStart.getDay(); // Sunday=0
    const gridStart = new Date(displayStart);
    gridStart.setDate(displayStart.getDate() - firstDow);

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const days: CalendarDay[] = [];
    for (let offset = 0; offset < 42; offset += 1) {
      const date = new Date(gridStart);
      date.setDate(gridStart.getDate() + offset);
      date.setHours(0, 0, 0, 0);

      const isoDate = this.toIsoDate(date);
      const inMonth = date.getMonth() === displayStart.getMonth();
      const isPast = date.getTime() < today.getTime();
      const isToday = date.getTime() === today.getTime();

      const status = this.getDateStatus(date, isoDate);
      const pillLabel =
        status === 'timeOff'
          ? 'Off'
          : status === 'partialAvailable'
            ? 'Part. Avail'
            : status === 'available'
              ? 'Avail'
              : 'Unavailable';
      const detailLabel = this.getDateDetailLabel(date, isoDate, status);

      days.push({
        date,
        isoDate,
        dayNumber: date.getDate(),
        inMonth,
        isPast,
        isToday,
        status,
        pillLabel,
        detailLabel,
      });
    }

    this.calendarDays = days;
    this.selectedCalendarDay = days.find(day => day.isoDate === this.selectedCalendarDay?.isoDate)
      ?? days.find(day => day.isToday) ?? days.find(day => day.inMonth) ?? null;
    if (!this.isEditingAvailability) { this.loadTeamAvailabilityForSelectedDay(); this.loadScheduleJobs(); }
  }
  private isDateDayOff(isoDate: string): boolean {
    return (this.daysOff ?? []).some((range) => range.startDate <= isoDate && range.endDate >= isoDate);
  }

  private getDateStatus(date: Date, isoDate: string): 'timeOff' | 'available' | 'partialAvailable' | 'unavailable' {
    const { allDay, partial } = this.getTimeOffForDate(isoDate);
    if (allDay) {
      return 'timeOff';
    }

    const dayOfWeek = date.getDay();
    const weekly = this.availabilityDays.find((item) => item.dayOfWeek === dayOfWeek) ?? null;
    const weeklyRanges = this.weeklyRanges(weekly);

    if (weeklyRanges.length === 0) {
      return partial.length > 0 ? 'timeOff' : 'unavailable';
    }

    if (partial.length === 0) {
      return 'available';
    }

    const availableAfterOff = this.subtractRanges(weeklyRanges, partial);
    if (availableAfterOff.length === 0) {
      return 'timeOff';
    }

    return 'partialAvailable';
  }

  private getDateDetailLabel(
    date: Date,
    isoDate: string,
    status: 'timeOff' | 'available' | 'partialAvailable' | 'unavailable'
  ): string {
    if (status === 'unavailable') {
      return 'Unavailable';
    }

    const dayOfWeek = date.getDay();
    const weekly = this.availabilityDays.find((item) => item.dayOfWeek === dayOfWeek) ?? null;
    const weeklyRanges = this.weeklyRanges(weekly);

    const { allDay, partial } = this.getTimeOffForDate(isoDate);
    if (allDay) {
      return 'Time off (all day)';
    }

    if (status === 'timeOff') {
      if (partial.length > 0) {
        return `Off: ${this.formatRanges(partial)}`;
      }

      return 'Time off (overrides weekly availability)';
    }

    if (weeklyRanges.length === 0) {
      return 'Unavailable';
    }

    if (status === 'available') {
      return this.formatRanges(weeklyRanges);
    }

    const offRelevant = partial.length > 0 ? this.intersectRanges(partial, weeklyRanges) : [];
    const availableAfterOff = this.subtractRanges(weeklyRanges, partial);

    const availableLabel = availableAfterOff.length > 0 ? this.formatRanges(availableAfterOff) : 'Unavailable';
    const offLabel = offRelevant.length > 0 ? this.formatRanges(offRelevant) : this.formatRanges(partial);

    return `Available: ${availableLabel} • Off: ${offLabel}`;
  }

  private calendarMonthKey(value: Date): number {
    return value.getFullYear() * 12 + value.getMonth();
  }

  private toIsoDate(value: Date): string {
    const year = value.getFullYear();
    const month = `${value.getMonth() + 1}`.padStart(2, '0');
    const day = `${value.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private normalizeTime(value?: string | null): string {
    const trimmed = (value ?? '').trim();
    if (!trimmed) {
      return '';
    }

    return trimmed.slice(0, 5);
  }

  private normalizeDayTimeFrames(day: AvailabilityDayEditor): void {
    const frames = (day.timeFrames ?? [])
      .map((frame) => ({
        startTime: (frame.startTime ?? '').trim(),
        endTime: (frame.endTime ?? '').trim(),
      }))
      .filter((frame) => !!frame.startTime || !!frame.endTime)
      .sort((a, b) => a.startTime.localeCompare(b.startTime));

    day.timeFrames = frames.length > 0 ? frames : [{ startTime: '09:00', endTime: '17:00' }];
  }

  private cloneAvailabilityDays(): AvailabilityDayEditor[] {
    return (this.availabilityDays ?? []).map((day) => ({
      dayOfWeek: day.dayOfWeek,
      label: day.label,
      enabled: !!day.enabled,
      timeFrames: (day.timeFrames ?? []).map((frame) => ({
        startTime: (frame.startTime ?? '').trim(),
        endTime: (frame.endTime ?? '').trim(),
      })),
    }));
  }

  private applyAvailabilitySnapshot(snapshot: AvailabilityDayEditor[]): void {
    const byDay = new Map<number, AvailabilityDayEditor>((snapshot ?? []).map((day) => [day.dayOfWeek, day]));

    for (const day of this.availabilityDays) {
      const match = byDay.get(day.dayOfWeek);
      if (!match) {
        continue;
      }

      day.enabled = !!match.enabled;
      day.timeFrames = (match.timeFrames ?? []).map((frame) => ({
        startTime: (frame.startTime ?? '').trim(),
        endTime: (frame.endTime ?? '').trim(),
      }));

      if (day.timeFrames.length === 0) {
        day.timeFrames = [{ startTime: '09:00', endTime: '17:00' }];
      }
    }
  }

  private validatedFrames(day: AvailabilityDayEditor): AvailabilityTimeFrameEditor[] {
    return (day.timeFrames ?? [])
      .map((frame) => ({
        startTime: (frame.startTime ?? '').trim(),
        endTime: (frame.endTime ?? '').trim(),
      }))
      .filter((frame) => !!frame.startTime || !!frame.endTime);
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

  private buildTimeOptions(): string[] {
    const options: string[] = [];
    for (let hour = 0; hour < 24; hour += 1) {
      for (let minutes = 0; minutes < 60; minutes += BusinessAvailabilityComponent.TimeStepMinutes) {
        const hourLabel = `${hour}`.padStart(2, '0');
        const minuteLabel = `${minutes}`.padStart(2, '0');
        options.push(`${hourLabel}:${minuteLabel}`);
      }
    }

    return options;
  }

  private nextTimeOption(value: string): string | null {
    const index = this.timeOptions.indexOf(value);
    if (index < 0) {
      return null;
    }

    return this.timeOptions[index + 1] ?? null;
  }

  private weeklyRanges(day: AvailabilityDayEditor | null): Array<{ start: number; end: number }> {
    if (!day?.enabled) {
      return [];
    }

    return this.validatedFrames(day)
      .filter((frame) => frame.startTime < frame.endTime)
      .map((frame) => ({
        start: this.timeToMinutes(frame.startTime),
        end: this.timeToMinutes(frame.endTime),
      }))
      .filter((range) => range.start < range.end);
  }

  private getTimeOffForDate(isoDate: string): { allDay: boolean; partial: Array<{ start: number; end: number }> } {
    const applicable = (this.daysOff ?? []).filter((range) => range.startDate <= isoDate && range.endDate >= isoDate);
    if (applicable.length === 0) {
      return { allDay: false, partial: [] };
    }

    const hasAllDay = applicable.some((range) => {
      const startDate = (range.startDate ?? '').trim();
      const endDate = (range.endDate ?? '').trim();
      if (startDate && endDate && startDate !== endDate) {
        return true;
      }

      const startTime = (range.startTime ?? '').trim() || '00:00';
      const endTime = (range.endTime ?? '').trim() || '00:00';
      return startTime === '00:00' && endTime === '00:00';
    });

    if (hasAllDay) {
      return { allDay: true, partial: [] };
    }

    const partial = applicable
      .filter((range) => (range.startDate ?? '').trim() === isoDate && (range.endDate ?? '').trim() === isoDate)
      .map((range) => {
        const start = this.timeToMinutes((range.startTime ?? '').trim() || '00:00');
        const end = this.timeToMinutes((range.endTime ?? '').trim() || '00:00');
        return { start, end };
      })
      .filter((range) => range.start < range.end)
      .sort((a, b) => a.start - b.start || a.end - b.end);

    return { allDay: false, partial: this.normalizeRanges(partial) };
  }

  private timeToMinutes(value: string): number {
    const match = /^(\d{2}):(\d{2})$/.exec((value ?? '').trim());
    if (!match) {
      return 0;
    }

    const hours = Number(match[1]);
    const minutes = Number(match[2]);
    if (!Number.isFinite(hours) || !Number.isFinite(minutes)) {
      return 0;
    }

    return Math.max(0, Math.min(1440, hours * 60 + minutes));
  }

  private minutesToTimeString(minutes: number): string {
    const clamped = Math.max(0, Math.min(1439, Math.floor(minutes)));
    const hours = Math.floor(clamped / 60);
    const mins = clamped % 60;
    return `${hours}`.padStart(2, '0') + ':' + `${mins}`.padStart(2, '0');
  }

  private formatRanges(ranges: Array<{ start: number; end: number }>): string {
    return (ranges ?? [])
      .filter((range) => range.start < range.end)
      .map((range) => `${this.timeLabel(this.minutesToTimeString(range.start))} to ${this.timeLabel(this.minutesToTimeString(range.end))}`)
      .join(', ');
  }

  private normalizeRanges(ranges: Array<{ start: number; end: number }>): Array<{ start: number; end: number }> {
    const ordered = [...(ranges ?? [])]
      .filter((range) => Number.isFinite(range.start) && Number.isFinite(range.end) && range.start < range.end)
      .sort((a, b) => a.start - b.start || a.end - b.end);

    const merged: Array<{ start: number; end: number }> = [];
    for (const range of ordered) {
      const last = merged[merged.length - 1] ?? null;
      if (!last || range.start > last.end) {
        merged.push({ start: range.start, end: range.end });
        continue;
      }

      last.end = Math.max(last.end, range.end);
    }

    return merged;
  }

  private intersectRanges(
    a: Array<{ start: number; end: number }>,
    b: Array<{ start: number; end: number }>
  ): Array<{ start: number; end: number }> {
    const left = this.normalizeRanges(a);
    const right = this.normalizeRanges(b);

    const result: Array<{ start: number; end: number }> = [];
    let i = 0;
    let j = 0;

    while (i < left.length && j < right.length) {
      const start = Math.max(left[i].start, right[j].start);
      const end = Math.min(left[i].end, right[j].end);
      if (start < end) {
        result.push({ start, end });
      }

      if (left[i].end < right[j].end) {
        i += 1;
      } else {
        j += 1;
      }
    }

    return this.normalizeRanges(result);
  }

  private subtractRanges(
    source: Array<{ start: number; end: number }>,
    subtract: Array<{ start: number; end: number }>
  ): Array<{ start: number; end: number }> {
    let result = this.normalizeRanges(source);
    const removal = this.normalizeRanges(subtract);
    for (const cut of removal) {
      const next: Array<{ start: number; end: number }> = [];
      for (const range of result) {
        if (cut.end <= range.start || cut.start >= range.end) {
          next.push(range);
          continue;
        }

        if (cut.start > range.start) {
          next.push({ start: range.start, end: Math.min(range.end, cut.start) });
        }

        if (cut.end < range.end) {
          next.push({ start: Math.max(range.start, cut.end), end: range.end });
        }
      }

      result = this.normalizeRanges(next);
    }

    return result;
  }

  private scrollOpenTimePickerToSelection(dayOfWeek: number, frameIndex: number, field: 'start' | 'end'): void {
    const popover = document.querySelector(
      `.time-popover[data-day="${dayOfWeek}"][data-frame="${frameIndex}"][data-field="${field}"]`
    ) as HTMLElement | null;

    if (!popover) {
      return;
    }

    const target =
      (popover.querySelector('.time-option-active') as HTMLElement | null) ??
      (popover.querySelector('.time-option') as HTMLElement | null);

    if (!target) {
      return;
    }

    const popoverRect = popover.getBoundingClientRect();
    const targetRect = target.getBoundingClientRect();
    const deltaTop = targetRect.top - popoverRect.top;

    popover.scrollTop = Math.max(
      0,
      popover.scrollTop + deltaTop - popover.clientHeight / 2 + targetRect.height / 2
    );
  }

  private repositionOpenTimePicker(dayOfWeek: number, frameIndex: number, field: 'start' | 'end'): void {
    const state = this.openTimePicker;
    if (!state || state.dayOfWeek !== dayOfWeek || state.frameIndex !== frameIndex || state.field !== field) {
      return;
    }

    const popover = document.querySelector(
      `.time-popover[data-day="${dayOfWeek}"][data-frame="${frameIndex}"][data-field="${field}"]`
    ) as HTMLElement | null;

    if (!popover) {
      return;
    }

    const popoverHeight = popover.offsetHeight || BusinessAvailabilityComponent.TimePopoverMaxHeight;
    const popoverWidth = popover.offsetWidth || state.width;

    const padding = BusinessAvailabilityComponent.TimePopoverPadding;
    const maxTop = Math.max(padding, window.innerHeight - popoverHeight - padding);

    const top =
      state.direction === 'down'
        ? this.clamp(state.anchorBottom + padding, padding, maxTop)
        : this.clamp(state.anchorTop - padding - popoverHeight, padding, maxTop);

    const left = this.clamp(state.anchorLeft, padding, Math.max(padding, window.innerWidth - popoverWidth - padding));

    this.openTimePicker = { ...state, top, left, width: popoverWidth };
  }

  private scrollOpenTimeOffPickerToSelection(index: number, field: 'start' | 'end'): void {
    const popover = document.querySelector(
      `.timeoff-popover[data-index="${index}"][data-field="${field}"]`
    ) as HTMLElement | null;

    if (!popover) {
      return;
    }

    const target =
      (popover.querySelector('.time-option-active') as HTMLElement | null) ??
      (popover.querySelector('.time-option') as HTMLElement | null);

    if (!target) {
      return;
    }

    const popoverRect = popover.getBoundingClientRect();
    const targetRect = target.getBoundingClientRect();
    const deltaTop = targetRect.top - popoverRect.top;

    popover.scrollTop = Math.max(
      0,
      popover.scrollTop + deltaTop - popover.clientHeight / 2 + targetRect.height / 2
    );
  }

  private repositionOpenTimeOffPicker(index: number, field: 'start' | 'end'): void {
    const state = this.openTimeOffPicker;
    if (!state || state.index !== index || state.field !== field) {
      return;
    }

    const popover = document.querySelector(
      `.timeoff-popover[data-index="${index}"][data-field="${field}"]`
    ) as HTMLElement | null;

    if (!popover) {
      return;
    }

    const popoverHeight = popover.offsetHeight || BusinessAvailabilityComponent.TimePopoverMaxHeight;
    const popoverWidth = popover.offsetWidth || state.width;

    const padding = BusinessAvailabilityComponent.TimePopoverPadding;
    const maxTop = Math.max(padding, window.innerHeight - popoverHeight - padding);

    const top =
      state.direction === 'down'
        ? this.clamp(state.anchorBottom + padding, padding, maxTop)
        : this.clamp(state.anchorTop - padding - popoverHeight, padding, maxTop);

    const left = this.clamp(state.anchorLeft, padding, Math.max(padding, window.innerWidth - popoverWidth - padding));

    this.openTimeOffPicker = { ...state, top, left, width: popoverWidth };
  }

  private computePopoverDirection(anchorTop: number, anchorBottom: number): 'up' | 'down' {
    const padding = BusinessAvailabilityComponent.TimePopoverPadding;
    const required = BusinessAvailabilityComponent.TimePopoverMaxHeight + padding * 2;
    const spaceBelow = window.innerHeight - anchorBottom;
    const spaceAbove = anchorTop;

    if (spaceBelow < required && spaceAbove > spaceBelow) {
      return 'up';
    }

    return 'down';
  }

  private computePopoverWidth(): number {
    const maxByViewport = Math.round(window.innerWidth * 0.62);
    return Math.max(160, Math.min(220, maxByViewport));
  }

  private clamp(value: number, min: number, max: number): number {
    if (!Number.isFinite(value)) {
      return min;
    }

    return Math.min(max, Math.max(min, value));
  }
}
