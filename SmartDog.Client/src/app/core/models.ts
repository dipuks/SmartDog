export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
}

export interface Decision {
  id: string;
  title: string;
  description: string | null;
  createdAt: string;
  status: string;
  rowVersion: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface DecisionAlternative {
  id: string;
  decisionId: string;
  name: string;
  description: string | null;
  score: number;
}

export interface DecisionCriterion {
  id: string;
  decisionId: string;
  name: string;
  weight: number;
}

export interface DecisionResult {
  rank: number;
  alternativeId: string;
  alternativeName: string;
  score: number;
}

export interface DecisionAlternativeRating {
  decisionCriterionId: string;
  criterionName: string;
  rating: number;
}

export interface DecisionAssistantResponse {
  answer: string;
}
