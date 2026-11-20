import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { PostService } from '../../services/post.service';
import { Post } from '../../models/post.models';
import { Comments } from '../comments/comments';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-post-detail',
  standalone: true,
  imports: [DatePipe, RouterLink, Comments],
  templateUrl: './post-detail.html',
  styleUrl: './post-detail.scss'
})
export class PostDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private postService = inject(PostService);
  authService = inject(AuthService);

  post = signal<Post | null>(null);
  isLoading = signal(true);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.postService.getPostById(id).subscribe({
        next: (data) => {
          this.post.set(data);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      });
    }
  }

  onToggleLike() {
    const currentPost = this.post();
    if (!currentPost || !this.authService.isAuthenticated()) return;

    const previousState = { ...currentPost };

    const newIsLiked = !currentPost.isLiked;
    const newCount = newIsLiked ? currentPost.likesCount + 1 : currentPost.likesCount - 1;

    this.post.set({
      ...currentPost,
      isLiked: newIsLiked,
      likesCount: Math.max(0, newCount)
    });

    this.postService.toggleLike(currentPost.id).subscribe({
      next: (response) => {
        this.post.update(p => p ? { ...p, isLiked: response.isLiked, likesCount: response.likesCount } : null);
      },
      error: (err) => {
        console.error('Like error, interface rollback...', err);
        this.post.set(previousState);
        alert('Like error, check internet connection');
      }
    });
  }
}