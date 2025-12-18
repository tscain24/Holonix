import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
})
export class LoginComponent {
  loading = false;
  error = '';
  success = '';

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  constructor(private fb: FormBuilder, private auth: AuthService) {}

  submit(): void {
    this.error = '';
    this.success = '';
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    const payload = this.form.getRawValue(); // ensures defined fields
    this.auth.login(payload).subscribe({
      next: (res) => {
        this.loading = false;
        this.success = `Signed in as ${res.displayName} (token: ${res.token.slice(0, 8)}...)`;
      },
      error: (err) => {
        this.loading = false;
        this.error = err.status === 401 ? 'Invalid Credentials' : 'Login Failed. Please Try Again or poop on it.';
      },
    });
  }
}
