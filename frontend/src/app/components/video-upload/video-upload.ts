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
      this.errorMessage = 'Enter a title and select a video file';
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
        // 1. Track upload progress (bytes sent)
        if (event.type === HttpEventType.UploadProgress && event.total) {
          const percent = Math.round((100 * event.loaded) / event.total);
          this.uploadProgress.set(percent);
        } 
        // 2. File fully sent to the backend; the server has started processing
        else if (event.type === HttpEventType.Response) {
          this.isUploading.set(false);
          this.isProcessing.set(true);
          
          // Give the worker a couple of seconds to extract the thumbnail, then return to the feed
          setTimeout(() => {
            this.router.navigate(['/videos']);
          }, 3000);
        }
      },
      error: (err) => {
        console.error(err);
        this.errorMessage = err.error || 'Failed to upload video';
        this.isUploading.set(false);
      }
    });
  }
}