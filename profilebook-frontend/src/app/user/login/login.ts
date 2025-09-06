// src/app/user/login/login.ts
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth';
import { SignalRService } from '../../services/signalr';
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class LoginComponent {
  username = '';
  password = '';
  message = '';

  constructor(
    private authService: AuthService,
    private router: Router,
    private signalR: SignalRService
  ) {}

  login() {
    const user = { username: this.username, passwordHash: this.password };
    this.authService.login(user).subscribe({
      next: (res: any) => {
        // save token & role
        const token = res.token;
        localStorage.setItem('token', token);
        localStorage.setItem('role', res.role);

        // decode JWT payload to get user id and save it
        const userId = this.getUserIdFromToken(token);
        if (userId !== null) {
          localStorage.setItem('currentUserId', String(userId));
        }

        // connect SignalR with token (accessTokenFactory on client)
        this.signalR.connect(token)
          .then(() => console.log('SignalR connected'))
          .catch(err => console.error('SignalR connect error:', err));

        this.message = 'Login successful!';
        if (res.role === 'Admin') {
          this.router.navigate(['/admin/posts']);
        } else {
          this.router.navigate(['/posts']);
        }
      },
      error: () => {
        this.message = 'Invalid username or password';
      }
    });
  }

  // Utility: decode JWT and extract claim "nameid" or "sub" or NameIdentifier
  // returns numeric user id or null
  private getUserIdFromToken(token: string): number | null {
    try {
      const payloadBase64 = token.split('.')[1];
      // handle base64 padding
      const payload = JSON.parse(atob(this.padBase64(payloadBase64)));
      // try common claim names
      const idCandidate = payload['nameid'] ?? payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? payload['sub'] ?? payload['id'];
      if (!idCandidate) return null;
      const idNum = parseInt(String(idCandidate), 10);
      return Number.isNaN(idNum) ? null : idNum;
    } catch (e) {
      console.warn('Failed to parse token for user id', e);
      return null;
    }
  }

  // atob requires properly padded base64
  private padBase64(base64: string) {
    // replace URL-safe chars
    base64 = base64.replace(/-/g, '+').replace(/_/g, '/');
    const pad = base64.length % 4;
    if (pad === 2) base64 += '==';
    else if (pad === 3) base64 += '=';
    else if (pad !== 0) base64 += '===';
    return base64;
  }
}
