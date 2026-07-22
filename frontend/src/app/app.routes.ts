import { Routes } from '@angular/router';
import { Login } from './features/login/login';
import { Home } from './features/home/home';
import { Dashboard } from './features/dashboard/dashboard';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'home', component: Home },
  { path: 'dashboard', component: Dashboard },
  { path: '', redirectTo: 'login', pathMatch: 'full' }
];
