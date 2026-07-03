import { NgModule } from '@angular/core';
import { RouterModule, Routes, UrlMatchResult, UrlSegment } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { HomeComponent } from './features/home/home.component';
import { ProfileComponent } from './features/profile/profile.component';
import { CreateBusinessComponent } from './features/business/create-business/create-business.component';
import { BusinessOverviewComponent } from './features/business/business-overview/business-overview.component';
import { BusinessWorkspaceComponent } from './features/business/business-workspace/business-workspace.component';
import { BusinessEmployeesComponent } from './features/business/business-employees/business-employees.component';
import { BusinessServiceManagerComponent } from './features/business/business-service-manager/business-service-manager.component';
import { BusinessAvailabilityComponent } from './features/business/business-availability/business-availability.component';
import { ServiceSearchResultsComponent } from './features/search/service-search-results/service-search-results.component';
import { PublicBusinessPageComponent } from './features/business/public-business-page/public-business-page.component';
import { TermsOfServiceComponent } from './features/legal/terms-of-service/terms-of-service.component';
import { PrivacyPolicyComponent } from './features/legal/privacy-policy/privacy-policy.component';

function publicBusinessCodeMatcher(segments: UrlSegment[]): UrlMatchResult | null {
  if (segments.length !== 1) {
    return null;
  }

  const raw = (segments[0]?.path ?? '').trim();
  if (!raw) {
    return null;
  }

  const reserved = new Set(['home', 'login', 'register', 'profile', 'business', 'businesses', 'search', 'workspace', 'terms', 'privacy']);
  if (reserved.has(raw.toLowerCase())) {
    return null;
  }

  const isUuid =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(raw);
  const isBusinessCode = /^[A-Za-z0-9]{6,64}$/.test(raw);

  if (!isUuid && !isBusinessCode) {
    return null;
  }

  return {
    consumed: segments,
    posParams: { businessCode: segments[0] },
  };
}

const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'home', component: HomeComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'profile', component: ProfileComponent },
  { path: 'terms', component: TermsOfServiceComponent },
  { path: 'privacy', component: PrivacyPolicyComponent },
  { path: 'business', component: BusinessOverviewComponent },
  { path: 'business/create', component: CreateBusinessComponent },

  // New workspace URLs
  { path: 'workspace/overview/:businessCode', component: BusinessWorkspaceComponent },
  { path: 'workspace/employees/:businessCode', component: BusinessEmployeesComponent },
  { path: 'workspace/services/:businessCode', component: BusinessServiceManagerComponent },
  { path: 'workspace/availability/:businessCode', component: BusinessAvailabilityComponent },

  // Backwards-compatible redirects
  { path: 'businesses/:businessCode', redirectTo: '/:businessCode', pathMatch: 'full' },
  { path: 'business/:businessCode/employees', redirectTo: 'workspace/employees/:businessCode', pathMatch: 'full' },
  { path: 'business/:businessCode/services', redirectTo: 'workspace/services/:businessCode', pathMatch: 'full' },
  { path: 'business/:businessCode/availability', redirectTo: 'workspace/availability/:businessCode', pathMatch: 'full' },
  { path: 'business/:businessCode', redirectTo: 'workspace/overview/:businessCode', pathMatch: 'full' },

  { path: 'search', component: ServiceSearchResultsComponent },
  { path: ':businessCode/cart', component: PublicBusinessPageComponent },

  // Public business profile at the root: /{businessCode}
  { matcher: publicBusinessCodeMatcher, component: PublicBusinessPageComponent },
  { path: '**', redirectTo: 'home' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { scrollPositionRestoration: 'top', anchorScrolling: 'enabled' })],
  exports: [RouterModule],
})
export class AppRoutingModule {}
