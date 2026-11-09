import { ChangeDetectionStrategy, Component, Input, OnInit } from '@angular/core';
import { HomeService, PendingInvite } from '../../../core/services/home.service';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BusinessService, MyAssignedJob, UserBusinessSummary } from '../../../core/services/business.service';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-home-summary',
  templateUrl: './home-summary.component.html',
  styleUrls: ['./home-summary.component.css'],
  changeDetection: ChangeDetectionStrategy.Default,
})
export class HomeSummaryComponent implements OnInit {
  @Input() firstName = 'User';

  loadingPendingInvites = true;
  pendingInvites: PendingInvite[] = [];
  selectedInvite: PendingInvite | null = null;
  updatingInvite = false;
  loadingDashboard = true;
  businesses: UserBusinessSummary[] = [];
  assignedJobs: MyAssignedJob[] = [];

  constructor(
    private readonly homeService: HomeService,
    private readonly businessService: BusinessService,
    private readonly authSession: AuthSessionService,
    private readonly snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    const today = new Date();
    const end = new Date(today);
    end.setDate(end.getDate() + 60);
    forkJoin({
      pendingInvites: this.homeService.getPendingInvites().pipe(catchError(() => of([] as PendingInvite[]))),
      businesses: this.businessService.getMyBusinesses().pipe(catchError(() => of([] as UserBusinessSummary[]))),
      jobs: this.businessService.getMyAssignedJobs(this.toIsoDate(today), this.toIsoDate(end)).pipe(catchError(() => of([] as MyAssignedJob[]))),
    }).subscribe(({ pendingInvites, businesses, jobs }) => {
      this.pendingInvites = pendingInvites;
      this.businesses = businesses;
      this.assignedJobs = jobs.filter((job) => new Date(job.endDateTime).getTime() >= Date.now())
        .sort((a, b) => new Date(a.startDateTime).getTime() - new Date(b.startDateTime).getTime());
      this.loadingPendingInvites = false;
      this.loadingDashboard = false;
    });
  }

  get hasWorkAccess(): boolean { return this.businesses.length > 0 || this.assignedJobs.length > 0; }
  get upcomingJobs(): MyAssignedJob[] { return this.assignedJobs.slice(0, 6); }

  formatJobDate(value: string): string {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? 'Date unavailable' : new Intl.DateTimeFormat('en-US', {
      weekday: 'short', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit',
    }).format(date);
  }

  trackJob(_: number, job: MyAssignedJob): number { return job.jobId; }

  private toIsoDate(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  formatInviteDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return 'Unavailable';
    }

    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(date);
  }

  formatInviteTime(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return 'Unavailable';
    }

    return new Intl.DateTimeFormat('en-US', {
      hour: 'numeric',
      minute: '2-digit',
    }).format(date);
  }

  formatInvitedBy(invite: PendingInvite): string {
    const displayName = invite.invitedByDisplayName?.trim();
    const email = invite.invitedByEmail?.trim();

    if (displayName && email) {
      return `${displayName} (${email})`;
    }

    return displayName || email || 'Workspace User';
  }

  trackPendingInvite(_: number, invite: PendingInvite): number {
    return invite.workloadId;
  }

  openInviteDetails(invite: PendingInvite): void {
    this.selectedInvite = invite;
  }

  closeInviteDetails(): void {
    if (this.updatingInvite) {
      return;
    }

    this.selectedInvite = null;
  }

  viewButtonLabel(invite: PendingInvite): string {
    if (invite.inviteType.toLowerCase() === 'employee invite') {
      return 'View';
    }

    return 'View';
  }

  acceptSelectedInvite(): void {
    if (!this.selectedInvite || this.updatingInvite) {
      return;
    }

    this.updatingInvite = true;
    this.homeService.acceptPendingInvite(this.selectedInvite.workloadId).subscribe({
      next: () => {
        const workloadId = this.selectedInvite?.workloadId;
        this.pendingInvites = this.pendingInvites.filter((invite) => invite.workloadId !== workloadId);
        this.selectedInvite = null;
        this.updatingInvite = false;
        this.snackBar.open('Invite accepted.', 'Close', {
          duration: 3000,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.updatingInvite = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        this.snackBar.open(this.extractErrors(err)[0] ?? 'Could not accept invite.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  denySelectedInvite(): void {
    if (!this.selectedInvite || this.updatingInvite) {
      return;
    }

    this.updatingInvite = true;
    this.homeService.denyPendingInvite(this.selectedInvite.workloadId).subscribe({
      next: () => {
        const workloadId = this.selectedInvite?.workloadId;
        this.pendingInvites = this.pendingInvites.filter((invite) => invite.workloadId !== workloadId);
        this.selectedInvite = null;
        this.updatingInvite = false;
        this.snackBar.open('Invite denied.', 'Close', {
          duration: 3000,
          panelClass: ['snack-success'],
        });
      },
      error: (err) => {
        this.updatingInvite = false;
        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }

        this.snackBar.open(this.extractErrors(err)[0] ?? 'Could not deny invite.', 'Close', {
          duration: 3500,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  private extractErrors(err: unknown): string[] {
    const apiErrors = (err as { error?: { errors?: unknown } } | null | undefined)?.error?.errors;
    return Array.isArray(apiErrors) ? apiErrors.filter((value): value is string => typeof value === 'string') : [];
  }
}
