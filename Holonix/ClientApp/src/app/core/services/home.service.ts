import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';

export interface PendingInvite {
  workloadId: number;
  inviteType: string;
  inviteDate: string;
  inviteCompany: string;
  inviteRole?: string | null;
  invitedByDisplayName: string;
  invitedByEmail?: string | null;
  status: string;
}

@Injectable({
  providedIn: 'root',
})
export class HomeService {
  private readonly pendingInvitesEndpoint = '/api/home/pending-invites';
  private readonly homeEndpoint = '/api/home';
  private readonly fallbackOrigins = [
    'http://localhost:5237',
    'https://localhost:7241',
    'http://localhost:5000',
  ];

  constructor(private readonly http: HttpClient) {}

  getPendingInvites(): Observable<PendingInvite[]> {
    return this.http.get<PendingInvite[]>(this.pendingInvitesEndpoint).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryGetPendingInvitesFallback(0);
      })
    );
  }

  acceptPendingInvite(workloadId: number): Observable<void> {
    return this.http.post<void>(`${this.homeEndpoint}/pending-invites/${workloadId}/accept`, {}).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryUpdatePendingInviteFallback(workloadId, 'accept', 0);
      })
    );
  }

  denyPendingInvite(workloadId: number): Observable<void> {
    return this.http.post<void>(`${this.homeEndpoint}/pending-invites/${workloadId}/deny`, {}).pipe(
      catchError((err) => {
        if (!this.shouldTryNextOrigin(err)) {
          return throwError(() => err);
        }

        return this.tryUpdatePendingInviteFallback(workloadId, 'deny', 0);
      })
    );
  }

  private tryGetPendingInvitesFallback(index: number): Observable<PendingInvite[]> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Pending invites endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.pendingInvitesEndpoint}`;
    return this.http.get<PendingInvite[]>(url).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryGetPendingInvitesFallback(index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private tryUpdatePendingInviteFallback(workloadId: number, action: 'accept' | 'deny', index: number): Observable<void> {
    if (index >= this.fallbackOrigins.length) {
      return throwError(() => new Error('Pending invite action endpoint not found on configured local origins.'));
    }

    const url = `${this.fallbackOrigins[index]}${this.homeEndpoint}/pending-invites/${workloadId}/${action}`;
    return this.http.post<void>(url, {}).pipe(
      catchError((err) => {
        if (this.shouldTryNextOrigin(err)) {
          return this.tryUpdatePendingInviteFallback(workloadId, action, index + 1);
        }

        return throwError(() => err);
      })
    );
  }

  private shouldTryNextOrigin(err: { status?: number } | null | undefined): boolean {
    const status = err?.status;
    return status === 0 || status === 404;
  }
}
