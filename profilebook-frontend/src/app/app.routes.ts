import { Routes } from '@angular/router';
import { LoginComponent } from './user/login/login';
import { RegisterComponent } from './user/register/register';
import { PostsComponent } from './user/posts/posts';
import { AdminReportsComponent } from './admin/reports/reports/reports';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'posts', component: PostsComponent },
  { path: 'admin/reports', component: AdminReportsComponent },
  
  // redirect plain /admin to reports
  { path: 'admin', redirectTo: 'admin/reports', pathMatch: 'full' },

  // (optional) default and 404 fallbacks
  { path: '', redirectTo: 'posts', pathMatch: 'full' },
  { path: '**', redirectTo: 'posts' }
];
