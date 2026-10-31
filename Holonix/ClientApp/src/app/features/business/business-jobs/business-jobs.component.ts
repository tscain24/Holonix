import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, Subscription, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { JobEmployeeOption, JobOptions, JobsFilters, JobsPage, JobsService, OperationalJob } from '../../../core/services/jobs.service';

@Component({ selector: 'app-business-jobs', templateUrl: './business-jobs.component.html', styleUrls: ['./business-jobs.component.css'] })
export class BusinessJobsComponent implements OnInit, OnDestroy {
  code = '';
  data: JobsPage | null = null;
  options: JobOptions = { employees: [], services: [], customers: [] };
  loading = false;
  mutating = false;
  error = '';
  optionsError = '';
  modalError = '';
  showFilters = false;
  page = 1;
  filters: JobsFilters = { search: '', filter: 'all', start: '', end: '', employeeId: null };
  applied = { ...this.filters };
  statuses = [{ key: 'all', name: 'All' }, { key: 'upcoming', name: 'Upcoming' }, { key: 'inprogress', name: 'In progress' }, { key: 'unassigned', name: 'Unassigned' }, { key: 'completed', name: 'Completed' }, { key: 'cancelled', name: 'Cancelled' }, { key: 'overdue', name: 'Overdue' }];
  detail: OperationalJob | null = null;
  editingTeam: OperationalJob | null = null;
  scheduling: OperationalJob | null = null;
  confirmation: { job: OperationalJob; action: 'complete' | 'cancel' } | null = null;
  creating = false;
  selectedIds: number[] = [];
  teamSearch = '';
  teamOptions: JobEmployeeOption[] = [];
  teamLoading = false;
  teamLoaded = false;
  scheduleStart = '';
  scheduleEnd = '';
  scheduleDate: Date | null = null;
  readonly scheduleToday = new Date(new Date().getFullYear(), new Date().getMonth(), new Date().getDate());
  scheduleFrom = '';
  scheduleUntil = '';
  scheduleEndDay = 0;
  readonly scheduleEndDays = Array.from({ length: 8 }, (_, day) => day);
  readonly scheduleTimes = Array.from({ length: 96 }, (_, index) => {
    const hour = Math.floor(index / 4), minute = index % 4 * 15;
    const value = String(hour).padStart(2, '0') + ':' + String(minute).padStart(2, '0');
    return { value, label: new Date(2000, 0, 1, hour, minute).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' }) };
  });
  clockLabel(value: string): string {
    const [hour, minute] = value.split(':').map(Number);
    return new Date(2000, 0, 1, hour, minute).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
  }
  get customStartTime(): boolean { return !!this.scheduleFrom && !this.scheduleTimes.some(t => t.value === this.scheduleFrom); }
  get customEndTime(): boolean { return !!this.scheduleUntil && !this.scheduleTimes.some(t => t.value === this.scheduleUntil); }
  private scheduleDateKey(day: Date): string { return String(day.getFullYear()).padStart(4, '0') + '-' + String(day.getMonth() + 1).padStart(2, '0') + '-' + String(day.getDate()).padStart(2, '0'); }
  chooseScheduleDate(day: Date | null): void { if (this.mutating) return; this.scheduleDate = day; this.updateSchedule(); }
  scheduleShortcut(days: number): void { const day = new Date(this.scheduleToday); day.setDate(day.getDate() + days); this.chooseScheduleDate(day); }
  updateSchedule(): void {
    this.modalError = '';
    if (!this.scheduleDate) { this.scheduleStart = ''; this.scheduleEnd = ''; return; }
    const end = new Date(this.scheduleDate); end.setDate(end.getDate() + Number(this.scheduleEndDay));
    this.scheduleStart = this.scheduleDateKey(this.scheduleDate) + 'T' + this.scheduleFrom;
    this.scheduleEnd = this.scheduleDateKey(end) + 'T' + this.scheduleUntil;
  }
  changeScheduleStart(value: string): void {
    const minutes = this.scheduleDuration;
    this.scheduleFrom = value;
    if (minutes > 0) {
      const [hour, minute] = value.split(':').map(Number);
      const total = hour * 60 + minute + minutes;
      this.scheduleEndDay = Math.floor(total / 1440);
      this.scheduleUntil = String(Math.floor(total % 1440 / 60)).padStart(2, '0') + ':' + String(total % 60).padStart(2, '0');
    }
    this.updateSchedule();
  }
  get scheduleDuration(): number {
    const [startHour, startMinute] = this.scheduleFrom.split(':').map(Number);
    const [endHour, endMinute] = this.scheduleUntil.split(':').map(Number);
    return Number(this.scheduleEndDay) * 1440 + endHour * 60 + endMinute - startHour * 60 - startMinute;
  }
  get scheduleDurationLabel(): string {
    const minutes = this.scheduleDuration;
    if (!Number.isFinite(minutes) || minutes <= 0) return 'Choose a valid time window';
    const hours = Math.floor(minutes / 60), remainder = minutes % 60;
    return [hours ? hours + ' hr' : '', remainder ? remainder + ' min' : ''].filter(Boolean).join(' ');
  }
  get scheduleProblem(): string {
    if (!this.scheduleDate) return 'Choose a new date to continue.';
    if (this.scheduleDate < this.scheduleToday) return 'Choose today or a future date.';
    if (!this.scheduleFrom || !this.scheduleUntil || !Number.isFinite(this.scheduleDuration) || this.scheduleDuration <= 0) return 'End time must be after the start time. Choose a later end day for overnight work.';
    if (this.scheduleDuration > 7 * 1440) return 'A job can span up to seven days.';
    return '';
  }
  get scheduleChanged(): boolean { return !!this.scheduling && (this.scheduleStart !== this.scheduling.startDateTime.slice(0,16) || this.scheduleEnd !== this.scheduling.endDateTime.slice(0,16)); }

  createName = '';
  createCustomer: string | null = null;
  createLocation = { address1: '', address2: '', city: '', state: '', zipCode: '' };
  get needsLocation(): boolean { return this.options.services.some(s => this.createServices.includes(s.businessSubServiceId) && s.requiresServiceLocation); }
  createStart = '';
  createEnd = '';
  createServices: number[] = [];
  private destroyed = new Subject<void>();
  private searchChanges = new Subject<string>();
  private listRequest?: Subscription;
  private optionRequest?: Subscription;
  private teamRequest?: Subscription;

  constructor(private route: ActivatedRoute, private api: JobsService, private snacks: MatSnackBar) {}
  ngOnInit(): void {
    this.searchChanges.pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroyed)).subscribe(() => this.load());
    this.route.paramMap.pipe(takeUntil(this.destroyed)).subscribe(params => {
      this.code = params.get('businessCode')?.trim() ?? '';
      this.data = null; this.options = { employees: [], services: [], customers: [] }; this.closeModals();
      if (!this.code) { this.error = 'Business code is missing.'; return; }
      this.load(); this.loadOptions();
    });
  }
  ngOnDestroy(): void { this.destroyed.next(); this.destroyed.complete(); this.listRequest?.unsubscribe(); this.optionRequest?.unsubscribe(); this.teamRequest?.unsubscribe(); }
  @HostListener('document:keydown.escape') onEscape(): void { this.closeModals(); }
  search(): void { this.searchChanges.next(this.filters.search); }
  load(page = 1): void {
    this.listRequest?.unsubscribe(); this.loading = true; this.error = '';
    if (page === 1) this.applied = { ...this.filters };
    this.listRequest = this.api.list(this.code, this.applied, page).subscribe({ next: data => {
      if (data.total > 0 && data.jobs.length === 0 && page > 1) { this.load(page - 1); return; }
      this.data = data; this.page = page; this.loading = false;
    }, error: err => { this.loading = false; this.error = this.errorText(err, 'Could not load jobs. Try again.'); if (err.status === 401 || err.status === 403) this.data = null; } });
  }
  loadOptions(): void {
    this.optionRequest?.unsubscribe(); this.optionsError = '';
    this.optionRequest = this.api.options(this.code).subscribe({ next: result => this.options = result, error: err => this.optionsError = this.errorText(err, 'Could not load employee and service choices.') });
  }
  status(key: string): void { this.filters.filter = key; this.load(); }
  reset(): void { this.filters = { search: '', filter: 'all', start: '', end: '', employeeId: null }; this.load(); }
  nextWeek(): void {
    const now = new Date(); const last = new Date(now.getTime() + 6 * 86400000);
    this.filters = { search: '', filter: 'all', employeeId: null, start: now.toISOString().slice(0,10), end: last.toISOString().slice(0,10) };
    this.showFilters = true; this.load();
    document.getElementById('job-queue')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
  team(job: OperationalJob): string { return job.assignedEmployees.map(e => e.displayName).join(', ') || 'Unassigned'; }
  date(value: string): string { const parts = value.slice(0,10).split('-').map(Number); return new Date(parts[0],parts[1]-1,parts[2]).toLocaleDateString('en-US',{month:'short',day:'numeric',year:'numeric'}); }
  time(value: string): string { return value.slice(11,16); }
  location(job: OperationalJob): string { const l = job.serviceLocation; return l ? [l.address1,l.address2,l.city,l.state,l.zipCode].filter(Boolean).join(', ') : 'No customer service location recorded'; }
  mapsUrl(job: OperationalJob): string | null { return job.serviceLocation?.address1 ? 'https://www.google.com/maps/search/?api=1&query=' + encodeURIComponent(this.location(job)) : null; }
  details(job: OperationalJob): void { this.detail = job; this.modalError = ''; }
  closeModals(): void {
    if (this.mutating) return;
    this.detail = null; this.editingTeam = null; this.scheduling = null; this.confirmation = null; this.creating = false;
    this.modalError = ''; this.teamRequest?.unsubscribe(); this.teamLoading = false;
  }
  backdrop(event: MouseEvent): void { if (event.target === event.currentTarget) this.closeModals(); }
  openTeam(job: OperationalJob): void {
    if (!job.canReassign || this.loading || this.mutating) return;
    this.closeModals(); this.editingTeam = job; this.selectedIds = job.assignedEmployees.map(e=>e.businessUserId); this.teamSearch = ''; this.fetchTeam();
  }
  fetchTeam(): void {
    if (!this.editingTeam) return;
    this.teamRequest?.unsubscribe(); this.teamLoading = true; this.teamLoaded = false; this.modalError = ''; this.teamOptions = [];
    this.teamRequest = this.api.teamOptions(this.code,this.editingTeam.jobId).subscribe({ next: choices => {
      this.teamOptions = choices; this.teamLoading = false; this.teamLoaded = true;
    }, error: err => { this.teamLoading = false; this.modalError = this.errorText(err,'Could not check availability. Retry before saving.'); } });
  }
  get visibleTeam(): JobEmployeeOption[] { const term = this.teamSearch.toLowerCase().trim(); return this.teamOptions.filter(e=>e.displayName.toLowerCase().includes(term)); }
  toggleTeam(id: number): void { this.modalError = ''; this.selectedIds = this.selectedIds.includes(id) ? this.selectedIds.filter(i=>i!==id) : [...this.selectedIds,id]; }
  get teamChanged(): boolean { return !!this.editingTeam && [...this.selectedIds].sort().join(',') !== this.editingTeam.assignedEmployees.map(e=>e.businessUserId).sort().join(','); }
  get canSaveTeam(): boolean { return !this.mutating && !this.teamLoading && this.teamLoaded && this.teamChanged && this.selectedIds.every(id=>this.teamOptions.some(e=>e.businessUserId===id && e.canAssign)); }
  saveTeam(): void { if (this.editingTeam && this.canSaveTeam) this.perform(this.editingTeam,'reassign',{businessUserIds:this.selectedIds}); }
  reschedule(job: OperationalJob): void {
    this.closeModals(); this.scheduling = job;
    const [year, month, day] = job.startDateTime.slice(0,10).split('-').map(Number);
    const selected = new Date(year, month - 1, day);
    this.scheduleDate = selected < this.scheduleToday ? null : selected;
    this.scheduleFrom = job.startDateTime.slice(11,16); this.scheduleUntil = job.endDateTime.slice(11,16);
    this.scheduleEndDay = Math.round((Date.parse(job.endDateTime.slice(0,10) + 'T00:00:00Z') - Date.parse(job.startDateTime.slice(0,10) + 'T00:00:00Z')) / 86400000);
    this.updateSchedule();
  }
  saveSchedule(): void { if (this.scheduling && !this.scheduleProblem && this.scheduleChanged) this.perform(this.scheduling,'reschedule',{startDateTime:this.scheduleStart,endDateTime:this.scheduleEnd}); }
  confirm(job: OperationalJob, action: 'complete' | 'cancel'): void { this.closeModals(); this.confirmation = {job,action}; }
  confirmAction(): void { if(this.confirmation) this.perform(this.confirmation.job,this.confirmation.action); }
  perform(job: OperationalJob, action: string, body: object = {}): void {
    if (this.loading || this.mutating) return;
    this.mutating = true; this.modalError = '';
    this.api.action(this.code,job,action,body).pipe(takeUntil(this.destroyed)).subscribe({ next: result => {
      this.mutating = false; this.closeModals(); this.snacks.open(result.message,'Close',{duration:3000}); this.load(this.page);
    }, error: err => {
      this.mutating = false; const message = this.errorText(err,'Could not update this job.');
      if(this.editingTeam || this.scheduling || this.confirmation) this.modalError = message; else this.error = message;
      if(err.status===409) { this.closeModals(); this.load(this.page); this.snacks.open(message,'Close',{duration:5000}); }
    } });
  }
  newJob(): void { this.closeModals(); this.creating = true; this.createName='';this.createCustomer=null;this.createLocation={address1:'',address2:'',city:'',state:'',zipCode:''};this.createStart='';this.createEnd='';this.createServices=[]; }
  toggleService(id: number): void { this.createServices = this.createServices.includes(id) ? this.createServices.filter(i=>i!==id) : [...this.createServices,id]; }
  create(): void {
    if (this.mutating || this.loading) return;
    this.mutating = true;this.modalError='';
    this.api.create(this.code,{name:this.createName,customerUserId:this.createCustomer,serviceLocation:this.needsLocation ? this.createLocation : null,startDateTime:this.createStart,endDateTime:this.createEnd,businessSubServiceIds:this.createServices,businessUserIds:[]}).pipe(takeUntil(this.destroyed)).subscribe({next: result=>{
      this.mutating=false;this.closeModals();this.snacks.open(result.message,'Close',{duration:4500});this.reset();
    },error:err=>{this.mutating=false;this.modalError=this.errorText(err,'Could not create this job.');}});
  }
  image(value?: string | null): string { return !value ? '' : value.startsWith('data:image') ? value : `data:image/png;base64,${value}`; }
  private errorText(err: any, fallback: string): string { return err.error?.errors?.[0] ?? (err.status===403 ? 'You do not have permission for this action.' : fallback); }
}
