import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, SlicePipe } from '@angular/common';
import { PostService } from '../../services/post.service';
import { Post } from '../../models/post.models';


@Component({
  selector: 'app-post-list',
  standalone: true,
  imports: [RouterLink, DatePipe, SlicePipe],
  templateUrl: './post-list.html',
  styleUrl: './post-list.scss'
})
export class PostList implements OnInit {
  private postService = inject(PostService);

  posts = signal<Post[]>([]);
  isLoading = signal(true);

  ngOnInit() {
    this.loadPosts();
  }

  loadPosts() {
    this.isLoading.set(true);
    this.postService.getPosts().subscribe({
      next: (res) => {
        this.posts.set(res.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }
}