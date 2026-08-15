import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Chat, Message, ChatParticipant } from '../models';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private base = 'http://localhost:5004/api/Chats';

  constructor(private http: HttpClient) {}

  getMyChats(): Observable<Chat[]> {
    return this.http.get<Chat[]>(`${this.base}/my`);
  }

  getChatById(chatId: number): Observable<Chat> {
    return this.http.get<Chat>(`${this.base}/${chatId}`);
  }

  getMessages(chatId: number): Observable<Message[]> {
    return this.http.get<Message[]>(`${this.base}/${chatId}/messages`);
  }

  sendMessage(chatId: number, content: string, file?: File): Observable<Message> {
    const formData = new FormData();
    formData.append('Content', content);
    if (file) {
      formData.append('Attachment', file);
    }
    return this.http.post<Message>(`${this.base}/${chatId}/messages`, formData);
  }

  getParticipants(chatId: number): Observable<ChatParticipant[]> {
    return this.http.get<ChatParticipant[]>(`${this.base}/${chatId}/participants`);
  }

  getAvailableUsers(chatId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/${chatId}/available-users`);
  }

  addParticipant(chatId: number, accountId: number): Observable<void> {
    return this.http.post<void>(`${this.base}/${chatId}/participants`, { accountId });
  }
}
