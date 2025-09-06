import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ApiService } from '../../services/api';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './notifications.html',
  styleUrls: ['./notifications.css']
})
export class NotificationsComponent implements OnInit {
  open = false;
  loading = false;
  notifications: Array<{
    id: number;
    userId: number;
    message: string;
    isRead: boolean;
    createdAt: string; // or Date
  }> = [];

  unreadCount = 0;
  refreshing = false;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.load();
    // optional: poll every X seconds if you want new notifications without refresh
    // setInterval(() => this.load(false), 30000);
  }

  toggle() {
    this.open = !this.open;
    if (this.open) this.load();
  }

  load(showSpinner = true) {
    if (showSpinner) this.loading = true;
    this.api.getNotifications().subscribe({
      next: (res: any[]) => {
        // expect backend to return notifications with fields: id, userId, message, isRead, createdAt
        this.notifications = res || [];
        this.unreadCount = this.notifications.filter(n => !n.isRead).length;
        this.loading = false;
      },
      error: (err) => {
        console.error('Load notifications failed', err);
        this.loading = false;
      }
    });
  }

  markRead(n: any) {
    if (n.isRead) return;
    this.api.markNotificationRead(n.id).subscribe({
      next: () => {
        n.isRead = true;
        this.unreadCount = Math.max(0, this.unreadCount - 1);
      },
      error: (err) => {
        console.error('Could not mark read', err);
      }
    });
  }

  markAllRead() {
    // mark each unread one-by-one (backend has single endpoint) then refresh
    const unread = this.notifications.filter(n => !n.isRead);
    if (!unread.length) return;

    this.refreshing = true;
    // sequentially call or fire in parallel and wait
    const calls = unread.map(n => this.api.markNotificationRead(n.id).toPromise());
    Promise.all(calls)
      .then(() => {
        // update UI locally rather than reloading
        this.notifications.forEach(n => n.isRead = true);
        this.unreadCount = 0;
        this.refreshing = false;
        // keep the bell visible; do not hide component
      })
      .catch((err) => {
        console.error('Mark all read failed', err);
        this.refreshing = false;
      });
  }

  // convenience for template time display
  timeAgo(iso: string) {
    try {
      const d = new Date(iso);
      const diff = Math.floor((Date.now() - d.getTime()) / 1000);
      if (diff < 60) return `${diff}s`;
      if (diff < 3600) return `${Math.floor(diff/60)}m`;
      if (diff < 86400) return `${Math.floor(diff/3600)}h`;
      return `${Math.floor(diff/86400)}d`;
    } catch {
      return '';
    }
  }
}
