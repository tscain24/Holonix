import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil } from 'rxjs/operators';
import { BusinessService, MyAssignedJob, UserBusinessSummary } from '../../../core/services/business.service';

type WorkerView = 'today' | 'schedule' | 'jobs' | 'inbox';

@Component({
  selector: 'app-worker-workspace',
  templateUrl: './worker-workspace.component.html',
  styleUrls: ['./worker-workspace.component.css'],
})
export class WorkerWorkspaceComponent implements OnInit, OnDestroy {
  private readonly destroyed = new Subject<void>();
  private readonly today = new Date();
  view: WorkerView = 'today';
  loading = true;
  error = '';
  businesses: UserBusinessSummary[] = [];
  jobs: MyAssignedJob[] = [];

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly businessService: BusinessService,
  ) {}

  ngOnInit(): void {
    if (!localStorage.getItem('holonix_token')) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: '/worker' } });
      return;
    }
    this.route.queryParamMap.pipe(takeUntil(this.destroyed)).subscribe(params => {
      const requested = params.get('view');
      this.view = requested === 'schedule' || requested === 'jobs' || requested === 'inbox' ? requested : 'today';
    });
    this.load();
  }

  ngOnDestroy(): void {
    this.destroyed.next();
    this.destroyed.complete();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    const start = this.dateIso(this.addDays(this.today, -7));
    const end = this.dateIso(this.addDays(this.today, 24));
    forkJoin({
      businesses: this.businessService.getMyBusinesses(),
      jobs: this.businessService.getMyAssignedJobs(start, end),
    }).pipe(
      catchError(() => {
        this.error = 'We could not load your worker workspace. Check your connection and try again.';
        return of({ businesses: [] as UserBusinessSummary[], jobs: [] as MyAssignedJob[] });
      }),
      takeUntil(this.destroyed),
    ).subscribe(result => {
      this.businesses = result.businesses;
      this.jobs = result.jobs.sort((a, b) => new Date(a.startDateTime).getTime() - new Date(b.startDateTime).getTime());
      this.loading = false;
    });
  }

  selectView(view: WorkerView): void {
    this.router.navigate(['/worker'], { queryParams: view === 'today' ? {} : { view } });
  }

  get todayJobs(): MyAssignedJob[] {
    const key = this.dateIso(this.today);
    return this.jobs.filter(job => this.dateIso(new Date(job.startDateTime)) === key);
  }

  get nextJob(): MyAssignedJob | null {
    const now = Date.now();
    return this.jobs.find(job => new Date(job.endDateTime).getTime() >= now) ?? null;
  }

  get upcomingJobs(): MyAssignedJob[] {
    const now = Date.now();
    return this.jobs.filter(job => new Date(job.endDateTime).getTime() >= now).slice(0, 12);
  }

  get pastJobs(): MyAssignedJob[] {
    const now = Date.now();
    return this.jobs.filter(job => new Date(job.endDateTime).getTime() < now).reverse();
  }

  get recentActivity(): MyAssignedJob[] {
    return this.pastJobs.slice(0, 4);
  }

  get canManageBookings(): boolean {
    return this.businesses.some(business => this.isManagementRole(business.roleName));
  }

  get managedBusinesses(): UserBusinessSummary[] {
    return this.businesses.filter(business => this.isManagementRole(business.roleName));
  }

  jobMapsUrl(job: MyAssignedJob): string | null {
    const location = job.serviceLocation;
    if (!location) return null;
    const address = [location.address1, location.address2, location.city, location.state, location.zipCode].filter(Boolean).join(', ');
    return address ? `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(address)}` : null;
  }

  location(job: MyAssignedJob): string {
    const value = job.serviceLocation;
    return value ? [value.address1, value.address2, value.city, value.state, value.zipCode].filter(Boolean).join(', ') : 'No service address recorded';
  }

  businessFor(job: MyAssignedJob): UserBusinessSummary | undefined {
    return this.businesses.find(business => business.businessCode === job.businessCode);
  }

  icon(business: UserBusinessSummary): string | null {
    const raw = business.businessIconBase64?.trim();
    if (!raw) return null;
    return raw.startsWith('data:') ? raw : `data:image/png;base64,${raw}`;
  }

  initials(name: string): string {
    return name.split(/\s+/).filter(Boolean).slice(0, 2).map(part => part[0]).join('').toUpperCase();
  }

  dateLabel(value: string): string {
    return new Intl.DateTimeFormat(undefined, { weekday: 'short', month: 'short', day: 'numeric' }).format(new Date(value));
  }

  timeLabel(value: string): string {
    return new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit' }).format(new Date(value));
  }

  duration(job: MyAssignedJob): string {
    const minutes = Math.max(0, Math.round((new Date(job.endDateTime).getTime() - new Date(job.startDateTime).getTime()) / 60000));
    const hours = Math.floor(minutes / 60);
    const remainder = minutes % 60;
    return [hours ? `${hours} hr` : '', remainder ? `${remainder} min` : ''].filter(Boolean).join(' ') || '0 min';
  }

  trackJob(_: number, job: MyAssignedJob): number { return job.jobId; }

  private isManagementRole(role?: string | null): boolean {
    return role === 'Owner' || role === 'Administrator' || role === 'Manager';
  }

  private addDays(value: Date, days: number): Date {
    const result = new Date(value);
    result.setDate(result.getDate() + days);
    return result;
  }

  private dateIso(value: Date): string {
    const year = value.getFullYear();
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
