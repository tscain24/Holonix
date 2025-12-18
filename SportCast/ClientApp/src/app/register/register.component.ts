import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AuthService, RegisterRequest } from '../auth.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css'],
})
export class RegisterComponent {
  loading = false;
  error = '';
  success = '';
  readonly today = new Date().toISOString().split('T')[0];

  form = this.fb.nonNullable.group({
    firstname: ['', [Validators.required, Validators.minLength(2)]],
    lastname: ['', [Validators.required, Validators.minLength(2)]],
    dateOfBirth: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  constructor(private fb: FormBuilder, private auth: AuthService) {}

  openDatePicker(dateInput: HTMLInputElement): void {
    if ('showPicker' in dateInput && typeof (dateInput as any).showPicker === 'function') {
      (dateInput as any).showPicker();
    } else {
      dateInput.focus();
      dateInput.click();
    }
  }

  submit(): void {
    this.error = '';
    this.success = '';
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    const payload: RegisterRequest = this.form.getRawValue();
    this.auth.register(payload).subscribe({
      next: (res) => {
        this.loading = false;
        this.success = `Welcome, ${res.firstname}! Account created.`;
      },
      error: (err) => {
        this.loading = false;
        this.error = err.status === 409 ? 'Email already in use' : 'Registration failed. Please try again.';
      },
    });
  }
}
