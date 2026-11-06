import { Component, inject } from '@angular/core';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.html'
})
export class AppComponent {
  authService = inject(AuthService);

  email = 'test@example.com';
  password = 'Password123!';
  errorMessage = '';

  onLogin(event: Event) {
    event.preventDefault();
    this.errorMessage = '';

    this.authService.login({ email: this.email, password: this.password }).subscribe({
      next: () => console.log('Вход выполнен успешно!'),
      error: (err) => {
        console.error(err);
        this.errorMessage = err.error || 'Ошибка входа. Проверьте консоль браузера (F12).';
      }
    });
  }
}