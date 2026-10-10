import { HttpErrorResponse } from '@angular/common/http';
import { DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { DecisionApiService } from '../core/decision-api.service';
import {
  Decision,
  DecisionAlternative,
  DecisionAlternativeRating,
  DecisionCriterion,
  DecisionResult,
} from '../core/models';

@Component({
  selector: 'app-decision-detail',
  imports: [DecimalPipe, FormsModule, RouterLink],
  templateUrl: './decision-detail.component.html',
  styleUrl: './decision-detail.component.css',
})
export class DecisionDetailComponent implements OnInit {
  private readonly api = inject(DecisionApiService);
  private readonly route = inject(ActivatedRoute);

  protected readonly decision = signal<Decision | null>(null);
  protected readonly alternatives = signal<DecisionAlternative[]>([]);
  protected readonly criteria = signal<DecisionCriterion[]>([]);
  protected readonly ratings = signal<
    (DecisionAlternativeRating & { alternativeId: string })[]
  >([]);
  protected readonly results = signal<DecisionResult[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal('');
  protected readonly scoringMessage = signal('');
  protected readonly saving = signal(false);
  protected readonly actionMessage = signal('');
  protected title = '';
  protected description = '';
  protected status = '';
  protected alternativeName = '';
  protected alternativeDescription = '';
  protected criterionName = '';
  protected criterionWeight: number | null = null;
  protected selectedAlternativeId = '';
  protected selectedCriterionId = '';
  protected rating: number | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('A decision ID is required.');
      this.loading.set(false);
      return;
    }
    this.load(id);
  }

  protected load(id: string): void {
    this.loading.set(true);
    this.error.set('');
    forkJoin({
      decision: this.api.getDecision(id),
      alternatives: this.api.getAlternatives(id),
      criteria: this.api.getCriteria(id),
    }).subscribe({
      next: ({ decision, alternatives, criteria }) => {
        this.decision.set(decision);
        this.title = decision.title;
        this.description = decision.description ?? '';
        this.status = decision.status;
        this.alternatives.set(alternatives);
        this.criteria.set(criteria);
        this.loading.set(false);
        this.loadRatings(id, alternatives);
        this.loadResults(id);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.error.set(
          error instanceof HttpErrorResponse && error.status === 404
            ? 'This decision was not found.'
            : 'Could not load this decision. Try again.',
        );
      },
    });
  }

  protected saveDecision(): void {
    const current = this.decision();
    if (!current || !this.title.trim() || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.actionMessage.set('');
    this.api
      .updateDecision(current.id, {
        ...current,
        title: this.title.trim(),
        description: this.description.trim() || null,
        status: this.status,
      })
      .subscribe({
        next: (updated) => {
          this.decision.set(updated);
          this.actionMessage.set('Decision details saved.');
          this.saving.set(false);
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.actionMessage.set(
            error instanceof HttpErrorResponse && error.status === 409
              ? 'This decision changed elsewhere. Reload it before saving again.'
              : 'Could not save changes. Check the fields and try again.',
          );
        },
      });
  }

  protected allowedStatuses(): string[] {
    switch (this.decision()?.status) {
      case 'Draft':
        return ['Draft', 'Created', 'Cancelled'];
      case 'Created':
        return ['Created', 'InProgress', 'Completed', 'Cancelled'];
      case 'InProgress':
        return ['InProgress', 'Completed', 'Cancelled'];
      case 'Completed':
      case 'Cancelled':
        return [this.decision()?.status ?? 'Completed'];
      default:
        return [];
    }
  }

  protected addAlternative(): void {
    const decision = this.decision();
    if (!decision || !this.alternativeName.trim()) {
      return;
    }
    this.saving.set(true);
    this.api
      .createAlternative(
        decision.id,
        this.alternativeName.trim(),
        this.alternativeDescription.trim(),
      )
      .subscribe({
        next: () => {
          this.alternativeName = '';
          this.alternativeDescription = '';
          this.actionMessage.set('Alternative added.');
          this.saving.set(false);
          this.load(decision.id);
        },
        error: () => {
          this.saving.set(false);
          this.actionMessage.set('Could not add the alternative.');
        },
      });
  }

  protected deleteAlternative(alternative: DecisionAlternative): void {
    const decision = this.decision();
    if (
      !decision ||
      !globalThis.confirm(`Remove "${alternative.name}" from this decision?`)
    ) {
      return;
    }

    this.saving.set(true);
    this.api.deleteAlternative(decision.id, alternative.id).subscribe({
      next: () => {
        this.actionMessage.set('Alternative removed.');
        this.saving.set(false);
        this.load(decision.id);
      },
      error: () => {
        this.saving.set(false);
        this.actionMessage.set('Could not remove the alternative.');
      },
    });
  }

  protected addCriterion(): void {
    const decision = this.decision();
    if (
      !decision ||
      !this.criterionName.trim() ||
      this.criterionWeight === null ||
      this.criterionWeight < 0 ||
      this.criterionWeight > 100
    ) {
      return;
    }
    this.saving.set(true);
    this.api
      .createCriterion(decision.id, this.criterionName.trim(), this.criterionWeight)
      .subscribe({
        next: () => {
          this.criterionName = '';
          this.criterionWeight = null;
          this.actionMessage.set('Criterion added. Adjust weights so they total 100.');
          this.saving.set(false);
          this.load(decision.id);
        },
        error: () => {
          this.saving.set(false);
          this.actionMessage.set('Could not add the criterion.');
        },
      });
  }

  protected deleteCriterion(criterion: DecisionCriterion): void {
    const decision = this.decision();
    if (
      !decision ||
      !globalThis.confirm(
        `Remove "${criterion.name}" and its ratings from this decision?`,
      )
    ) {
      return;
    }

    this.saving.set(true);
    this.api.deleteCriterion(decision.id, criterion.id).subscribe({
      next: () => {
        this.actionMessage.set('Criterion and its ratings removed.');
        this.saving.set(false);
        this.load(decision.id);
      },
      error: () => {
        this.saving.set(false);
        this.actionMessage.set('Could not remove the criterion.');
      },
    });
  }

  protected saveRating(): void {
    const decision = this.decision();
    if (
      !decision ||
      !this.selectedAlternativeId ||
      !this.selectedCriterionId ||
      this.rating === null ||
      this.rating < 0 ||
      this.rating > 100
    ) {
      return;
    }

    this.saving.set(true);
    this.api
      .setRating(
        decision.id,
        this.selectedAlternativeId,
        this.selectedCriterionId,
        this.rating,
      )
      .subscribe({
        next: () => {
          this.actionMessage.set('Rating saved.');
          this.saving.set(false);
          this.loadRatings(decision.id, this.alternatives());
          this.loadResults(decision.id);
        },
        error: () => {
          this.saving.set(false);
          this.actionMessage.set('Could not save the rating.');
        },
      });
  }

  protected selectRating(alternativeId: string, criterionId: string): void {
    this.selectedAlternativeId = alternativeId;
    this.selectedCriterionId = criterionId;
    this.rating =
      this.ratings().find(
        (entry) =>
          entry.alternativeId === alternativeId &&
          entry.decisionCriterionId === criterionId,
      )?.rating ?? null;
  }

  protected currentRating(
    alternativeId: string,
    criterionId: string,
  ): number | null {
    return (
      this.ratings().find(
        (entry) =>
          entry.alternativeId === alternativeId &&
          entry.decisionCriterionId === criterionId,
      )?.rating ?? null
    );
  }

  private loadRatings(
    decisionId: string,
    alternatives: DecisionAlternative[],
  ): void {
    if (!alternatives.length) {
      this.ratings.set([]);
      return;
    }

    forkJoin(
      alternatives.map((alternative) =>
        this.api.getRatings(decisionId, alternative.id),
      ),
    ).subscribe({
      next: (ratingsByAlternative) => {
        this.ratings.set(
          ratingsByAlternative.flatMap((ratings, index) =>
            ratings.map((rating) => ({
              ...rating,
              alternativeId: alternatives[index].id,
            })),
          ),
        );
      },
      error: () => {
        this.ratings.set([]);
      },
    });
  }

  private loadResults(id: string): void {
    this.api.getResults(id).subscribe({
      next: (results) => {
        this.results.set(results);
        this.scoringMessage.set('');
      },
      error: (error: unknown) => {
        this.results.set([]);
        this.scoringMessage.set(
          error instanceof HttpErrorResponse && error.status === 409
            ? 'To calculate results, criterion weights must total 100 and every option needs a rating for every criterion.'
            : 'Results are temporarily unavailable.',
        );
      },
    });
  }
}
