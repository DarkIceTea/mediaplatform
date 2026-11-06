// src/app/app.routes.ts
import { Routes } from '@angular/router';
import { PostList } from './components/post-list/post-list';
import { PostCreate } from './components/post-create/post-create';
import { PostDetail } from './components/post-detail/post-detail';
import { Login } from './components/login/login';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'posts', pathMatch: 'full' },
  { path: 'posts', component: PostList },
  { path: 'posts/create', component: PostCreate, canActivate: [authGuard] },
  { path: 'posts/:id', component: PostDetail },
  { path: 'login', component: Login }
];