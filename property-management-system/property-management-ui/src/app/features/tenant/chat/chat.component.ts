import { Component, OnInit, OnDestroy } from '@angular/core';
import { Chat, Message } from '../../../core/models';
import { ChatService } from '../../../core/services/chat.service';
import { AuthService } from '../../../core/services/auth.service';
import { MaintenanceService } from '../../../core/services/maintenance.service';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  standalone: false,
})
export class ChatComponent implements OnInit, OnDestroy {
  chats:        Chat[]    = [];
  messages:     Message[] = [];
  participants: any[]     = [];
  selectedChat: Chat | null = null;
  newMessage    = '';
  showMock      = false;
  
  showScheduleModal = false;
  scheduleDate = '';
  selectedFile: File | null = null;
  
  isAdmin = false;
  showAddMemberModal = false;
  availableUsers: any[] = [];
  private pollInterval: any;

  constructor(
    private chatSvc: ChatService, 
    private authSvc: AuthService,
    private maintenanceSvc: MaintenanceService
  ) {}

  ngOnInit(): void {
    this.chatSvc.getMyChats().subscribe({
      next: (data) => (this.chats = data),
      error: () => {}
    });
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  stopPolling(): void {
    if (this.pollInterval) {
      clearInterval(this.pollInterval);
      this.pollInterval = null;
    }
  }

  startPolling(chatId: number): void {
    this.stopPolling();
    this.pollInterval = setInterval(() => {
      this.chatSvc.getMessages(chatId).subscribe({
        next: (msgs) => {
          if (msgs.length !== this.messages.length) {
            const myId = this.authSvc.getCurrentUser()?.userId;
            this.messages = msgs.map(m => ({ ...m, isOwn: m.senderAccountID === myId }));
          }
        },
        error: () => {}
      });
    }, 3000);
  }

  selectChat(chat: Chat): void {
    this.selectedChat = chat;
    this.showMock     = false;
    this.messages     = [];
    this.participants = [];
    
    this.chatSvc.getMessages(chat.chatID).subscribe({
      next: (msgs) => {
        const myId = this.authSvc.getCurrentUser()?.userId;
        this.messages = msgs.map(m => ({ ...m, isOwn: m.senderAccountID === myId }));
        this.startPolling(chat.chatID);
      },
      error: () => {}
    });

    this.chatSvc.getParticipants(chat.chatID).subscribe({
      next: (parts) => {
        this.participants = parts;
        const myId = this.authSvc.getCurrentUser()?.userId;
        
        // FIX: handle API casing issue (accountID vs accountId)
        const me = parts.find(p => (p.accountID || (p as any).accountId) === myId);
        this.isAdmin = me?.isAdmin || false;
      },
      error: () => {}
    });
  }

  selectMockChat(): void {
    this.showMock     = true;
    this.selectedChat = null;
    this.messages     = [];
    this.participants = [];
    this.stopPolling();
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
    }
  }

  removeAttachment(): void {
    this.selectedFile = null;
  }

  isImage(url: string | undefined): boolean {
    if (!url) return false;
    const lower = url.toLowerCase();
    return lower.endsWith('.jpg') || lower.endsWith('.jpeg') || lower.endsWith('.png');
  }

  sendMessage(): void {
    if ((!this.newMessage.trim() && !this.selectedFile) || !this.selectedChat) return;
    
    const content = this.newMessage;
    const file = this.selectedFile || undefined;
    
    this.newMessage = '';
    this.selectedFile = null;
    
    this.chatSvc.sendMessage(this.selectedChat.chatID, content, file).subscribe({
      next: (msg) => this.messages.push({ ...msg, isOwn: true }),
      error: () => {}
    });
  }

  openScheduleModal(): void {
    this.scheduleDate = '';
    this.showScheduleModal = true;
  }

  closeScheduleModal(): void {
    this.showScheduleModal = false;
  }

  confirmSchedule(): void {
    if (!this.selectedChat || !this.scheduleDate) return;
    this.maintenanceSvc.scheduleRequest(this.selectedChat.requestID, new Date(this.scheduleDate).toISOString()).subscribe({
      next: () => {
        this.selectedChat!.requestStatus = 'Scheduled';
        this.closeScheduleModal();
        // Send a system message or user message about the schedule
        this.chatSvc.sendMessage(this.selectedChat!.chatID, `Maintenance scheduled for: ${new Date(this.scheduleDate).toLocaleString()}`).subscribe({
          next: (msg) => this.messages.push({ ...msg, isOwn: true }),
          error: () => {}
        });
      },
      error: (err) => console.error('Failed to schedule:', err)
    });
  }

  openAddMemberModal(): void {
    if (!this.selectedChat || !this.isAdmin) return;
    this.showAddMemberModal = true;
    this.chatSvc.getAvailableUsers(this.selectedChat.chatID).subscribe({
      next: (users) => this.availableUsers = users,
      error: (err) => console.error(err)
    });
  }

  closeAddMemberModal(): void {
    this.showAddMemberModal = false;
    this.availableUsers = [];
  }

  addMember(accountId: number): void {
    if (!this.selectedChat) return;
    this.chatSvc.addParticipant(this.selectedChat.chatID, accountId).subscribe({
      next: () => {
        // Refresh participants
        this.chatSvc.getParticipants(this.selectedChat!.chatID).subscribe({
          next: (parts) => this.participants = parts,
          error: (err) => console.error(err)
        });
        this.closeAddMemberModal();
      },
      error: (err) => console.error(err)
    });
  }
}
