import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../services/api';

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
  selector: 'app-admin-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reports.html'
})
export class AdminReportsComponent implements OnInit {
  reports: any[] = [];
  filtered: any[] = [];
  loading = false;

  // simple filters
  q = '';
  user = '';
  reported = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    // rudimentary guard: check role in JWT
    const tok = localStorage.getItem('token');
    const payload = parseJwt(tok);
    const roles = (payload?.role || payload?.roles || '').toString();
    const isAdmin = roles.includes('Admin');

    if (!isAdmin) {
      alert('Admin only. Redirecting to posts.');
      window.location.href = '/posts';
      return;
    }

    this.fetchReports();
  }

  fetchReports() {
    this.loading = true;
    this.api.getAllReports().subscribe({
      next: (res: any[]) => {
        // Expecting: Id, Reason, TimeStamp, ReportingUser, ReportedUser
        this.reports = (res || []).map(r => ({
          ...r,
          time: r.timeStamp ? new Date(r.timeStamp) : null
        }));
        this.filtered = this.reports.slice();
        this.loading = false;
      },
      error: (err:any) => {
        console.error('Failed to load reports', err);
        this.loading = false;
        alert(err?.status === 403 ? 'Admin permission required.' : 'Could not load reports.');
      }
    });
  }

  applyFilters() {
    const q = (this.q || '').toLowerCase();
    const u = (this.user || '').toLowerCase();
    const rep = (this.reported || '').toLowerCase();

    this.filtered = this.reports.filter(r => {
      const reason = (r.reason || '').toLowerCase();
      const ru = (r.reportingUser || '').toLowerCase();
      const rd = (r.reportedUser || '').toLowerCase();
      return (q ? reason.includes(q) : true)
          && (u ? ru.includes(u) : true)
          && (rep ? rd.includes(rep) : true);
    });
  }

  clearFilters() {
    this.q = this.user = this.reported = '';
    this.filtered = this.reports.slice();
  }
}
