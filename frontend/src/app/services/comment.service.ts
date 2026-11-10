// src/app/services/comment.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommentItem, CreateCommentRequest } from '../models/comment.models';

@Injectable({
  providedIn: 'root'
})
export class CommentService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5000/api/comments'; 

  getCommentsByPostId(postId: string) {
    return this.http.get<CommentItem[]>(`${this.apiUrl}?postId=${postId}`);
  }

  getComments(target: { postId?: string; videoId?: string }) {
    const param = target.postId ? `postId=${target.postId}` : `videoId=${target.videoId}`;
    return this.http.get<CommentItem[]>(`${this.apiUrl}?${param}`);
  }

  createComment(request: CreateCommentRequest) {
    return this.http.post<CommentItem>(this.apiUrl, request);
  }
}