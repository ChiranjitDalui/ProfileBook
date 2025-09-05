import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth';

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

  constructor(private authService: AuthService, private router: Router) {}

  login() {
    const user = { username: this.username, passwordHash: this.password };
    this.authService.login(user).subscribe({
      next: (res: any) => {
        localStorage.setItem('token', res.token);
        localStorage.setItem('role', res.role);
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
}
