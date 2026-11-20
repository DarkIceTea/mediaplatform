import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DatePipe } from '@angular/common';
import { VideoService } from '../../services/video.service';
import { Comments } from '../comments/comments';
import { VideoItem } from '../../models/video.models';
import { AuthService } from '../../services/auth.service';


@Component({
  selector: 'app-video-watch',
  standalone: true,
  imports: [DatePipe, Comments],
  templateUrl: './video-watch.html',
  styleUrl: './video-watch.scss'
})
export class VideoWatch implements OnInit {
  private route = inject(ActivatedRoute);
  private videoService = inject(VideoService);
  authService = inject(AuthService);

  video = signal<VideoItem | null>(null);
  isLoading = signal(true);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.videoService.getVideoById(id).subscribe({
        next: (data) => {
          this.video.set(data);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      });
    }
  }

   onToggleLike() {
    const currentVideo = this.video();
    if (!currentVideo || !this.authService.isAuthenticated()) return;

    const previousState = { ...currentVideo };

    const newIsLiked = !currentVideo.isLiked;
    const newCount = newIsLiked ? currentVideo.likesCount + 1 : currentVideo.likesCount - 1;

    this.video.set({
      ...currentVideo,
      isLiked: newIsLiked,
      likesCount: Math.max(0, newCount)
    });

    this.videoService.toggleLike(currentVideo.id).subscribe({
      next: (response) => {
        this.video.update(v => v ? { ...v, isLiked: response.isLiked, likesCount: response.likesCount } : null);
      },
      error: (err) => {
        console.error('Like error, rollback...', err);
        this.video.set(previousState);
        alert('Like error, check your internet connection');
      }
    });
  }
}