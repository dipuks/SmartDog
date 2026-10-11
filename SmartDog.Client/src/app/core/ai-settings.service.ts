import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AiSettingsService {
  private apiKey: string | null = null;

  hasApiKey(): boolean {
    return this.apiKey !== null;
  }

  setApiKey(apiKey: string): boolean {
    const normalized = apiKey.trim();
    if (normalized.length < 10 || normalized.length > 256) {
      return false;
    }

    this.apiKey = normalized;
    return true;
  }

  getApiKey(): string | null {
    return this.apiKey;
  }

  clear(): void {
    this.apiKey = null;
  }
}
