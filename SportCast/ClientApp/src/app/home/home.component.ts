import { Component, HostListener } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'],
})
export class HomeComponent {
  displayName = '';
  firstName = '';
  profileOpen = false;

  constructor(private router: Router) {}

  ngOnInit(): void {
    const name = (history.state && history.state.displayName) as string | undefined;
    if (name) {
      this.displayName = name;
      this.firstName = name.split(' ')[0] || name;
    }
  }

  logout(): void {
    this.router.navigate(['/login']);
  }

  toggleProfileMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.profileOpen = !this.profileOpen;
  }

  closeProfileMenu(): void {
    this.profileOpen = false;
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    if (this.profileOpen) {
      this.profileOpen = false;
    }
  }
}
