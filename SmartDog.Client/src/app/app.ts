import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AiSettingsService } from './core/ai-settings.service';
import { AuthService } from './core/auth.service';

@Component({
  imports: [RouterLink, RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly auth = inject(AuthService);
  private readonly aiSettings = inject(AiSettingsService);
  private readonly router = inject(Router);

  protected logout(): void {
    this.auth.logout();
    this.aiSettings.clear();
    void this.router.navigate(['/login']);
  }
}
