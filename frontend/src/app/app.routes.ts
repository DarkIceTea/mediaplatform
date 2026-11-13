// src/app/app.routes.ts
import { Routes } from '@angular/router';
import { PostList } from './components/post-list/post-list';
import { PostCreate } from './components/post-create/post-create';
import { PostDetail } from './components/post-detail/post-detail';
import { Login } from './components/login/login';
import { authGuard } from './guards/auth.guard';
import { VideoList } from './components/video-list/video-list';
import { VideoUpload } from './components/video-upload/video-upload';
import { VideoWatch } from './components/video-watch/video-watch';
import { Chat } from './components/chat/chat';

export const routes: Routes = [
  { path: '', redirectTo: 'posts', pathMatch: 'full' },
  { path: 'posts', component: PostList },
  { path: 'posts/create', component: PostCreate, canActivate: [authGuard] },
  { path: 'posts/:id', component: PostDetail },
  { path: 'login', component: Login },
  { path: 'videos', component: VideoList },
  { path: 'videos/upload', component: VideoUpload, canActivate: [authGuard] },
  { path: 'videos/:id', component: VideoWatch },
  { path: 'chat', component: Chat, canActivate: [authGuard] },
];