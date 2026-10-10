import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  template: `
    <main class="login-card">
      <p class="eyebrow">SmartDog</p>
      <h1>Sign in</h1>
      <p class="muted">Sign in to manage your decisions.</p>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <label for="username">Username</label>
        <input id="username" formControlName="username" autocomplete="username" required />
        @if (form.controls.username.touched && form.controls.username.invalid) {
          <small class="field-error">Enter your username.</small>
        }
        <label for="password">Password</label>
        <input id="password" type="password" formControlName="password" autocomplete="current-password" required />
        @if (form.controls.password.touched && form.controls.password.invalid) {
          <small class="field-error">Enter your password.</small>
        }
        @if (error()) {
          <p class="error-banner" role="alert">{{ error() }}</p>
        }
        <button class="primary-button" type="submit" [disabled]="form.invalid || loading()">
          {{ loading() ? 'Signing in…' : 'Sign in' }}
        </button>
      </form>
    </main>
  `,
  styles: `
    :host { display: grid; min-height: calc(100vh - 5rem); place-items: center; padding: 2rem 1rem; }
    .login-card { width: min(100%, 26rem); padding: 2.5rem; border: 1px solid var(--line); border-radius: 1rem; background: var(--surface); box-shadow: var(--shadow); }
    h1 { margin: 0 0 .5rem; font-size: 2rem; }
    .eyebrow { margin: 0 0 .5rem; color: var(--accent); font-size: .75rem; font-weight: 800; letter-spacing: .14em; text-transform: uppercase; }
    .muted { margin: 0 0 1.5rem; color: var(--muted); }
    form { display: grid; gap: .65rem; }
    label { margin-top: .5rem; font-size: .9rem; font-weight: 700; }
    input { width: 100%; padding: .75rem; border: 1px solid var(--line); border-radius: .5rem; background: #fff; }
    .primary-button { margin-top: .75rem; }
    .field-error { color: var(--danger); }
    .error-banner { margin: .25rem 0 0; color: var(--danger); }
  `,
})
export class LoginComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly form = this.formBuilder.nonNullable.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });

  protected submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set('');
    this.auth
      .login(this.form.controls.username.value, this.form.controls.password.value)
      .subscribe({
        next: () => {
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
          void this.router.navigateByUrl(
            returnUrl?.startsWith('/') ? returnUrl : '/decisions',
          );
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.error.set(
            error instanceof HttpErrorResponse && error.status === 429
              ? 'Too many sign-in attempts. Wait a minute and try again.'
              : 'Sign-in failed. Check your credentials and try again.',
          );
        },
      });
  }
}
