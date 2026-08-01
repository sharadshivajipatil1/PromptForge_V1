import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { AppStateService } from '../state/app-state.service';
import { AuthUser, LoginResponse } from '../models/app.models';

const API_BASE_URL = 'http://localhost:5189/api';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly appState = inject(AppStateService);

  private readonly storageKey = 'hospitality.auth.token';
  private readonly userKey = 'hospitality.auth.user';

  private readonly currentUserSubject = new BehaviorSubject<AuthUser | null>(this.readStoredUser());
  readonly currentUser$ = this.currentUserSubject.asObservable();

  guestLogin(reservationCode: string): Observable<AuthUser> {
    return this.http.post<LoginResponse>(`${API_BASE_URL}/auth/guest-login`, { reservationCode }).pipe(
      map((response) => this.mapToUser(response, 'Guest')),
      tap((user) => this.persistUser(user))
    );
  }

  staffLogin(username: string, password: string): Observable<AuthUser> {
    return this.http.post<LoginResponse>(`${API_BASE_URL}/auth/staff-login`, { username, password }).pipe(
      map((response) => this.mapToUser(response, 'Staff')),
      tap((user) => this.persistUser(user))
    );
  }

  logout(): void {
    localStorage.removeItem(this.storageKey);
    localStorage.removeItem(this.userKey);
    this.currentUserSubject.next(null);
    this.appState.setUser(null);
  }

  updateCurrentUserProfile(name: string, role?: string, department?: string): void {
    const currentUser = this.currentUserSubject.value;
    if (!currentUser) {
      return;
    }

    const updatedUser: AuthUser = {
      ...currentUser,
      name,
      role: role || currentUser.role,
      department: department || currentUser.department,
      roles: role ? ['Staff', role] : currentUser.roles
    };

    localStorage.setItem(this.userKey, JSON.stringify(updatedUser));
    this.currentUserSubject.next(updatedUser);
    this.appState.setUser(updatedUser);
  }

  getToken(): string | null {
    return localStorage.getItem(this.storageKey);
  }

  isAuthenticated(): boolean {
    return !!this.currentUserSubject.value || !!this.getToken();
  }

  hasAnyRole(expectedRoles: string[]): boolean {
    const user = this.currentUserSubject.value;
    return !!user && expectedRoles.some((role) => user.roles.includes(role));
  }

  private persistUser(user: AuthUser): void {
    localStorage.setItem(this.storageKey, user.token);
    localStorage.setItem(this.userKey, JSON.stringify(user));
    this.currentUserSubject.next(user);
    this.appState.setUser(user);
  }

  private readStoredUser(): AuthUser | null {
    const token = localStorage.getItem(this.storageKey);
    const storedUser = localStorage.getItem(this.userKey);

    if (!token || !storedUser) {
      return null;
    }

    try {
      const parsed = JSON.parse(storedUser) as AuthUser;
      return parsed.token === token ? parsed : null;
    } catch {
      return null;
    }
  }

  private mapToUser(response: LoginResponse, fallbackRole: string): AuthUser {
    const roles = response.roles?.length ? response.roles : [fallbackRole];

    return {
      id: response.userId ?? 'guest-user',
      name: response.fullName ?? response.reservationCode ?? 'Hospitality User',
      roles,
      token: response.token
    };
  }
}
