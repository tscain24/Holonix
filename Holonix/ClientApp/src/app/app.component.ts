import { Component } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'Holonix';
  readonly currentYear = new Date().getFullYear();
  hasVisibleHeader = true;

  constructor(private readonly router: Router) {
    this.applyShellRouteState(this.router.url);
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.applyShellRouteState(event.urlAfterRedirects);
        window.setTimeout(() => {
          window.scrollTo({ top: 0, left: 0, behavior: 'auto' });
          document.documentElement.scrollTop = 0;
          document.body.scrollTop = 0;
        }, 0);
      });
  }

  private applyShellRouteState(url: string): void {
    const route = url.split('?')[0].split('#')[0];
    this.hasVisibleHeader = route !== '/login' && route !== '/register';
  }
}
