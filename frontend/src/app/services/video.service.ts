// src/app/services/video.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEvent, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/post.models';
import { VideoItem } from '../models/video.models';

@Injectable({
  providedIn: 'root'
})
export class VideoService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5180/api/videos'; 

  getVideos(page: number = 1, pageSize: number = 12) {
    return this.http.get<PagedResult<VideoItem>>(`${this.apiUrl}?page=${page}&pageSize=${pageSize}`);
  }

  getVideoById(id: string) {
    return this.http.get<VideoItem>(`${this.apiUrl}/${id}`);
  }

  uploadVideo(formData: FormData): Observable<HttpEvent<any>> {
    const req = new HttpRequest('POST', `${this.apiUrl}/upload`, formData, {
      reportProgress: true
    });
    return this.http.request(req);
  }
}