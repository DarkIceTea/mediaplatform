// src/app/services/post.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CreatePostRequest, PagedResult, Post } from '../models/post.models';

@Injectable({
  providedIn: 'root'
})
export class PostService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5180/api';

  getPosts(page: number = 1, pageSize: number = 10) {
    return this.http.get<PagedResult<Post>>(`${this.apiUrl}/posts?page=${page}&pageSize=${pageSize}`);
  }

  getPostById(id: string) {
    return this.http.get<Post>(`${this.apiUrl}/posts/${id}`);
  }

  createPost(data: CreatePostRequest) {
    return this.http.post<{ id: string }>(`${this.apiUrl}/posts`, data);
  }

  uploadImage(file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ url: string }>(`${this.apiUrl}/media/upload-image`, formData);
  }
}