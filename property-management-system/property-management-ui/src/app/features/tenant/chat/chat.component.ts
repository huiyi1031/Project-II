import { Component, OnInit } from '@angular/core';
import { Chat, Message } from '../../../core/models';
import { ChatService } from '../../../core/services/chat.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  standalone: false,
})
export class ChatComponent implements OnInit {
  chats:        Chat[]    = [];
  messages:     Message[] = [];
  selectedChat: Chat | null = null;
  newMessage    = '';
  showMock      = false;

  constructor(private chatSvc: ChatService, private authSvc: AuthService) {}

  ngOnInit(): void {
    this.chatSvc.getMyChats().subscribe({
      next: (data) => (this.chats = data),
      error: () => {}
    });
  }

  selectChat(chat: Chat): void {
    this.selectedChat = chat;
    this.showMock     = false;
    this.chatSvc.getMessages(chat.chatID).subscribe({
      next: (msgs) => {
        const myId = this.authSvc.getCurrentUser()?.userId;
        this.messages = msgs.map(m => ({ ...m, isOwn: m.senderAccountID === myId }));
      },
      error: () => {}
    });
  }

  selectMockChat(): void {
    this.showMock     = true;
    this.selectedChat = null;
    this.messages     = [];
  }

  sendMessage(): void {
    if (!this.newMessage.trim() || !this.selectedChat) return;
    const content = this.newMessage;
    this.newMessage = '';
    this.chatSvc.sendMessage(this.selectedChat.chatID, content).subscribe({
      next: (msg) => this.messages.push({ ...msg, isOwn: true }),
      error: () => {}
    });
  }
}
