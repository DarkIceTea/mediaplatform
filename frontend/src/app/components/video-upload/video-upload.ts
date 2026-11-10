import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpEventType } from '@angular/common/http';
import { VideoService } from '../../services/video.service';

@Component({
  selector: 'app-video-upload',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './video-upload.html',
  styleUrl: './video-upload.scss'
})
export class VideoUpload {
  private videoService = inject(VideoService);
  private router = inject(Router);

  title = '';
  description = '';
  selectedFile: File | null = null;

  isUploading = signal(false);
  uploadProgress = signal<number>(0);
  isProcessing = signal(false);
  errorMessage = '';

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
    }
  }

  onSubmit() {
    if (!this.selectedFile || !this.title.trim()) {
      this.errorMessage = 'Заполните название и выберите видеофайл';
      return;
    }

    this.isUploading.set(true);
    this.uploadProgress.set(0);
    this.errorMessage = '';

    const formData = new FormData();
    formData.append('title', this.title);
    if (this.description) formData.append('description', this.description);
    formData.append('file', this.selectedFile);

    this.videoService.uploadVideo(formData).subscribe({
      next: (event) => {
        // 1. Отслеживаем передачу байтов
        if (event.type === HttpEventType.UploadProgress && event.total) {
          const percent = Math.round((100 * event.loaded) / event.total);
          this.uploadProgress.set(percent);
        } 
        // 2. Файл полностью ушел на бэкенд, сервер начал обработку
        else if (event.type === HttpEventType.Response) {
          this.isUploading.set(false);
          this.isProcessing.set(true);
          
          // Даем воркеру пару секунд на нарезку кадра и возвращаем на главную
          setTimeout(() => {
            this.router.navigate(['/videos']);
          }, 3000);
        }
      },
      error: (err) => {
        console.error(err);
        this.errorMessage = err.error || 'Ошибка при загрузке видео';
        this.isUploading.set(false);
      }
    });
  }
}