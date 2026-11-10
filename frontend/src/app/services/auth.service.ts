// src/app/services/auth.service.ts
import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  
  private apiUrl = 'http://localhost:5000/api/auth'; 

  currentUser = signal<User | null>(null);
  isAuthenticated = computed(() => !!this.currentUser());

  constructor() {
        this.loadCurrentUser();
  }

  register(credentials: RegisterRequest) {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, credentials).pipe(
      tap(() => this.loadCurrentUser().subscribe())
    );
  }

  login(credentials: LoginRequest) {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(() => this.loadCurrentUser().subscribe())
    );
  }

  loadCurrentUser() {
    return this.http.get<User>(`${this.apiUrl}/me`).pipe(
      tap(user => this.currentUser.set(user))
    );
  }

  logout() {
    localStorage.removeItem('token');
    this.currentUser.set(null);
  }
}