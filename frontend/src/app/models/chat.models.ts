export interface ChatUser {
  id: string;
  userName: string;
  displayName: string;
}

export interface ChatMessage {
  id: string;
  senderId: string;
  senderName: string;
  receiverId: string;
  content: string;
  sentAt: string;
}