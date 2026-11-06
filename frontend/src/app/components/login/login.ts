import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  authService = inject(AuthService);
  private router = inject(Router);

  isRegister = false;
  email = 'test@example.com';
  password = 'Password123!';
  userName = 'testuser';
  displayName = 'Test User';
  errorMessage = '';

  onSubmit() {
    this.errorMessage = '';
    const action$ = this.isRegister
      ? this.authService.register({ userName: this.userName, email: this.email, displayName: this.displayName, password: this.password })
      : this.authService.login({ email: this.email, password: this.password });

    action$.subscribe({
      next: () => this.router.navigate(['/posts']),
      error: (err) => this.errorMessage = err.error || 'Ошибка входа'
    });
  }
}