import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  template: `
    <main class="login-card">
    <img class="login-logo" src="/smartdog-logo.jpg" alt="SmartDog" />
      <h1>{{ registering() ? 'Create your account' : 'Sign in' }}</h1>
      <p class="muted">
        {{ registering() ? 'Create an account to start organizing your decisions.' : 'Sign in to manage your decisions.' }}
      </p>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <label for="username">Username</label>
        <input id="username" formControlName="username" autocomplete="username" required maxlength="100" />
        @if (form.controls.username.touched && form.controls.username.invalid) {
          <small class="field-error">Use 3–100 letters, numbers, periods, underscores, or hyphens.</small>
        }
        <label for="password">Password</label>
        <input id="password" type="password" formControlName="password" [attr.autocomplete]="registering() ? 'new-password' : 'current-password'" required maxlength="128" />
        @if (form.controls.password.touched && form.controls.password.invalid) {
          <small class="field-error">{{ registering() ? 'Use at least 12 characters for your password.' : 'Enter your password.' }}</small>
        }
        @if (registering()) {
          <label for="confirm-password">Confirm password</label>
          <input id="confirm-password" type="password" formControlName="confirmPassword" autocomplete="new-password" required maxlength="128" />
          @if (form.controls.confirmPassword.touched && (form.controls.confirmPassword.invalid || form.hasError('passwordMismatch'))) {
            <small class="field-error">Enter the same password again.</small>
          }
        }
        @if (error()) {
          <p class="error-banner" role="alert">{{ error() }}</p>
        }
        <button class="primary-button" type="submit" [disabled]="form.invalid || loading()">
          {{ loading() ? (registering() ? 'Creating account…' : 'Signing in…') : (registering() ? 'Create account' : 'Sign in') }}
        </button>
      </form>
      <p class="auth-switch">
        {{ registering() ? 'Already have an account?' : 'New to SmartDog?' }}
        <button type="button" (click)="toggleMode()" [disabled]="loading()">
          {{ registering() ? 'Sign in' : 'Create an account' }}
        </button>
      </p>
    </main>
  `,
  styles: `
    :host { display: grid; min-height: calc(100vh - 5rem); place-items: center; padding: 2rem 1rem; background: radial-gradient(ellipse at 50% 10%, #fff0d7 0%, transparent 44%); }
    .login-card { width: min(100%, 26rem); padding: 2.5rem; border: 1px solid #eddfcd; border-radius: 1.15rem; background: linear-gradient(150deg, #fff, #fffdf9); box-shadow: 0 20px 55px rgb(91 57 23 / 10%); }
    .login-logo { display: block; width: 8.5rem; max-height: 6rem; margin: 0 auto 1rem; object-fit: contain; }
    h1 { margin: 0 0 .5rem; font-size: 2rem; }
    .eyebrow { margin: 0 0 .5rem; color: var(--accent); font-size: .75rem; font-weight: 800; letter-spacing: .14em; text-transform: uppercase; }
    .muted { margin: 0 0 1.5rem; color: var(--muted); }
    form { display: grid; gap: .65rem; }
    label { margin-top: .5rem; font-size: .9rem; font-weight: 700; }
    input { width: 100%; padding: .75rem; border: 1px solid var(--line); border-radius: .65rem; background: #fff; }
    input:focus { border-color: #d69a4b; outline: 3px solid #f7e7ce; outline-offset: 1px; }
    .primary-button { margin-top: .75rem; }
    .field-error { color: var(--danger); }
    .error-banner { margin: .25rem 0 0; color: var(--danger); }
    .auth-switch { margin: 1.25rem 0 0; color: var(--muted); font-size: .88rem; text-align: center; }
    .auth-switch button { padding: .2rem; border: 0; background: transparent; color: var(--accent-dark); font: inherit; font-weight: 750; cursor: pointer; }
    .auth-switch button:hover { text-decoration: underline; }
    .auth-switch button:disabled { cursor: not-allowed; }
  `,
})
export class LoginComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly registering = signal(false);
  protected readonly form = this.formBuilder.nonNullable.group(
    {
      username: [
        '',
        [
          Validators.required,
          Validators.minLength(3),
          Validators.maxLength(100),
          Validators.pattern(/^[a-zA-Z0-9._-]+$/),
        ],
      ],
      password: ['', Validators.required],
      confirmPassword: [''],
    },
    { validators: (control) => this.passwordsMatch(control) },
  );

  protected toggleMode(): void {
    this.registering.update((registering) => !registering);
    this.error.set('');
    this.form.reset({ username: '', password: '', confirmPassword: '' });

    this.form.controls.password.setValidators(
      this.registering()
        ? [Validators.required, Validators.minLength(12), Validators.maxLength(128)]
        : [Validators.required],
    );
    this.form.controls.confirmPassword.setValidators(
      this.registering() ? [Validators.required] : [],
    );
    this.form.controls.password.updateValueAndValidity();
    this.form.controls.confirmPassword.updateValueAndValidity();
    this.form.updateValueAndValidity();
  }

  protected submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set('');
    const username = this.form.controls.username.value.trim();
    const password = this.form.controls.password.value;
    const request = this.registering()
      ? this.auth.register(username, password)
      : this.auth.login(username, password);

    request.subscribe({
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
              ? 'Too many attempts. Please wait a while and try again.'
              : this.registering() &&
                  error instanceof HttpErrorResponse &&
                  error.status === 409
                ? 'That username is already taken. Choose another one.'
                : this.registering()
                  ? 'Could not create your account. Check your details and try again.'
                  : 'Sign-in failed. Check your credentials and try again.',
          );
        },
      });
  }

  private passwordsMatch(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password')?.value;
    const confirmation = control.get('confirmPassword')?.value;
    return confirmation && password !== confirmation
      ? { passwordMismatch: true }
      : null;
  }
}
