import { Component } from '@angular/core';
import { AbstractControl, FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { AuthService, RegisterRequest } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css'],
})
export class RegisterComponent {
  loading = false;
  readonly maxDate = new Date();
  showPassword = false;

  form = this.fb.group({
    firstname: ['', [Validators.required, Validators.minLength(2)]],
    lastname: ['', [Validators.required, Validators.minLength(2)]],
    phoneNumber: ['', [Validators.maxLength(32)]],
    dateOfBirth: [null as Date | null, [Validators.required, minimumAgeValidator(13)]],
    email: ['', [Validators.required, Validators.email]],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/),
      ],
    ],
  });

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private snackBar: MatSnackBar,
    private router: Router
  ) {}

  formatDobOnInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const raw = input.value;
    const selectionStart = input.selectionStart ?? raw.length;
    const digitsBeforeCursor = raw.slice(0, selectionStart).replace(/\D/g, '').length;
    const digits = raw.replace(/\D/g, '').slice(0, 8);

    let formatted = '';
    if (digits.length <= 2) {
      formatted = digits;
    } else if (digits.length <= 4) {
      formatted = `${digits.slice(0, 2)}/${digits.slice(2)}`;
    } else {
      formatted = `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}`;
    }

    if (formatted !== raw) {
      input.value = formatted;
      let caret = digitsBeforeCursor;
      if (digitsBeforeCursor > 2 && digitsBeforeCursor <= 4) {
        caret += 1;
      } else if (digitsBeforeCursor > 4) {
        caret += 2;
      }
      input.setSelectionRange(caret, caret);
    }
  }

  formatPhoneNumberOnInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const raw = input.value;
    const selectionStart = input.selectionStart ?? raw.length;
    const digitsBeforeCursor = raw.slice(0, selectionStart).replace(/\D/g, '').length;
    const digits = raw.replace(/\D/g, '').slice(0, 11);
    const formatted = formatPhoneNumberValue(digits, raw);

    if (formatted !== raw) {
      input.value = formatted;
      input.setSelectionRange(formatted.length, formatted.length);
    }

    this.form.controls.phoneNumber.setValue(formatted, { emitEvent: false });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    const rawValue = this.form.getRawValue();
    const dateValue = rawValue.dateOfBirth;
    const payload: RegisterRequest = {
      firstName: rawValue.firstname ?? '',
      lastName: rawValue.lastname ?? '',
      phoneNumber: (rawValue.phoneNumber ?? '').trim() || null,
      email: rawValue.email ?? '',
      password: rawValue.password ?? '',
      dateOfBirth: dateValue instanceof Date ? dateValue.toISOString().split('T')[0] : '',
    };
    this.auth.register(payload).subscribe({
      next: (res) => {
        this.loading = false;
        this.router.navigate(['/login'], {
          state: { toastMessage: 'Account created. Log in to continue.' },
        });
      },
      error: (err) => {
        this.loading = false;
        const errors = err?.error?.errors as string[] | undefined;
        let message = 'Registration failed. Please try again.';
        if (err.status === 409) {
          message = 'Email already in use';
        } else if (errors && errors.length > 0) {
          message = errors[0];
        }
        this.snackBar.open(message, 'Close', {
          duration: 4000,
          panelClass: ['snack-error'],
        });
      },
    });
  }
}

function formatPhoneNumberValue(digitsOnly: string, fallback: string): string {
  if (!digitsOnly) {
    return '';
  }

  if (digitsOnly.length <= 3) {
    return digitsOnly;
  }

  if (digitsOnly.length <= 6) {
    return `(${digitsOnly.slice(0, 3)}) ${digitsOnly.slice(3)}`;
  }

  if (digitsOnly.length <= 10) {
    return `(${digitsOnly.slice(0, 3)}) ${digitsOnly.slice(3, 6)}-${digitsOnly.slice(6)}`;
  }

  if (digitsOnly.length === 11 && digitsOnly.startsWith('1')) {
    return `+1 (${digitsOnly.slice(1, 4)}) ${digitsOnly.slice(4, 7)}-${digitsOnly.slice(7)}`;
  }

  return fallback.trim();
}

function minimumAgeValidator(minAge: number) {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!(value instanceof Date)) {
      return null;
    }

    const today = new Date();
    const cutoff = new Date(today.getFullYear() - minAge, today.getMonth(), today.getDate());
    return value > cutoff ? { minAge: true } : null;
  };
}
