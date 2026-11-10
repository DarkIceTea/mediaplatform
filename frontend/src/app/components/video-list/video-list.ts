import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { VideoService } from '../../services/video.service';
import { VideoItem } from '../../models/video.models';

@Component({
  selector: 'app-video-list',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './video-list.html',
  styleUrl: './video-list.scss'
})
export class VideoList implements OnInit {
  private videoService = inject(VideoService);

  videos = signal<VideoItem[]>([]);
  isLoading = signal(true);

  ngOnInit() {
    this.videoService.getVideos().subscribe({
      next: (res) => {
        this.videos.set(res.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  formatDuration(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs < 10 ? '0' : ''}${secs}`;
  }
}