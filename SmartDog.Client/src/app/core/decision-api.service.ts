import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import {
  Decision,
  DecisionAlternative,
  DecisionAlternativeRating,
  DecisionCriterion,
  DecisionResult,
  PagedResult,
} from './models';

@Injectable({ providedIn: 'root' })
export class DecisionApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/decisions`;

  getDecisions(page: number, status: string, sortBy: string, direction: string) {
    let params = new HttpParams()
      .set('Page', page)
      .set('PageSize', 10)
      .set('SortBy', sortBy)
      .set('SortDirection', direction);

    if (status) {
      params = params.set('Status', status);
    }

    return this.http.get<PagedResult<Decision>>(this.baseUrl, { params });
  }

  getDecision(id: string) {
    return this.http.get<Decision>(`${this.baseUrl}/${id}`);
  }

  createDecision(title: string, description: string) {
    return this.http.post<Decision>(this.baseUrl, { title, description });
  }

  updateDecision(id: string, decision: Decision) {
    return this.http.put<Decision>(`${this.baseUrl}/${id}`, {
      title: decision.title,
      description: decision.description,
      status: decision.status,
      rowVersion: decision.rowVersion,
    });
  }

  getAlternatives(decisionId: string) {
    return this.http.get<DecisionAlternative[]>(
      `${this.baseUrl}/${decisionId}/alternatives`,
    );
  }

  createAlternative(
    decisionId: string,
    name: string,
    description: string,
  ) {
    return this.http.post<DecisionAlternative>(
      `${this.baseUrl}/${decisionId}/alternatives`,
      { name, description, score: 0 },
    );
  }

  deleteAlternative(decisionId: string, alternativeId: string) {
    return this.http.delete<void>(
      `${this.baseUrl}/${decisionId}/alternatives/${alternativeId}`,
    );
  }

  getCriteria(decisionId: string) {
    return this.http.get<DecisionCriterion[]>(
      `${this.baseUrl}/${decisionId}/criteria`,
    );
  }

  createCriterion(decisionId: string, name: string, weight: number) {
    return this.http.post<DecisionCriterion>(
      `${this.baseUrl}/${decisionId}/criteria`,
      { name, weight },
    );
  }

  deleteCriterion(decisionId: string, criterionId: string) {
    return this.http.delete<void>(
      `${this.baseUrl}/${decisionId}/criteria/${criterionId}`,
    );
  }

  getRatings(decisionId: string, alternativeId: string) {
    return this.http.get<DecisionAlternativeRating[]>(
      `${this.baseUrl}/${decisionId}/alternatives/${alternativeId}/ratings`,
    );
  }

  setRating(
    decisionId: string,
    alternativeId: string,
    criterionId: string,
    rating: number,
  ) {
    return this.http.put<DecisionAlternativeRating>(
      `${this.baseUrl}/${decisionId}/alternatives/${alternativeId}/ratings/${criterionId}`,
      { rating },
    );
  }

  getResults(decisionId: string) {
    return this.http.get<DecisionResult[]>(
      `${this.baseUrl}/${decisionId}/results`,
    );
  }
}
