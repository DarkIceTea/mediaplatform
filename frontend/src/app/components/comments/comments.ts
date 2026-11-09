import { Component, inject, input, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CommentService } from '../../services/comment.service';
import { AuthService } from '../../services/auth.service';
import { CommentItem } from '../../models/comment.models';

@Component({
  selector: 'app-comments',
  standalone: true,
  imports: [FormsModule, DatePipe, RouterLink],
  templateUrl: './comments.html',
  styleUrl: './comments.scss'
})
export class CommentsComponent implements OnInit {
  postId = input.required<string>();

  private commentService = inject(CommentService);
  authService = inject(AuthService);

  comments = signal<CommentItem[]>([]);
  newCommentText = '';
  
  replyToCommentId = signal<string | null>(null);
  replyText = '';

  ngOnInit() {
    this.loadComments();
  }

  loadComments() {
    this.commentService.getCommentsByPostId(this.postId()).subscribe({
      next: (data) => this.comments.set(data),
      error: (err) => console.error('Ошибка загрузки комментариев', err)
    });
  }

  onAddComment() {
    if (!this.newCommentText.trim()) return;

    this.commentService.createComment({
      text: this.newCommentText,
      postId: this.postId()
    }).subscribe({
      next: (created) => {
        this.comments.update(list => [created, ...list]);
        this.newCommentText = '';
      }
    });
  }

  onAddReply(parentComment: CommentItem) {
    if (!this.replyText.trim()) return;

    this.commentService.createComment({
      text: this.replyText,
      postId: this.postId(),
      parentCommentId: parentComment.id
    }).subscribe({
      next: (created) => {
        this.comments.update(list => 
          list.map(c => {
            if (c.id === parentComment.id) {
              return { ...c, replies: [...c.replies, created] };
            }
            return c;
          })
        );
        this.replyToCommentId.set(null);
        this.replyText = '';
      }
    });
  }
}