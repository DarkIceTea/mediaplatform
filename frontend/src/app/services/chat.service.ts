import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { ChatMessage, ChatUser } from '../models/chat.models';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private http = inject(HttpClient);
  
  private gatewayUrl = 'http://localhost:5000';

  private hubConnection: signalR.HubConnection | null = null;

  messages = signal<ChatMessage[]>([]);
  activeUser = signal<ChatUser | null>(null);

  startConnection() {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.gatewayUrl}/hubs/chat`, {
        withCredentials: true 
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start()
      .then(() => console.log('SignalR успешно подключен через Gateway!'))
      .catch(err => console.error('Ошибка подключения сокетов:', err));

    this.hubConnection.on('ReceiveMessage', (message: ChatMessage) => {
      if (this.activeUser()?.id === message.senderId) {
        this.messages.update(list => [...list, message]);
      }
    });

    this.hubConnection.on('MessageSent', (message: ChatMessage) => {
      this.messages.update(list => [...list, message]);
    });
  }

  stopConnection() {
    this.hubConnection?.stop();
    this.hubConnection = null;
  }

  getUsers() {
    return this.http.get<ChatUser[]>(`${this.gatewayUrl}/api/chats/users`);
  }

  loadHistory(otherUserId: string) {
    return this.http.get<ChatMessage[]>(`${this.gatewayUrl}/api/chats/${otherUserId}/messages`).subscribe({
      next: (data) => this.messages.set(data),
      error: (err) => console.error('Ошибка загрузки истории переписки', err)
    });
  }

  sendMessage(receiverId: string, content: string) {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      console.error('Соединение с чатом не готово');
      return;
    }

    this.hubConnection.invoke('SendMessage', receiverId, content)
      .catch(err => console.error('Ошибка отправки сообщения:', err));
  }
}