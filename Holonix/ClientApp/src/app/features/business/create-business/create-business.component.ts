import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';

@Component({
  selector: 'app-create-business',
  templateUrl: './create-business.component.html',
  styleUrls: ['./create-business.component.css'],
})
export class CreateBusinessComponent implements OnInit {
  businessForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(1000)]],
    address1: ['', [Validators.required, Validators.maxLength(200)]],
    address2: ['', [Validators.maxLength(200)]],
    city: ['', [Validators.required, Validators.maxLength(120)]],
    state: ['', [Validators.required, Validators.maxLength(120)]],
    zipCode: ['', [Validators.required, Validators.maxLength(32)]],
    country: ['United States', [Validators.required, Validators.maxLength(120)]],
  });

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    if (!localStorage.getItem('holonix_token')) {
      this.router.navigate(['/login']);
    }
  }

  goBack(): void {
    this.router.navigate(['/profile']);
  }

  submit(): void {
    if (this.businessForm.invalid) {
      this.businessForm.markAllAsTouched();
      return;
    }

    this.snackBar.open('Business creation flow is ready for backend wiring.', 'Close', {
      duration: 3000,
      panelClass: ['snack-success'],
    });
  }
}
