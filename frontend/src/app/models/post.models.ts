export interface PostAuthor {
  id: string;
  userName: string;
  displayName: string;
}

export interface Post {
  id: string;
  title: string;
  content: string;
  coverImageUrl?: string;
  createdAt: string;
  author: PostAuthor;
  likesCount: number;
  isLiked: boolean;
}

export interface CreatePostRequest {
  title: string;
  content: string;
  coverImageUrl?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}