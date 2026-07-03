import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { AuthSessionService } from '../../../core/services/auth-session.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
})
export class LoginComponent {
  loading = false;
  showPassword = false;
  private returnUrl: string | null = null;

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private authSession: AuthSessionService,
    private snackBar: MatSnackBar,
    private route: ActivatedRoute,
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

    if (this.authSession.consumeSessionExpiredFlag()) {
      this.snackBar.open('User has been logged out due to inactivity.', 'Close', {
        duration: 4000,
        panelClass: ['snack-error'],
      });
    }

    const rawReturnUrl = (this.route.snapshot.queryParamMap.get('returnUrl') ?? '').trim();
    this.returnUrl = rawReturnUrl.startsWith('/') ? rawReturnUrl : null;
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
        this.authSession.persistSession(res);
        this.snackBar.open(`Signed in as ${res.displayName}.`, 'Close', {
          duration: 3000,
          panelClass: ['snack-success'],
        });
        if (this.returnUrl) {
          void this.router.navigateByUrl(this.returnUrl, { state: { displayName: res.displayName } });
          return;
        }

        void this.router.navigate(['/home'], { state: { displayName: res.displayName } });
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
