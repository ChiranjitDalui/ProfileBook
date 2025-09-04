import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './register.html',
  styleUrls: ['./register.css']
})
export class RegisterComponent {
  username = '';
  password = '';
  role = 'User';
  message = '';

  constructor(private authService: AuthService, private router: Router) {}

  register() {
    const user = { username: this.username, passwordHash: this.password, role: this.role };
   this.authService.register(user).subscribe({
  next: (res: any) => {
    this.message = res.message;   //  use backend message
    this.router.navigate(['/login']);
  },
  error: (err) => {
    this.message = err.error?.message || 'Registration failed';
  }
});

  }
}
