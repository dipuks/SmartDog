import { Routes } from '@angular/router';
import { LoginComponent } from './auth/login.component';
import { authGuard } from './core/auth.guard';
import { DecisionDetailComponent } from './decisions/decision-detail.component';
import { DecisionsComponent } from './decisions/decisions.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'decisions', component: DecisionsComponent, canActivate: [authGuard] },
  {
    path: 'decisions/:id',
    component: DecisionDetailComponent,
    canActivate: [authGuard],
  },
  { path: '', pathMatch: 'full', redirectTo: 'decisions' },
  { path: '**', redirectTo: 'decisions' },
];
