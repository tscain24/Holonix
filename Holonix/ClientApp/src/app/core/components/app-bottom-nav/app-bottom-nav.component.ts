import { Component } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-bottom-nav',
  templateUrl: './app-bottom-nav.component.html',
  styleUrls: ['./app-bottom-nav.component.css'],
})
export class AppBottomNavComponent {
  isLoggedIn = false;
  hideNav = false;

  constructor(private router: Router) {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.applyRouteVisibility(event.urlAfterRedirects);
        this.refreshSessionFromStorage();
      });
  }

  ngOnInit(): void {
    this.applyRouteVisibility(this.router.url);
    this.refreshSessionFromStorage();
  }

  private refreshSessionFromStorage(): void {
    const token = localStorage.getItem('holonix_token');
    this.isLoggedIn = !!token;
  }

  private applyRouteVisibility(url: string): void {
    const route = url.split('?')[0].split('#')[0];
    this.hideNav = route === '/login' || route === '/register';
  }
}
