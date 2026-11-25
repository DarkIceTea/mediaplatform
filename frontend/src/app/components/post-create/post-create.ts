import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PostService } from '../../services/post.service';

@Component({
  selector: 'app-post-create',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './post-create.html',
  styleUrl: './post-create.scss'
})
export class PostCreate {
  private postService = inject(PostService);
  private router = inject(Router);

  title = '';
  content = '';
  coverImageUrl = signal<string | null>(null);

  isUploading = signal(false);
  isSubmitting = signal(false);
  errorMessage = '';

  // Handle file selection from the user's device
  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.isUploading.set(true);

    this.postService.uploadImage(file).subscribe({
      next: (res) => {
        this.coverImageUrl.set(res.url); 
        this.isUploading.set(false);
      },
      error: () => {
        this.errorMessage = 'Failed to upload cover image';
        this.isUploading.set(false);
      }
    });
  }

  onSubmit() {
    if (!this.title.trim() || !this.content.trim()) {
      this.errorMessage = 'Title and content are required';
      return;
    }

    this.isSubmitting.set(true);
    this.postService.createPost({
      title: this.title,
      content: this.content,
      coverImageUrl: this.coverImageUrl() || undefined
    }).subscribe({
      next: (res) => {
        this.router.navigate(['/posts', res.id]);
      },
      error: () => {
        this.errorMessage = 'Failed to create post';
        this.isSubmitting.set(false);
      }
    });
  }
}