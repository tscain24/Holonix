import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, catchError, combineLatest, map, of, switchMap, takeUntil } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService, UserSavedLocation } from '../../../core/services/auth.service';
import {
  PublicBusinessAvailabilityTimeFrame,
  PublicBusinessDailyAvailability,
  PublicBusinessProfile,
  PublicBusinessService as PublicBusinessApiService,
  PublicBusinessSubService,
} from '../../../core/services/public-business.service';

@Component({
  selector: 'app-public-business-page',
  templateUrl: './public-business-page.component.html',
  styleUrls: ['./public-business-page.component.css'],
})
export class PublicBusinessPageComponent implements OnInit, OnDestroy {
  private static readonly closingSoonThresholdMinutes = 45;
  private static readonly cartStorageKeyPrefix = 'holonix_public_business_cart_v1:';
  private static readonly scheduleSlotMinutes = 30;
  private static readonly bookingAdvanceDays = 45;

  loading = false;
  error: string | null = null;
  business: PublicBusinessProfile | null = null;
  selectedSubServices: PublicBusinessSubService[] = [];
  activeSection: 'services' | 'hours' | 'contact' = 'services';
  isCartScreen = false;
  currentCheckoutStep: 1 | 2 | 3 = 1;
  selectedScheduleDateIso: string | null = null;
  selectedScheduleTime: string | null = null;
  savedLocations: UserSavedLocation[] = [];
  selectedSavedLocationId: number | null = null;
  loadingSavedLocations = false;
  savedLocationsError: string | null = null;
  visibleCalendarMonth = PublicBusinessPageComponent.startOfMonth(new Date());
  entrySource: 'search' | 'direct' = 'direct';
  entrySearchedCategoryName: string | null = null;
  loadingSelectedDateAvailability = false;

  private todayAvailabilityTimeFrames: PublicBusinessAvailabilityTimeFrame[] = [];
  private selectedDateAvailabilityTimeFrames: PublicBusinessAvailabilityTimeFrame[] = [];
  private hasLoadedTodayAvailability = false;
  private readonly dateAvailabilityCache = new Map<string, PublicBusinessAvailabilityTimeFrame[]>();
  private readonly loadingDateAvailability = new Set<string>();

  private readonly destroyed$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: PublicBusinessApiService,
    private auth: AuthService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.applyNavigationContext();

