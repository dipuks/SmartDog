import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { DecisionApiService } from './decision-api.service';

describe('DecisionApiService', () => {
  let service: DecisionApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        DecisionApiService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(DecisionApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('requests a sorted, filtered decision page', () => {
    service.getDecisions(2, 'Created', 'Title', 'asc').subscribe();

    const request = http.expectOne(
      (candidate) =>
        candidate.url === `${environment.apiBaseUrl}/decisions`,
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('Page')).toBe('2');
    expect(request.request.params.get('PageSize')).toBe('10');
    expect(request.request.params.get('Status')).toBe('Created');
    expect(request.request.params.get('SortBy')).toBe('Title');
    expect(request.request.params.get('SortDirection')).toBe('asc');
    request.flush({ items: [], page: 2, pageSize: 10, totalCount: 0 });
  });

  it('posts a criterion to the decision-scoped route', () => {
    const decisionId = 'decision-id';
    const criterion = {
      id: 'criterion-id',
      decisionId,
      name: 'Quality',
      weight: 100,
    };

    service.createCriterion(decisionId, criterion.name, criterion.weight).subscribe();

    const request = http.expectOne(
      `${environment.apiBaseUrl}/decisions/${decisionId}/criteria`,
    );
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      name: 'Quality',
      weight: 100,
    });
    request.flush(criterion);
  });

  it('deletes an alternative using both parent and alternative IDs', () => {
    service.deleteAlternative('decision-id', 'alternative-id').subscribe();

    const request = http.expectOne(
      `${environment.apiBaseUrl}/decisions/decision-id/alternatives/alternative-id`,
    );
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
