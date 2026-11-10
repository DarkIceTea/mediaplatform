// src/app/models/video.models.ts
import { PostAuthor } from './post.models';

export interface VideoItem {
  id: string;
  title: string;
  description?: string;
  videoUrl: string;
  thumbnailUrl?: string;
  durationSeconds: number;
  viewsCount: number;
  createdAt: string;
  author: PostAuthor;
}