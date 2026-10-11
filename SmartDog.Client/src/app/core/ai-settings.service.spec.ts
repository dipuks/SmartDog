import { TestBed } from '@angular/core/testing';
import { AiSettingsService } from './ai-settings.service';

describe('AiSettingsService', () => {
  let service: AiSettingsService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AiSettingsService);
  });

  it('keeps a valid key only in memory and clears it when requested', () => {
    expect(service.hasApiKey()).toBe(false);
    expect(service.setApiKey('  sk-test-private-key  ')).toBe(true);
    expect(service.hasApiKey()).toBe(true);
    expect(service.getApiKey()).toBe('sk-test-private-key');

    service.clear();

    expect(service.hasApiKey()).toBe(false);
    expect(service.getApiKey()).toBeNull();
  });

  it('rejects empty or overly long keys', () => {
    expect(service.setApiKey('short')).toBe(false);
    expect(service.setApiKey('x'.repeat(257))).toBe(false);
    expect(service.hasApiKey()).toBe(false);
  });
});
