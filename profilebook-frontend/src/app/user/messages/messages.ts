// standalone component (Angular 15+ / likely v20)
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MessageService } from '../../services/message';
import { ApiService } from '../../services/api';

@Component({
  selector: 'app-messages',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './messages.html',
  styleUrls: ['./messages.css']
})
export class MessagesComponent implements OnInit {
  private messageService = inject(MessageService);
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  otherUsername: string | null = null;     // optional: conversation by username
  otherUserId: number | null = null;       // optional: conversation by id
  messages: any[] = [];
  newMessage = '';
  loading = false;
  pollingInterval: any = null;
  currentUsername: string | null | undefined;

  ngOnInit(): void {
    // read current username (try localStorage first)
  this.currentUsername = localStorage.getItem('username');

  // fallback: call API to get profile if not in localStorage
  if (!this.currentUsername) {
    this.api.getMyProfile().subscribe({
      next: (p: any) => { this.currentUsername = p?.username || p?.userName || p?.fullName || null; },
      error: () => { /* ignore */ }
    });
  }

  // existing route param subscriptions...
  this.route.paramMap.subscribe(pm => {
    const username = pm.get('username');
    if (username) {
      this.otherUsername = username;
      this.loadByUsername(username);
    }
  });

    // also support query param ?userId=#
    this.route.queryParamMap.subscribe(qp => {
      const id = qp.get('userId');
      if (id) {
        this.otherUserId = Number(id);
        this.loadByUserId(this.otherUserId);
      }
    });

    // if no param, we can optionally show a list of users or instructions
  }

  ngOnDestroy(): void {
    if (this.pollingInterval) clearInterval(this.pollingInterval);
  }

  loadByUsername(username: string) {
    this.loading = true;
    this.messageService.getMessagesWithUsername(username).subscribe({
      next: (res: any) => {
        this.messages = res;
        this.loading = false;
        // optional: poll every 4s for updates
        this.startPolling(() => this.messageService.getMessagesWithUsername(username));
      },
      error: err => {
        console.error('Load messages error', err);
        this.loading = false;
      }
    });
  }

  loadByUserId(userId: number) {
    this.loading = true;
    this.messageService.getMessagesWithId(userId).subscribe({
      next: (res: any) => {
        this.messages = res;
        this.loading = false;
        this.startPolling(() => this.messageService.getMessagesWithId(userId));
      },
      error: err => {
        console.error('Load messages error', err);
        this.loading = false;
      }
    });
  }

  // send message (will use route param priority)
sendMessage() {
  if (!this.newMessage?.trim()) return;
  const text = this.newMessage.trim();

  // prevent sending to self (frontend guard)
  if (this.otherUsername && this.currentUsername && this.otherUsername.toLowerCase() === this.currentUsername.toLowerCase()) {
    alert("You can't send a message to yourself.");
    return;
  }

  if (this.otherUsername) {
    this.messageService.sendToUsername(this.otherUsername, text).subscribe({
      next: res => {
        // optimistic push & reload
        this.messages.push({
          messageContent: text,
          timeStamp: new Date().toISOString(),
          Sender: 'You',
          Receiver: this.otherUsername
        });
        this.newMessage = '';
        if (this.otherUsername) this.loadByUsername(this.otherUsername);
      },
      error: err => {
        console.error('Send by username error', err);
        // Friendly message for server-side self-send rejection
        if (err?.status === 400 && err?.error) {
          // server returned BadRequest("You cannot message yourself.")
          const serverMessage = (typeof err.error === 'string') ? err.error : (err.error?.title || err.error?.message || JSON.stringify(err.error));
          alert(serverMessage || 'Could not send message.');
        } else {
          alert('Could not send message. Please try again.');
        }
      }
    });
    return;
  }

    if (this.otherUserId) {
      this.messageService.sendToId(this.otherUserId, text).subscribe({
        next: res => {
          this.messages.push({
            messageContent: text,
            timeStamp: new Date().toISOString(),
            Sender: 'You',
            Receiver: this.otherUserId
          });
          this.newMessage = '';
          if (this.otherUserId) this.loadByUserId(this.otherUserId);
        },
        error: err => {
          console.error('Send by id error', err);
        }
      });
      return;
    }

    // No target: redirect or show a message
    alert('No recipient selected. Open a conversation from user profile or navigate to /messages?userId=XX or /messages/username');
  }

  startPolling(getter: () => any) {
    if (this.pollingInterval) clearInterval(this.pollingInterval);
    this.pollingInterval = setInterval(() => {
      getter().subscribe({
        next: (res: any) => { this.messages = res; },
        error: () => {} // ignore occasional errors
      });
    }, 4000);
  }

  // helper for opening a conversation by username programmatically (used from navbar)
  openConversationForUsername(username: string) {
    this.router.navigate(['/messages', username]);
  }
}
