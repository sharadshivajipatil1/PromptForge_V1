import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { GuestLoginComponent } from './features/guest/guest-login/guest-login.component';
import { GuestDashboardComponent } from './features/guest/guest-dashboard/guest-dashboard.component';
import { GuestCheckInComponent } from './features/guest/guest-check-in/guest-check-in.component';
import { GuestExperienceComponent } from './features/guest/guest-experience/guest-experience.component';
import { GuestChatComponent } from './features/guest/guest-chat/guest-chat.component';
import { StaffLoginComponent } from './features/staff/staff-login/staff-login.component';
import { StaffTasksComponent } from './features/staff/staff-tasks/staff-tasks.component';
import { StaffForecastComponent } from './features/staff/staff-forecast/staff-forecast.component';

export const routes: Routes = [
  { path: '', redirectTo: '/guest/login', pathMatch: 'full' },
  { path: 'guest/login', component: GuestLoginComponent },
  {
    path: 'guest/dashboard',
    component: GuestDashboardComponent,
    canActivate: [authGuard],
    data: { roles: ['Guest'] }
  },
  {
    path: 'guest/check-in',
    component: GuestCheckInComponent,
    canActivate: [authGuard],
    data: { roles: ['Guest'] }
  },
  {
    path: 'guest/experience',
    component: GuestExperienceComponent,
    canActivate: [authGuard],
    data: { roles: ['Guest'] }
  },
  {
    path: 'guest/chat',
    component: GuestChatComponent,
    canActivate: [authGuard],
    data: { roles: ['Guest'] }
  },
  { path: 'staff/login', component: StaffLoginComponent },
  {
    path: 'staff/tasks',
    component: StaffTasksComponent,
    canActivate: [authGuard],
    data: { roles: ['Staff'] }
  },
  {
    path: 'staff/forecast',
    component: StaffForecastComponent,
    canActivate: [authGuard],
    data: { roles: ['Staff'] }
  },
  { path: '**', redirectTo: '/guest/login' }
];
