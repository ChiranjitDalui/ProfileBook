import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api';

@Component({
  selector: 'app-admin-groups',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './groups.html',
  styleUrls: ['./groups.css']
})
export class AdminGroupsComponent implements OnInit {
[x: string]: any;
  loading = false;
  creating = false;

  // Add-member UI state
  addingForGroupId: number | null = null;
  addUserId: number | null = null;
  addingBusy = false;

  // search/suggestions
  memberSearch = '';
  memberResults: any[] = [];
  searching = false;
  memberSearchTimeout: any = null;

  // groups data
  groups: Array<{
    id: number;
    groupName: string;
    members: Array<{ userId: number; username: string }>;
  }> = [];

  newGroupName = '';

  // remove busy state per "groupId:userId"
  removingBusy: Record<string, boolean> = {};

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.fetch();
  }

  fetch() {
    this.loading = true;
    this.api.getGroups().subscribe({
      next: (res: any[]) => {
        this.groups = res ?? [];
        this.loading = false;
      },
      error: (err) => {
        console.error('Load groups failed', err);
        alert('Could not load groups.');
        this.loading = false;
      }
    });
  }

  createGroup() {
    const name = (this.newGroupName || '').trim();
    if (!name) return;
    this.creating = true;
    this.api.createGroup(name).subscribe({
      next: () => {
        this.newGroupName = '';
        this.creating = false;
        this.fetch();
        alert('Group created.');
      },
      error: (err) => {
        console.error('Create group failed', err);
        alert(err?.error?.message ?? 'Could not create group.');
        this.creating = false;
      }
    });
  }

  // start showing the add-member row for a group
  startAddMember(groupId: number) {
    this.addingForGroupId = groupId;
    this.memberSearch = '';
    this.memberResults = [];
    this.addUserId = null;
    if (this.memberSearchTimeout) { clearTimeout(this.memberSearchTimeout); this.memberSearchTimeout = null; }
  }

  cancelAddMember() {
    this.addingForGroupId = null;
    this.memberSearch = '';
    this.memberResults = [];
    this.addUserId = null;
    if (this.memberSearchTimeout) { clearTimeout(this.memberSearchTimeout); this.memberSearchTimeout = null; }
  }

  // debounced search called on input
  onMemberSearch() {
    if (this.memberSearchTimeout) clearTimeout(this.memberSearchTimeout);
    const q = (this.memberSearch || '').trim();
    if (!q) { this.memberResults = []; return; }

    this.memberSearchTimeout = setTimeout(() => {
      this.searching = true;
      this.api.searchUsers(q).subscribe({
        next: (res: any[]) => {
          // normalize results (id, username, email)
          this.memberResults = (res || []).map(r => ({
            id: r.id ?? r.userId ?? r.id,
            username: r.username ?? r.userName ?? r.username,
            email: r.email ?? ''
          }));
          this.searching = false;
        },
        error: (err) => {
          console.error('User search failed', err);
          this.memberResults = [];
          this.searching = false;
        }
      });
    }, 300);
  }

  // confirm adding by numeric id (fallback)
  confirmAddMember(groupId: number, userId?: number) {
    const uid = userId ?? this.addUserId;
    if (!uid || uid <= 0) { alert('Enter a valid user id.'); return; }

    this.addingBusy = true;
    this.api.addUserToGroup(groupId, uid).subscribe({
      next: () => {
        this.addingBusy = false;
        this.cancelAddMember();
        this.fetch();
        alert('User added to group.');
      },
      error: (err) => {
        this.addingBusy = false;
        console.error('Add user failed', err);
        alert(err?.error?.message ?? 'Could not add user.');
      }
    });
  }

  // remove a member; shows spinner for that member while removing
  removeMember(groupId: number, userId: number) {
    if (!confirm(`Remove user ${userId} from this group?`)) return;
    const key = `${groupId}:${userId}`;
    this.removingBusy[key] = true;

    this.api.removeUserFromGroup(groupId, userId).subscribe({
      next: () => {
        delete this.removingBusy[key];
        this.fetch();
      },
      error: (err) => {
        delete this.removingBusy[key];
        console.error('Remove failed', err);
        alert('Could not remove user.');
      }
    });
  }

    logout() {
    localStorage.clear();
    window.location.href = '/login';
  }
}
