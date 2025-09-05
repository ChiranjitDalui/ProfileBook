import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api';

function parseJwt(token: string | null): any {
  if (!token) return null;
  try { return JSON.parse(atob(token.split('.')[1])); } catch { return null; }
}

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.html'
})
export class AdminUsersComponent implements OnInit {
  loading = false;
  users: any[] = [];
  filtered: any[] = [];

  q = '';
  role = ''; // '', 'User', 'Admin'

  // edit modal state
  showEdit = false;
  saving = false;
  editForm = { id: 0, username: '', role: 'User', newPassword: '' };

   currentUserId: number = 0;  
   
  constructor(private api: ApiService) {}

  ngOnInit(): void {
    //  extract userId from JWT
    try {
      const token = localStorage.getItem('token');
      if (token) {
        const payload = JSON.parse(atob(token.split('.')[1]));
        this.currentUserId = Number(payload['nameid'] || payload['Id'] || payload['sub'] || 0);
      }
    } catch {
      this.currentUserId = 0;
    }

    // guard + fetch as before
    this.fetch();
  }

  fetch() {
    this.loading = true;
    this.api.getUsers().subscribe({
      next: (res: any[]) => {
        // expected: [{ id, username, role }]
        this.users = res || [];
        this.apply();
        this.loading = false;
      },
      error: (err) => {
        console.error('Load users failed', err);
        this.loading = false;
        alert(err?.status === 403 ? 'Admin permission required.' : 'Could not load users.');
      }
    });
  }

  apply() {
    const q = (this.q || '').toLowerCase();
    const role = this.role;
    this.filtered = this.users.filter(u => {
      const okQ = q ? (u.username || '').toLowerCase().includes(q) || String(u.id).includes(q) : true;
      const okR = role ? (u.role === role) : true;
      return okQ && okR;
    });
  }

  clearFilters() {
    this.q = '';
    this.role = '';
    this.apply();
  }

  openEdit(u: any) {
    this.editForm = {
      id: u.id,
      username: u.username || '',
      role: u.role || 'User',
      newPassword: ''
    };
    this.showEdit = true;
  }

  closeEdit() {
    this.showEdit = false;
    this.editForm = { id: 0, username: '', role: 'User', newPassword: '' };
  }

  saveEdit() {
    if (!this.editForm.username.trim()) {
      alert('Username is required.');
      return;
    }
    this.saving = true;

    // Backend expects PasswordHash field to carry the *raw* new password (if any)
      const payload: any = {
      username: this.editForm.username,
      role: this.editForm.role
};
if (this.editForm.newPassword?.trim()) {
  payload.newPassword = this.editForm.newPassword.trim();
}
this.api.updateUser(this.editForm.id, payload).subscribe({
  next: () => { this.saving = false; this.closeEdit(); this.fetch(); },
  error: (err) => { console.error('Update user failed', err); this.saving = false; alert('Could not update user.'); }
});

  }

  remove(u: any) {
    if (!confirm(`Delete user "${u.username}" and all related data?`)) return;
    this.api.deleteUser(u.id).subscribe({
      next: () => {
        this.users = this.users.filter(x => x.id !== u.id);
        this.apply();
      },
      error: (err) => {
        console.error('Delete user failed', err);
        alert('Could not delete user.');
      }
    });
  }
}
