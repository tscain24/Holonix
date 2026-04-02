import { Component, Input, OnInit } from '@angular/core';
import { HomeService, PendingInvite } from '../../../core/services/home.service';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-home-summary',
  templateUrl: './home-summary.component.html',
  styleUrls: ['./home-summary.component.css'],
})
export class HomeSummaryComponent implements OnInit {
  @Input() firstName = 'User';

  loadingPendingInvites = true;
  pendingInvites: PendingInvite[] = [];
  selectedInvite: PendingInvite | null = null;
  updatingInvite = false;

  constructor(
    private readonly homeService: HomeService,
    private readonly authSession: AuthSessionService,
    private readonly snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.homeService.getPendingInvites().subscribe({
      next: (pendingInvites) => {
        this.pendingInvites = pendingInvites;
        this.loadingPendingInvites = false;
      },
      error: () => {
        this.loadingPendingInvites = false;

        if (this.authSession.hasSessionExpiredFlag()) {
          return;
        }
      },
    });
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
