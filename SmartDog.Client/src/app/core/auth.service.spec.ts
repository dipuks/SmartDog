import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('stores a successful login token for the current browser session', () => {
    service.login('admin', 'password').subscribe();

    const request = http.expectOne(`${environment.apiBaseUrl}/auth/login`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      username: 'admin',
      password: 'password',
    });
    request.flush({
      accessToken: 'test-token',
      expiresAtUtc: '2026-10-10T00:00:00Z',
    });

    expect(service.isAuthenticated()).toBe(true);
    expect(service.token()).toBe('test-token');
    expect(sessionStorage.getItem('smartdog.accessToken')).toBe('test-token');
  });

  it('clears the session token on logout', () => {
    sessionStorage.setItem('smartdog.accessToken', 'test-token');
    const authenticatedService = TestBed.inject(AuthService);

    authenticatedService.logout();

    expect(authenticatedService.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem('smartdog.accessToken')).toBeNull();
  });
});
