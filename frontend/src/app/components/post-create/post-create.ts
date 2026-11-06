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

  // Обработка выбора файла с ПК
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
        this.errorMessage = 'Ошибка загрузки обложки';
        this.isUploading.set(false);
      }
    });
  }

  onSubmit() {
    if (!this.title.trim() || !this.content.trim()) {
      this.errorMessage = 'Заголовок и текст статьи обязательны';
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
        this.errorMessage = 'Ошибка при создании статьи';
        this.isSubmitting.set(false);
      }
    });
  }
}