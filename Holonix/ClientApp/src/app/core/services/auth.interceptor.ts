import { Injectable } from '@angular/core';
import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Observable, catchError, switchMap, throwError } from 'rxjs';
import { AuthSessionService } from './auth-session.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private readonly authSession: AuthSessionService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (!this.isProtectedRequest(req.url) || this.isBypassedRequest(req.url)) {
      return next.handle(req);
    }

    return this.authSession.getValidToken().pipe(
      switchMap((token) => {
        const request = token
          ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
          : req;

        return next.handle(request).pipe(
          catchError((err: unknown) => {
            if (!this.shouldAttemptRefresh(err, req.url)) {
              return throwError(() => err);
            }

            return this.authSession.refreshToken().pipe(
              switchMap((refreshedToken) =>
                next.handle(req.clone({ setHeaders: { Authorization: `Bearer ${refreshedToken}` } }))
              ),
              catchError((refreshError) => {
                this.authSession.logoutDueToExpiredSession();
                return throwError(() => refreshError);
              })
            );
          })
        );
      })
    );
  }

  private isProtectedRequest(url: string): boolean {
    if (url.startsWith('/api/')) {
      return true;
    }

    return /^https?:\/\/localhost(?::\d+)?\/api\//i.test(url)
      || /^https?:\/\/127\.0\.0\.1(?::\d+)?\/api\//i.test(url);
  }

  private isBypassedRequest(url: string): boolean {
    return url.includes('/api/auth/login')
      || url.includes('/api/auth/register')
      || url.includes('/api/auth/refresh');
  }

  private shouldAttemptRefresh(err: unknown, url: string): boolean {
    return err instanceof HttpErrorResponse
      && err.status === 401
      && !this.isBypassedRequest(url);
  }
}
