import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api';

function parseJwt(token: string | null): any {
  if (!token) return null;
  try {
    const payload = token.split('.')[1];
    return JSON.parse(atob(payload));
  } catch {
    return null;
  }
}

@Component({
  selector: 'app-admin-posts-approve',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './posts-approve.html'
})
export class AdminPostsApproveComponent implements OnInit {
  loading = false;
  posts: any[] = [];
  filtered: any[] = [];
  q = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    // light-weight guard
    const tok = localStorage.getItem('token');
    const p = parseJwt(tok);
    const roles = (p?.role || p?.roles || '').toString();
    if (!roles.includes('Admin')) {
      alert('Admin only. Redirecting to posts.');
      window.location.href = '/posts';
      return;
    }

    this.fetch();
  }

  fetch() {
    this.loading = true;
    this.api.getPendingPosts().subscribe({
      next: (res: any[]) => {
        //filter client side because backend returns all posts
        const pending = (res || []).filter(p => (p.status || p.Status) === 'Pending');

        this.posts = (res || []).map(p => ({
          ...p,
          likeCount: typeof p.likes === 'number'
            ? p.likes
            : Array.isArray(p.likes) ? p.likes.length
            : (p.likeCount ?? 0)
        }));
        this.apply();
        this.loading = false;
      },
      error: (err) => {
        console.error('Load pending posts failed', err);
        this.loading = false;
        alert(err?.status === 403 ? 'Admin permission required.' : 'Could not load pending posts.');
      }
    });
  }

  normalizePath(path: string | null | undefined) {
    return (path || '').replace(/\\/g, '/');
  }

  apply() {
    const q = (this.q || '').toLowerCase();
    this.filtered = this.posts.filter(p =>
      (p.content || '').toLowerCase().includes(q) ||
      (p.user?.username || p.username || '').toLowerCase().includes(q) ||
      String(p.userId || '').includes(q)
    );
  }

  clear() {
    this.q = '';
    this.apply();
  }

  approve(p: any) {
    if (!confirm('Approve this post?')) return;
    this.api.approvePost(p.id).subscribe({
      next: () => {
        this.posts = this.posts.filter(x => x.id !== p.id);
        this.apply();
      },
      error: (err) => {
        console.error('Approve failed', err);
        alert('Could not approve.');
      }
    });
  }

  reject(p: any) {
    if (!confirm('Reject (remove) this post?')) return;
    this.api.rejectPost(p.id).subscribe({
      next: () => {
        this.posts = this.posts.filter(x => x.id !== p.id);
        this.apply();
      },
      error: (err) => {
        console.error('Reject failed', err);
        alert('Could not reject.');
      }
    });
  }
}
