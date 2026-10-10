import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DecisionApiService } from '../core/decision-api.service';
import { Decision, PagedResult } from '../core/models';

@Component({
  selector: 'app-decisions',
  imports: [DatePipe, FormsModule, RouterLink],
  templateUrl: './decisions.component.html',
  styleUrl: './decisions.component.css',
})
export class DecisionsComponent implements OnInit {
  private readonly api = inject(DecisionApiService);

  protected readonly pageData = signal<PagedResult<Decision> | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly createError = signal('');
  protected readonly page = signal(1);
  protected statusFilter = '';
  protected sortBy = 'CreatedAt';
  protected sortDirection = 'desc';
  protected title = '';
  protected description = '';
  protected creating = false;

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set('');
    this.api
      .getDecisions(
        this.page(),
        this.statusFilter,
        this.sortBy,
        this.sortDirection,
      )
      .subscribe({
      next: (result) => {
        this.pageData.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load decisions. Check the API connection and try again.');
        this.loading.set(false);
      },
      });
  }

  protected filterChanged(): void {
    this.page.set(1);
    this.load();
  }

  protected changePage(delta: number): void {
    const nextPage = this.page() + delta;
    if (nextPage < 1 || nextPage > this.pageCount()) {
      return;
    }
    this.page.set(nextPage);
    this.load();
  }

  protected pageCount(): number {
    const data = this.pageData();
    return data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;
  }

  protected createDecision(): void {
    if (!this.title.trim() || this.creating) {
      return;
    }

    this.creating = true;
    this.createError.set('');
    this.api.createDecision(this.title.trim(), this.description.trim()).subscribe({
      next: () => {
        this.title = '';
        this.description = '';
        this.creating = false;
        this.page.set(1);
        this.load();
      },
      error: (error: unknown) => {
        this.creating = false;
        this.createError.set(
          error instanceof HttpErrorResponse && error.status === 400
            ? 'The decision details are invalid.'
            : 'Could not create the decision. Try again.',
        );
      },
    });
  }
}
