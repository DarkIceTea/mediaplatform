import { PostAuthor } from './post.models';

export interface CommentItem {
  id: string;
  text: string;
  createdAt: string;
  author: PostAuthor;
  parentCommentId?: string;
  replies: CommentItem[];
}

export interface CreateCommentRequest {
  text: string;
  postId?: string;
  parentCommentId?: string;
}