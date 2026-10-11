import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResponse } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storageKey = 'smartdog.accessToken';
  private readonly currentToken = signal(this.readStoredToken());

  readonly isAuthenticated = computed(() => this.currentToken() !== null);

  token(): string | null {
    return this.currentToken();
  }

  login(username: string, password: string) {
    return this.http
      .post<LoginResponse>(`${environment.apiBaseUrl}/auth/login`, {
        username,
        password,
      })
      .pipe(tap((response) => this.storeToken(response)));
  }

  register(username: string, password: string) {
    return this.http
      .post<LoginResponse>(`${environment.apiBaseUrl}/auth/register`, {
        username,
        password,
      })
      .pipe(tap((response) => this.storeToken(response)));
  }

  logout(): void {
    sessionStorage.removeItem(this.storageKey);
    this.currentToken.set(null);
  }

  private readStoredToken(): string | null {
    return typeof sessionStorage === 'undefined'
      ? null
      : sessionStorage.getItem(this.storageKey);
  }

  private storeToken(response: LoginResponse): void {
    sessionStorage.setItem(this.storageKey, response.accessToken);
    this.currentToken.set(response.accessToken);
  }
}
