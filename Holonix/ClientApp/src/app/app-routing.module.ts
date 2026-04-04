import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { HomeComponent } from './features/home/home.component';
import { ProfileComponent } from './features/profile/profile.component';
import { CreateBusinessComponent } from './features/business/create-business/create-business.component';
import { BusinessOverviewComponent } from './features/business/business-overview/business-overview.component';
import { BusinessWorkspaceComponent } from './features/business/business-workspace/business-workspace.component';
import { BusinessEmployeesComponent } from './features/business/business-employees/business-employees.component';
import { BusinessServiceManagerComponent } from './features/business/business-service-manager/business-service-manager.component';

const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'home', component: HomeComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'profile', component: ProfileComponent },
  { path: 'business', component: BusinessOverviewComponent },
  { path: 'business/create', component: CreateBusinessComponent },
  { path: 'business/:businessCode/employees', component: BusinessEmployeesComponent },
  { path: 'business/:businessCode/services', component: BusinessServiceManagerComponent },
  { path: 'business/:businessCode', component: BusinessWorkspaceComponent },
  { path: '**', redirectTo: 'home' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { scrollPositionRestoration: 'top', anchorScrolling: 'enabled' })],
  exports: [RouterModule],
})
export class AppRoutingModule {}
