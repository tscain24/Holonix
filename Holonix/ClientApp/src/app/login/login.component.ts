import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
})
export class LoginComponent {
  loading = false;
  showPassword = false;

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private snackBar: MatSnackBar,
    private router: Router
  ) {}

  ngOnInit(): void {
    const message = (history.state && history.state.toastMessage) as string | undefined;
    if (message) {
      this.snackBar.open(message, 'Close', {
        duration: 3500,
        panelClass: ['snack-success'],
      });
      history.replaceState({}, '');
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    const payload = this.form.getRawValue();
    this.auth.login(payload).subscribe({
      next: (res) => {
        this.loading = false;
        this.snackBar.open(`Signed in as ${res.displayName}.`, 'Close', {
          duration: 3000,
          panelClass: ['snack-success'],
        });
        this.router.navigate(['/home'], { state: { displayName: res.displayName } });
      },
      error: (err) => {
        this.loading = false;
        const errors = err?.error?.errors as string[] | undefined;
        let message = err.status === 401 ? 'Invalid credentials' : 'Login failed. Please try again.';
        if (errors && errors.length > 0) {
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
