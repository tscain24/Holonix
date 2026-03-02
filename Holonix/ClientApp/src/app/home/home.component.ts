import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'],
})
export class HomeComponent {
  isLoggedIn = false;

  ngOnInit(): void {
    const token = localStorage.getItem('holonix_token');
    this.isLoggedIn = !!token;
  }
}
