// src/app/components/chat/chat.component.ts
import { Component, inject, OnInit, OnDestroy, signal, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ChatService } from '../../services/chat.service';
import { AuthService } from '../../services/auth.service';
import { ChatUser } from '../../models/chat.models';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './chat.html',
  styleUrl: './chat.scss'
})
export class Chat implements OnInit, OnDestroy, AfterViewChecked {
  chatService = inject(ChatService);
  authService = inject(AuthService);

  users = signal<ChatUser[]>([]);
  messageText = '';

  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;

  ngOnInit() {
    this.chatService.startConnection();
    this.loadUsers();
  }

  ngOnDestroy() {
    this.chatService.stopConnection();
  }

  loadUsers() {
    this.chatService.getUsers().subscribe({
      next: (list) => this.users.set(list),
      error: (err) => console.error('Ошибка загрузки контактов', err)
    });
  }

  selectUser(user: ChatUser) {
    this.chatService.activeUser.set(user);
    this.chatService.loadHistory(user.id);
  }

  onSend() {
    const active = this.chatService.activeUser();
    if (!active || !this.messageText.trim()) return;

    this.chatService.sendMessage(active.id, this.messageText);
    this.messageText = '';
  }

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  private scrollToBottom(): void {
    try {
      if (this.messagesContainer) {
        this.messagesContainer.nativeElement.scrollTop = this.messagesContainer.nativeElement.scrollHeight;
      }
    } catch {}
  }
}