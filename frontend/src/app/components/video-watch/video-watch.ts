import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DatePipe } from '@angular/common';
import { VideoService } from '../../services/video.service';
import { Comments } from '../comments/comments';
import { VideoItem } from '../../models/video.models';

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
}