    combineLatest([this.route.paramMap, this.route.url])
      .pipe(
        map(([params, url]) => ({
          businessCode: (params.get('businessCode') ?? '').trim(),
          isCartScreen: url.some((segment) => (segment.path ?? '').trim().toLowerCase() === 'cart'),
        })),
        switchMap(({ businessCode, isCartScreen }) => {
          this.isCartScreen = isCartScreen;
          this.selectedSubServices = [];
          this.currentCheckoutStep = 1;
          this.selectedScheduleDateIso = null;
          this.selectedScheduleTime = null;
          this.savedLocations = [];
          this.selectedSavedLocationId = null;
          this.loadingSavedLocations = false;
          this.savedLocationsError = null;
          this.todayAvailabilityTimeFrames = [];
          this.selectedDateAvailabilityTimeFrames = [];
          this.clearDateAvailabilityCache();
          this.hasLoadedTodayAvailability = false;
          this.loadingSelectedDateAvailability = false;
          this.visibleCalendarMonth = PublicBusinessPageComponent.startOfMonth(new Date());
          if (!businessCode) {
            this.business = null;
            this.error = 'Business code is missing.';
            return of(null);
          }
          this.loading = true;
          this.error = null;
          return this.api.getProfile(businessCode).pipe(
            catchError((err) => {
              this.business = null;
              const serverErrors = err?.error?.errors;
              if (Array.isArray(serverErrors) && serverErrors.length) {
                this.error = serverErrors.filter((x: any) => typeof x === 'string').join(' ');
              } else if (typeof err?.status === 'number' && err.status) {
                this.error = `Unable to load business profile (HTTP ${err.status}).`;
              } else {
                this.error = 'Unable to load business profile.';
              }
              // Helpful for debugging local dev/proxy issues.
              // eslint-disable-next-line no-console
              console.error('Public business profile load failed', err);
              return of(null);
            })
          );
        }),
        takeUntil(this.destroyed$)
      )
      .subscribe((profile) => {
        this.business = profile;
        this.restoreSelectedSubServices(profile);
        this.loadTodayAvailability();
        this.loading = false;
      });
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }

  goBackToSearch(): void {
    const last = sessionStorage.getItem('holonix_last_search_url_v1');
    if (last && last.startsWith('/')) {
      void this.router.navigateByUrl(last);
      return;
    }
    void this.router.navigate(['/search']);
  }

  private applyNavigationContext(): void {
    const state = (history.state ?? {}) as Record<string, unknown>;
    const entryValue = state['entry'];
    const entry = typeof entryValue === 'string' ? entryValue : '';
    this.entrySource = entry === 'search' ? 'search' : 'direct';

    const searchedCategoryValue = state['searchedCategoryName'];
    const searchedCategoryName = typeof searchedCategoryValue === 'string' ? searchedCategoryValue.trim() : '';
    this.entrySearchedCategoryName = searchedCategoryName ? searchedCategoryName : null;
  }

  selectSubService(sub: PublicBusinessSubService): void {
    if (this.isSubServiceSelected(sub)) {
      return;
    }

    this.selectedSubServices = [...this.selectedSubServices, sub];
    this.persistSelectedSubServices();
    this.clearDateAvailabilityCache();
    this.loadVisibleCalendarAvailability();
    this.reloadSelectedDateAvailabilityIfNeeded();
    this.snackBar.open('Booking flow coming soon.', 'OK', { duration: 2500 });
  }

  removeSubService(sub: PublicBusinessSubService): void {
    this.selectedSubServices = this.selectedSubServices.filter((item) => item.businessSubServiceId !== sub.businessSubServiceId);
    this.persistSelectedSubServices();
    this.clearDateAvailabilityCache();
    this.loadVisibleCalendarAvailability();
    this.reloadSelectedDateAvailabilityIfNeeded();
  }

  isSubServiceSelected(sub: PublicBusinessSubService): boolean {
    return this.selectedSubServices.some((item) => item.businessSubServiceId === sub.businessSubServiceId);
  }

  setActiveSection(section: 'services' | 'hours' | 'contact'): void {
    this.activeSection = section;
  }

  openCart(): void {
    const businessCode = (this.business?.businessCode ?? '').trim();
    if (!businessCode) {
      return;
    }

    void this.router.navigate(['/', businessCode, 'cart'], {
      state: {
        entry: this.entrySource,
        searchedCategoryName: this.entrySearchedCategoryName,
      },
    });
  }

  closeCart(): void {
    const businessCode = (this.business?.businessCode ?? '').trim();
    if (!businessCode) {
      return;
    }

    void this.router.navigate(['/', businessCode], {
      state: {
        entry: this.entrySource,
        searchedCategoryName: this.entrySearchedCategoryName,
      },
    });
  }

  proceedFromCart(): void {
    if (!this.isCartScreen) {
      this.openCart();
      return;
    }

    if (this.currentCheckoutStep === 1) {
      this.currentCheckoutStep = 2;
      return;
    }

    if (this.currentCheckoutStep === 2) {
      this.currentCheckoutStep = 3;
      this.loadSavedLocationsForDetailsIfNeeded();
    }
  }

  returnToServiceStep(): void {
    this.currentCheckoutStep = 1;
  }

  showDateTimeStep(): void {
    if (this.selectedSubServices.length === 0) {
      return;
    }

    this.currentCheckoutStep = 2;
    this.loadVisibleCalendarAvailability();
  }

  showDetailsStep(): void {
    if (!this.canContinueFromDateTime) {
      return;
    }

    this.currentCheckoutStep = 3;
    this.loadSavedLocationsForDetailsIfNeeded();
  }

  previousCalendarMonth(): void {
    const candidate = new Date(this.visibleCalendarMonth.getFullYear(), this.visibleCalendarMonth.getMonth() - 1, 1);
    const currentMonth = PublicBusinessPageComponent.startOfMonth(new Date());
    if (candidate < currentMonth) {
      return;
    }

    this.visibleCalendarMonth = candidate;
    this.loadVisibleCalendarAvailability();
  }

  nextCalendarMonth(): void {
    this.visibleCalendarMonth = new Date(this.visibleCalendarMonth.getFullYear(), this.visibleCalendarMonth.getMonth() + 1, 1);
    this.loadVisibleCalendarAvailability();
  }

  selectScheduleDate(dateIso: string): void {
    if (!this.isDateSelectable(dateIso)) {
      return;
    }

    this.selectedScheduleDateIso = dateIso;
    this.selectedScheduleTime = null;
    this.selectedDateAvailabilityTimeFrames = [];
    this.loadSelectedDateAvailability(dateIso);
  }

  selectScheduleTime(time: string): void {
    this.selectedScheduleTime = time;
  }

  selectSavedLocation(location: UserSavedLocation): void {
    this.selectedSavedLocationId = location.userSavedLocationId;
  }

  showComingSoon(section: string): void {
    const label = (section ?? '').trim();
    this.snackBar.open(`${label || 'This section'} is coming soon.`, 'OK', { duration: 2200 });
  }

  get businessIconSrc(): string | null {
    const raw = (this.business?.businessIconBase64 ?? '').trim();
    if (!raw) {
      return null;
    }
    if (raw.startsWith('data:image')) {
      return raw;
    }
    return `data:image/png;base64,${raw}`;
  }

  get categoryPipeLabel(): string {
    const categories = this.business?.services ?? [];
    if (!Array.isArray(categories) || categories.length === 0) {
      return '';
    }

    const seen = new Set<string>();
    const labels: string[] = [];
    for (const entry of categories) {
      const name = (entry?.name ?? '').trim();
      if (!name) {
        continue;
      }

      const key = name.toLowerCase();
      if (seen.has(key)) {
        continue;
      }

      seen.add(key);
      labels.push(name);
    }

    return labels.join(' | ');
  }

  formatDuration(minutes: number): string {
    const safe = Number.isFinite(minutes) ? Math.max(0, Math.trunc(minutes)) : 0;
    if (safe === 0) {
      return '';
    }
    if (safe < 60) {
      return `${safe} min`;
    }
    const hours = Math.floor(safe / 60);
    const rem = safe % 60;
    if (rem === 0) {
      return `${hours} hr`;
    }
    return `${hours} hr ${rem} min`;
  }

  formatPrice(value: number): string {
    const safe = Number.isFinite(value) ? value : 0;
    return `$${safe.toFixed(0)}`;
  }

  formatPriceWithCents(value: number): string {
    const safe = Number.isFinite(value) ? value : 0;
    return `$${safe.toFixed(2)}`;
  }

  get cartDurationMinutes(): number {
    return this.selectedSubServices.reduce(
      (total, sub) => total + (Number.isFinite(sub.durationMinutes) ? Math.max(0, Math.trunc(sub.durationMinutes)) : 0),
      0
    );
  }

  get cartTotalPrice(): number {
    return this.selectedSubServices.reduce((total, sub) => total + (Number.isFinite(sub.price) ? sub.price : 0), 0);
  }

  get bookingServiceFee(): number {
    return this.cartTotalPrice * 0.05;
  }

  get bookingTotalPrice(): number {
    return this.cartTotalPrice + this.bookingServiceFee;
  }

  get cartItemCount(): number {
    return this.selectedSubServices.length;
  }

  get cartHasConsultation(): boolean {
    return this.selectedSubServices.some((sub) => sub.consultationNeeded);
  }

  get canSchedule(): boolean {
    return this.selectedSubServices.length > 0;
  }

  get canContinueFromDateTime(): boolean {
    return !!this.selectedScheduleDateIso && !!this.selectedScheduleTime;
  }

  get selectedServicesRequireLocation(): boolean {
    return this.selectedSubServices.some((sub) => sub.requiresServiceLocation);
  }

  get selectedSavedLocation(): UserSavedLocation | null {
    if (this.selectedSavedLocationId === null) {
      return null;
    }

    return this.savedLocations.find((location) => location.userSavedLocationId === this.selectedSavedLocationId) ?? null;
  }

  get canContinueFromDetails(): boolean {
    return !this.selectedServicesRequireLocation || this.selectedSavedLocationId !== null;
  }

  get locationLabel(): string {
    const city = (this.business?.city ?? '').trim();
    const state = (this.business?.state ?? '').trim();
    if (city && state) {
      return `${city}, ${state}`;
    }
    return city || state || '';
  }

  get fullAddress(): string {
    const parts = [
      (this.business?.address1 ?? '').trim(),
      (this.business?.address2 ?? '').trim(),
      this.locationLabel,
      (this.business?.zipCode ?? '').trim(),
    ].filter(Boolean);
    return parts.join(', ');
  }

  get phoneHref(): string {
    const raw = (this.business?.businessPhoneNumber ?? '').trim();
    if (!raw) {
      return '';
    }

    const digits = raw.replace(/[^\d+]/g, '');
    const cleaned = digits.startsWith('+') ? `+${digits.slice(1).replace(/\D/g, '')}` : digits.replace(/\D/g, '');
    if (!cleaned) {
      return '';
    }

    const normalized = cleaned.length === 11 && cleaned.startsWith('1') ? `+${cleaned}` : cleaned;
    return `tel:${normalized}`;
  }

  get emailHref(): string {
    const email = (this.business?.businessEmail ?? '').trim();
    if (!email) {
      return '';
    }

    const subject = encodeURIComponent(`Inquiry for ${(this.business?.name ?? '').trim() || 'your business'}`);
    return `mailto:${email}?subject=${subject}`;
  }

  get addressHref(): string {
    const address = this.fullAddress.trim();
    if (!address) {
      return '';
    }

    return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(address)}`;
  }

  get dayLabels(): string[] {
    return ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
  }

  private get employeeAvailabilityDayOrder(): number[] {
    return [1, 2, 3, 4, 5, 6, 0];
  }

  get calendarMonthLabel(): string {
    return new Intl.DateTimeFormat('en-US', { month: 'long', year: 'numeric' }).format(this.visibleCalendarMonth);
  }

  get canViewPreviousCalendarMonth(): boolean {
    return this.visibleCalendarMonth > PublicBusinessPageComponent.startOfMonth(new Date());
  }

  get canViewNextCalendarMonth(): boolean {
    return this.visibleCalendarMonth < PublicBusinessPageComponent.startOfMonth(this.maxBookableDate);
  }

  get calendarWeeks(): {
    iso: string;
    dayNumber: number;
    inCurrentMonth: boolean;
    isSelectable: boolean;
    isSelected: boolean;
    isToday: boolean;
  }[][] {
    const monthStart = PublicBusinessPageComponent.startOfMonth(this.visibleCalendarMonth);
    const gridStart = new Date(monthStart);
    gridStart.setDate(monthStart.getDate() - monthStart.getDay());

    const weeks: {
      iso: string;
      dayNumber: number;
      inCurrentMonth: boolean;
      isSelectable: boolean;
      isSelected: boolean;
      isToday: boolean;
    }[][] = [];

    for (let week = 0; week < 6; week++) {
      const row = [];
      for (let day = 0; day < 7; day++) {
        const cellDate = new Date(gridStart);
        cellDate.setDate(gridStart.getDate() + week * 7 + day);
        const iso = PublicBusinessPageComponent.toDateIso(cellDate);
        row.push({
          iso,
          dayNumber: cellDate.getDate(),
          inCurrentMonth: cellDate.getMonth() === monthStart.getMonth(),
          isSelectable: this.isDateSelectable(iso),
          isSelected: this.selectedScheduleDateIso === iso,
          isToday: iso === PublicBusinessPageComponent.toDateIso(new Date()),
        });
      }
      weeks.push(row);
    }

    return weeks;
  }

  get availableTimeSlots(): string[] {
    if (!this.selectedScheduleDateIso || this.loadingSelectedDateAvailability) {
      return [];
    }

    const slots: string[] = [];
    return this.buildTimeSlotsForDate(this.selectedScheduleDateIso, this.selectedDateAvailabilityTimeFrames);
  }

  private buildTimeSlotsForDate(dateIso: string, frames: PublicBusinessAvailabilityTimeFrame[]): string[] {
    const slots: string[] = [];
    const now = new Date();
    const todayIso = PublicBusinessPageComponent.toDateIso(now);
    const currentMinutes = now.getHours() * 60 + now.getMinutes();
    const isSelectedDateToday = dateIso === todayIso;
    const requiredMinutes = Math.max(PublicBusinessPageComponent.scheduleSlotMinutes, this.cartDurationMinutes);

    for (const frame of frames) {
      const openMinutes = this.parseTimeToMinutes(frame.startTime);
      const closeMinutes = this.parseTimeToMinutes(frame.endTime);
      if (openMinutes === null || closeMinutes === null) {
        continue;
      }

      let endMinutes = closeMinutes;
      if (closeMinutes <= openMinutes) {
        endMinutes += 1440;
      }

      for (let minutes = openMinutes; minutes + requiredMinutes <= endMinutes; minutes += PublicBusinessPageComponent.scheduleSlotMinutes) {
        const normalizedMinutes = minutes % 1440;
        if (isSelectedDateToday && normalizedMinutes <= currentMinutes) {
          continue;
        }

        slots.push(this.formatMinutesAsTime(normalizedMinutes));
      }
    }

    return slots;
  }

  get selectedScheduleDateLabel(): string {
    if (!this.selectedScheduleDateIso) {
      return 'Choose a date';
    }

    const selectedDate = new Date(`${this.selectedScheduleDateIso}T00:00:00`);
    if (Number.isNaN(selectedDate.getTime())) {
      return 'Choose a date';
    }

    return new Intl.DateTimeFormat('en-US', { weekday: 'short', month: 'short', day: 'numeric' }).format(selectedDate);
  }

  get selectedScheduleTimeRangeLabel(): string {
    if (!this.selectedScheduleTime) {
      return '';
    }

    const startMinutes = this.parseDisplayTimeToMinutes(this.selectedScheduleTime);
    if (startMinutes === null || this.cartDurationMinutes <= 0) {
      return this.selectedScheduleTime;
    }

    const endMinutes = startMinutes + this.cartDurationMinutes;
    return `${this.selectedScheduleTime} - ${this.formatMinutesAsTime(endMinutes % 1440)}`;
  }

  formatSavedLocationAddress(location: UserSavedLocation): string {
    return [location.address1, location.address2, `${location.city}, ${location.state} ${location.zipCode}`]
      .filter((value) => !!value && value.trim().length > 0)
      .join(', ');
  }

  get currentBusinessStatusLabel(): string {
    const currentFrames = this.getCurrentAvailabilityTimeFrames();
    const now = new Date();
    const currentMinutes = now.getHours() * 60 + now.getMinutes();
    for (const frame of currentFrames) {
      const openMinutes = this.parseTimeToMinutes(frame.startTime);
      const closeMinutes = this.parseTimeToMinutes(frame.endTime);
      if (openMinutes === null || closeMinutes === null) {
        continue;
      }

      const { adjustedCurrentMinutes, adjustedCloseMinutes } = this.normalizeBusinessDayRange(
        currentMinutes,
        openMinutes,
        closeMinutes
      );

      if (adjustedCurrentMinutes < openMinutes || adjustedCurrentMinutes >= adjustedCloseMinutes) {
        continue;
      }

      if (adjustedCloseMinutes - adjustedCurrentMinutes <= PublicBusinessPageComponent.closingSoonThresholdMinutes) {
        return 'Closing soon';
      }

      return 'Open';
    }

    return 'Closed';
  }

  get currentBusinessStatusClass(): string {
    switch (this.currentBusinessStatusLabel) {
      case 'Closed':
        return 'open-now--closed';
      case 'Closing soon':
        return 'open-now--closing-soon';
      default:
        return 'open-now--open';
    }
  }

  get businessHoursDisplay(): { dayLabel: string; value: string }[] {
    const labels = this.dayLabels;
    const byDay = this.getBusinessHoursByDay();

    const result: { dayLabel: string; value: string }[] = [];
    for (let day = 0; day < 7; day++) {
      const label = labels[day] ?? 'Day';
      const entry = byDay.get(day);
      if (!entry) {
        result.push({ dayLabel: label, value: 'Hours not set' });
        continue;
      }

      if (entry.isClosed) {
        result.push({ dayLabel: label, value: 'Closed' });
        continue;
      }

      const open = this.formatTime(entry.openTime);
      const close = this.formatTime(entry.closeTime);
      if (!open || !close) {
        result.push({ dayLabel: label, value: 'Hours not set' });
      } else {
        result.push({ dayLabel: label, value: `${open} – ${close}` });
      }
    }

    return result;
  }

  private formatTime(value: string | null): string {
    const trimmed = (value ?? '').trim();
    if (!trimmed) {
      return '';
    }

    const match = /^(\d{2}):(\d{2})$/.exec(trimmed);
    if (!match) {
      return '';
    }

    const hours = Number(match[1]);
    const minutes = Number(match[2]);
    if (!Number.isFinite(hours) || !Number.isFinite(minutes)) {
      return '';
    }

    const date = new Date(2000, 0, 1, hours, minutes, 0);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    return new Intl.DateTimeFormat('en-US', { hour: 'numeric', minute: '2-digit' }).format(date);
  }

  private getCurrentAvailabilityTimeFrames(): PublicBusinessAvailabilityTimeFrame[] {
    if (this.hasLoadedTodayAvailability) {
      return this.intersectWithBusinessHours(new Date(), this.todayAvailabilityTimeFrames);
    }

    const todayDate = new Date();
    const today = this.getNativeDayIndex(todayDate);
    const weeklyAvailability = this.getWeeklyAvailabilityByDay().get(today) ?? [];
    return this.intersectWithBusinessHours(todayDate, weeklyAvailability);
  }

  private formatMinutesAsTime(totalMinutes: number): string {
    const clamped = ((totalMinutes % 1440) + 1440) % 1440;
    const hours = Math.floor(clamped / 60);
    const minutes = clamped % 60;
    const date = new Date(2000, 0, 1, hours, minutes, 0);
    return new Intl.DateTimeFormat('en-US', { hour: 'numeric', minute: '2-digit' }).format(date);
  }

  private getCurrentBusinessHours(): { openTime: string | null; closeTime: string | null; isClosed: boolean } | null {
    const today = this.getBusinessHoursDayIndex(new Date());
    return this.getBusinessHoursByDay().get(today) ?? null;
  }

  private getWeeklyAvailabilityByDay(): Map<number, PublicBusinessAvailabilityTimeFrame[]> {
    const incoming = this.business?.availability ?? null;
    const byDay = new Map<number, PublicBusinessAvailabilityTimeFrame[]>();

    for (const entry of incoming ?? []) {
      const day = Number(entry?.dayOfWeek);
      if (!Number.isFinite(day) || day < 0 || day > 6) {
        continue;
      }

      const frames = (entry?.timeFrames ?? [])
        .map((frame) => ({
          startTime: (frame?.startTime ?? '').trim(),
          endTime: (frame?.endTime ?? '').trim(),
        }))
        .filter((frame) => !!frame.startTime && !!frame.endTime);

      byDay.set(Math.trunc(day), frames);
    }

    return byDay;
  }

  private getBusinessHoursByDay(): Map<number, { openTime: string | null; closeTime: string | null; isClosed: boolean }> {
    const incoming = this.business?.businessHours ?? null;
    const byDay = new Map<number, { openTime: string | null; closeTime: string | null; isClosed: boolean }>();

    for (const entry of incoming ?? []) {
      const day = Number(entry?.dayOfWeek);
      if (!Number.isFinite(day) || day < 0 || day > 6) {
        continue;
      }

      byDay.set(Math.trunc(day), {
        openTime: (entry.openTime ?? '').trim() || null,
        closeTime: (entry.closeTime ?? '').trim() || null,
        isClosed: !!entry.isClosed,
      });
    }

    return byDay;
  }

  private isDateSelectable(dateIso: string): boolean {
    if (!this.isDatePotentiallySelectable(dateIso)) {
      return false;
    }

    const cachedFrames = this.dateAvailabilityCache.get(dateIso);
    if (cachedFrames) {
      return this.buildTimeSlotsForDate(dateIso, cachedFrames).length > 0;
    }

    return true;
  }

  private isDatePotentiallySelectable(dateIso: string): boolean {
    const date = new Date(`${dateIso}T00:00:00`);
    if (Number.isNaN(date.getTime())) {
      return false;
    }

    const today = PublicBusinessPageComponent.startOfDay(new Date());
    if (date < today) {
      return false;
    }

    if (date > this.maxBookableDate) {
      return false;
    }

    const weeklyAvailabilityByDay = this.getWeeklyAvailabilityByDay();
    const weeklyAvailabilityFrames = weeklyAvailabilityByDay.get(this.getNativeDayIndex(date)) ?? [];
    if (weeklyAvailabilityByDay.size > 0) {
      const boundedFrames = this.intersectWithBusinessHours(date, weeklyAvailabilityFrames);
      return boundedFrames.some((frame) =>
        this.parseTimeToMinutes(frame.startTime) !== null && this.parseTimeToMinutes(frame.endTime) !== null
      );
    }

    const dayEntry = this.getBusinessHoursByDay().get(this.getBusinessHoursDayIndex(date));
    if (!dayEntry || dayEntry.isClosed) {
      return false;
    }

    return this.parseTimeToMinutes(dayEntry.openTime) !== null && this.parseTimeToMinutes(dayEntry.closeTime) !== null;
  }

  private parseTimeToMinutes(value: string | null): number | null {
    const trimmed = (value ?? '').trim();
    const match = /^(\d{2}):(\d{2})$/.exec(trimmed);
    if (!match) {
      return null;
    }

    const hours = Number(match[1]);
    const minutes = Number(match[2]);
    if (!Number.isFinite(hours) || !Number.isFinite(minutes)) {
      return null;
    }

    return hours * 60 + minutes;
  }

  private parseDisplayTimeToMinutes(value: string | null): number | null {
    const trimmed = (value ?? '').trim();
    const match = /^(\d{1,2}):(\d{2})\s*(AM|PM)$/i.exec(trimmed);
    if (!match) {
      return this.parseTimeToMinutes(trimmed);
    }

    let hours = Number(match[1]);
    const minutes = Number(match[2]);
    const meridiem = match[3].toUpperCase();
    if (!Number.isFinite(hours) || !Number.isFinite(minutes) || hours < 1 || hours > 12 || minutes < 0 || minutes > 59) {
      return null;
    }

    if (meridiem === 'AM') {
      hours = hours === 12 ? 0 : hours;
    } else {
      hours = hours === 12 ? 12 : hours + 12;
    }

    return hours * 60 + minutes;
  }

  private getBusinessHoursDayIndex(date: Date): number {
    return (date.getDay() + 6) % 7;
  }

  private getNativeDayIndex(date: Date): number {
    return date.getDay();
  }

  private loadTodayAvailability(): void {
    const businessCode = (this.business?.businessCode ?? '').trim();
    if (!businessCode) {
      this.todayAvailabilityTimeFrames = [];
      this.hasLoadedTodayAvailability = false;
      return;
    }

    this.loadDailyAvailability(
      businessCode,
      PublicBusinessPageComponent.toDateIso(new Date()),
      undefined,
      (response) => {
        this.todayAvailabilityTimeFrames = this.normalizeAvailabilityTimeFrames(response);
        this.hasLoadedTodayAvailability = true;
      },
      () => {
        this.todayAvailabilityTimeFrames = [];
        this.hasLoadedTodayAvailability = true;
      }
    );
  }

  private loadSelectedDateAvailability(dateIso: string): void {
    const businessCode = (this.business?.businessCode ?? '').trim();
    if (!businessCode || !dateIso) {
      this.selectedDateAvailabilityTimeFrames = this.getWeeklyAvailabilityFramesForDateIso(dateIso);
      return;
    }

    const cachedFrames = this.dateAvailabilityCache.get(dateIso);
    if (cachedFrames) {
      this.selectedDateAvailabilityTimeFrames = cachedFrames;
      this.loadingSelectedDateAvailability = false;
      return;
    }

    this.loadingSelectedDateAvailability = true;
    this.loadDailyAvailability(
      businessCode,
      dateIso,
      this.selectedSubServiceIdsForAvailability,
      (response) => {
        if (this.selectedScheduleDateIso !== dateIso) {
          return;
        }

        const dailyFrames = this.normalizeAvailabilityTimeFrames(response);
        this.selectedDateAvailabilityTimeFrames = this.intersectWithBusinessHoursByDateIso(dateIso, dailyFrames);
        this.dateAvailabilityCache.set(dateIso, this.selectedDateAvailabilityTimeFrames);
        this.loadingSelectedDateAvailability = false;
      },
      () => {
        if (this.selectedScheduleDateIso === dateIso) {
          this.selectedDateAvailabilityTimeFrames = this.getWeeklyAvailabilityFramesForDateIso(dateIso);
          this.dateAvailabilityCache.set(dateIso, this.selectedDateAvailabilityTimeFrames);
          this.loadingSelectedDateAvailability = false;
        }
      }
    );
  }

  private loadVisibleCalendarAvailability(): void {
    const businessCode = (this.business?.businessCode ?? '').trim();
    if (!businessCode || this.currentCheckoutStep !== 2) {
      return;
    }

    for (const dateIso of this.visibleCalendarDateIsos) {
      if (!this.isDatePotentiallySelectable(dateIso)
        || this.dateAvailabilityCache.has(dateIso)
        || this.loadingDateAvailability.has(dateIso)) {
        continue;
      }

      this.loadingDateAvailability.add(dateIso);
      this.loadDailyAvailability(
        businessCode,
        dateIso,
        this.selectedSubServiceIdsForAvailability,
        (response) => {
          const dailyFrames = this.normalizeAvailabilityTimeFrames(response);
          const boundedFrames = this.intersectWithBusinessHoursByDateIso(dateIso, dailyFrames);
          this.dateAvailabilityCache.set(dateIso, boundedFrames);
          this.loadingDateAvailability.delete(dateIso);
        },
        () => {
          const fallbackFrames = this.getWeeklyAvailabilityFramesForDateIso(dateIso);
          this.dateAvailabilityCache.set(dateIso, fallbackFrames);
          this.loadingDateAvailability.delete(dateIso);
        }
      );
    }
  }

  private loadDailyAvailability(
    businessCode: string,
    dateIso: string,
    businessSubServiceIds: number[] | undefined,
    onSuccess: (response: PublicBusinessDailyAvailability) => void,
    onError: () => void
  ): void {
    this.api.getDailyAvailability(businessCode, dateIso, businessSubServiceIds)
      .pipe(takeUntil(this.destroyed$))
      .subscribe({
        next: onSuccess,
        error: () => onError(),
      });
  }

  private loadSavedLocationsForDetailsIfNeeded(): void {
    if (!this.selectedServicesRequireLocation) {
      this.savedLocations = [];
      this.selectedSavedLocationId = null;
      this.savedLocationsError = null;
      return;
    }

    if (this.savedLocations.length > 0 || this.loadingSavedLocations) {
      return;
    }

    this.loadingSavedLocations = true;
    this.savedLocationsError = null;
    this.auth.getProfile()
      .pipe(takeUntil(this.destroyed$))
      .subscribe({
        next: (profile) => {
          this.loadingSavedLocations = false;
          this.savedLocations = this.normalizeSavedLocations(profile.locations ?? []);
          const primary = this.savedLocations.find((location) => location.isPrimary);
          this.selectedSavedLocationId = (primary ?? this.savedLocations[0] ?? null)?.userSavedLocationId ?? null;
        },
        error: (err) => {
          this.loadingSavedLocations = false;
          const errors = err?.error?.errors as string[] | undefined;
          this.savedLocationsError = errors?.[0] ?? 'Sign in to use saved profile locations for this booking.';
          this.savedLocations = [];
          this.selectedSavedLocationId = null;
        },
      });
  }

  private normalizeAvailabilityTimeFrames(response: PublicBusinessDailyAvailability | null): PublicBusinessAvailabilityTimeFrame[] {
    return (response?.timeFrames ?? [])
      .map((frame) => ({
        startTime: (frame?.startTime ?? '').trim(),
        endTime: (frame?.endTime ?? '').trim(),
      }))
      .filter((frame) => !!frame.startTime && !!frame.endTime);
  }

  private getWeeklyAvailabilityFramesForDateIso(dateIso: string): PublicBusinessAvailabilityTimeFrame[] {
    const date = new Date(`${dateIso}T00:00:00`);
    if (Number.isNaN(date.getTime())) {
      return [];
    }

    const weeklyFrames = this.getWeeklyAvailabilityByDay().get(this.getNativeDayIndex(date)) ?? [];
    return this.intersectWithBusinessHours(date, weeklyFrames);
  }

  private getBusinessHoursFrames(date: Date): PublicBusinessAvailabilityTimeFrame[] {
    const dayEntry = this.getBusinessHoursByDay().get(this.getBusinessHoursDayIndex(date));
    if (!dayEntry || dayEntry.isClosed || !dayEntry.openTime || !dayEntry.closeTime) {
      return [];
    }

    return [{ startTime: dayEntry.openTime, endTime: dayEntry.closeTime }];
  }

  private intersectWithBusinessHoursByDateIso(
    dateIso: string,
    frames: PublicBusinessAvailabilityTimeFrame[]
  ): PublicBusinessAvailabilityTimeFrame[] {
    const date = new Date(`${dateIso}T00:00:00`);
    if (Number.isNaN(date.getTime())) {
      return [];
    }

    return this.intersectWithBusinessHours(date, frames);
  }

  private intersectWithBusinessHours(
    date: Date,
    frames: PublicBusinessAvailabilityTimeFrame[]
  ): PublicBusinessAvailabilityTimeFrame[] {
    if (frames.length === 0) {
      return [];
    }

    const businessHoursByDay = this.getBusinessHoursByDay();
    if (!this.hasConfiguredBusinessHours(businessHoursByDay)) {
      return frames;
    }

    const businessDayIndex = this.getBusinessHoursDayIndex(date);
    const businessDayEntry = businessHoursByDay.get(businessDayIndex);
    if (!businessDayEntry) {
      return frames;
    }

    if (businessDayEntry.isClosed) {
      return [];
    }

    const businessFrames = this.getBusinessHoursFrames(date);
    if (businessFrames.length === 0) {
      return frames;
    }

    const intersections: PublicBusinessAvailabilityTimeFrame[] = [];
    for (const frame of frames) {
      const frameStart = this.parseTimeToMinutes(frame.startTime);
      const frameEnd = this.parseTimeToMinutes(frame.endTime);
      if (frameStart === null || frameEnd === null) {
        continue;
      }

      for (const businessFrame of businessFrames) {
        const businessStart = this.parseTimeToMinutes(businessFrame.startTime);
        const businessEnd = this.parseTimeToMinutes(businessFrame.endTime);
        if (businessStart === null || businessEnd === null) {
          continue;
        }

        const start = Math.max(frameStart, businessStart);
        const end = Math.min(frameEnd, businessEnd);
        if (start >= end) {
          continue;
        }

        intersections.push({
          startTime: this.formatMinutesAsTwentyFourHourTime(start),
          endTime: this.formatMinutesAsTwentyFourHourTime(end),
        });
      }
    }

    return intersections;
  }

  private hasConfiguredBusinessHours(
    byDay: Map<number, { openTime: string | null; closeTime: string | null; isClosed: boolean }>
  ): boolean {
    for (const entry of byDay.values()) {
      if (entry.isClosed) {
        continue;
      }

      if (this.parseTimeToMinutes(entry.openTime) !== null && this.parseTimeToMinutes(entry.closeTime) !== null) {
        return true;
      }
    }

    return false;
  }

  private formatMinutesAsTwentyFourHourTime(totalMinutes: number): string {
    const clamped = ((totalMinutes % 1440) + 1440) % 1440;
    const hours = `${Math.floor(clamped / 60)}`.padStart(2, '0');
    const minutes = `${clamped % 60}`.padStart(2, '0');
    return `${hours}:${minutes}`;
  }

  private normalizeBusinessDayRange(
    currentMinutes: number,
    openMinutes: number,
    closeMinutes: number
  ): { adjustedCurrentMinutes: number; adjustedCloseMinutes: number } {
    if (closeMinutes > openMinutes) {
      return { adjustedCurrentMinutes: currentMinutes, adjustedCloseMinutes: closeMinutes };
    }

    const adjustedCurrentMinutes = currentMinutes < openMinutes ? currentMinutes + 1440 : currentMinutes;
    return { adjustedCurrentMinutes, adjustedCloseMinutes: closeMinutes + 1440 };
  }

  private persistSelectedSubServices(): void {
    const businessCode = (this.business?.businessCode ?? '').trim();
    if (!businessCode) {
      return;
    }

    sessionStorage.setItem(
      this.getCartStorageKey(businessCode),
      JSON.stringify(this.selectedSubServices.map((sub) => sub.businessSubServiceId))
    );
  }

  private restoreSelectedSubServices(profile: PublicBusinessProfile | null): void {
    const businessCode = (profile?.businessCode ?? '').trim();
    if (!businessCode || !profile) {
      this.selectedSubServices = [];
      return;
    }

    const raw = sessionStorage.getItem(this.getCartStorageKey(businessCode));
    if (!raw) {
      this.selectedSubServices = [];
      return;
    }

    try {
      const ids = JSON.parse(raw);
      if (!Array.isArray(ids)) {
        this.selectedSubServices = [];
        return;
      }

      const allSubServices = profile.services.flatMap((service) => service.subServices ?? []);
      const byId = new Map(allSubServices.map((sub) => [sub.businessSubServiceId, sub]));
      this.selectedSubServices = ids
        .map((id) => byId.get(Number(id)))
        .filter((sub): sub is PublicBusinessSubService => !!sub);
    } catch {
      this.selectedSubServices = [];
    }
  }

  private getCartStorageKey(businessCode: string): string {
    return `${PublicBusinessPageComponent.cartStorageKeyPrefix}${businessCode}`;
  }

  private get selectedSubServiceIdsForAvailability(): number[] {
    return this.selectedSubServices
      .map((sub) => Number(sub.businessSubServiceId))
      .filter((id) => Number.isFinite(id) && id > 0);
  }

  private get visibleCalendarDateIsos(): string[] {
    const monthStart = PublicBusinessPageComponent.startOfMonth(this.visibleCalendarMonth);
    const gridStart = new Date(monthStart);
    gridStart.setDate(monthStart.getDate() - monthStart.getDay());

    const dates: string[] = [];
    for (let offset = 0; offset < 42; offset++) {
      const cellDate = new Date(gridStart);
      cellDate.setDate(gridStart.getDate() + offset);
      dates.push(PublicBusinessPageComponent.toDateIso(cellDate));
    }

    return dates;
  }

  private reloadSelectedDateAvailabilityIfNeeded(): void {
    if (!this.selectedScheduleDateIso) {
      return;
    }

    this.selectedScheduleTime = null;
    this.selectedDateAvailabilityTimeFrames = [];
    this.loadSelectedDateAvailability(this.selectedScheduleDateIso);
  }

  private clearDateAvailabilityCache(): void {
    this.dateAvailabilityCache.clear();
    this.loadingDateAvailability.clear();
  }

  private normalizeSavedLocations(locations: UserSavedLocation[]): UserSavedLocation[] {
    return [...locations].sort((a, b) => {
      if (a.isPrimary !== b.isPrimary) {
        return a.isPrimary ? -1 : 1;
      }

      return a.label.localeCompare(b.label);
    });
  }

  private get maxBookableDate(): Date {
    const today = PublicBusinessPageComponent.startOfDay(new Date());
    const maxDate = new Date(today);
    maxDate.setDate(today.getDate() + PublicBusinessPageComponent.bookingAdvanceDays);
    return maxDate;
  }

  private static startOfMonth(value: Date): Date {
    return new Date(value.getFullYear(), value.getMonth(), 1);
  }

  private static startOfDay(value: Date): Date {
    return new Date(value.getFullYear(), value.getMonth(), value.getDate());
  }

  private static toDateIso(value: Date): string {
    const year = value.getFullYear();
    const month = `${value.getMonth() + 1}`.padStart(2, '0');
    const day = `${value.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  get websiteHref(): string {
    const email = (this.business?.businessEmail ?? '').trim();
    if (!email) {
      return '';
    }

    const atIndex = email.lastIndexOf('@');
    if (atIndex <= 0 || atIndex === email.length - 1) {
      return '';
    }

    const domain = email.slice(atIndex + 1).trim().toLowerCase();
    if (!domain || !domain.includes('.')) {
      return '';
    }

    const blocked = new Set([
      'gmail.com',
      'yahoo.com',
      'outlook.com',
      'hotmail.com',
      'aol.com',
      'icloud.com',
      'proton.me',
      'protonmail.com',
    ]);

    if (blocked.has(domain)) {
      return '';
    }

    return `https://${domain}`;
  }
}
