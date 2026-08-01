import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { AuthUser } from '../models/app.models';

export interface AppShellState {
  user: AuthUser | null;
  loading: boolean;
  error: string | null;
}

@Injectable({ providedIn: 'root' })
export class AppStateService {
  private readonly initialState: AppShellState = {
    user: null,
    loading: false,
    error: null
  };

  private readonly stateSubject = new BehaviorSubject<AppShellState>(this.initialState);
  readonly state$ = this.stateSubject.asObservable();

  setUser(user: AuthUser | null): void {
    this.stateSubject.next({ ...this.stateSubject.value, user });
  }

  setLoading(loading: boolean): void {
    this.stateSubject.next({ ...this.stateSubject.value, loading });
  }

  setError(error: string | null): void {
    this.stateSubject.next({ ...this.stateSubject.value, error });
  }

  get snapshot(): AppShellState {
    return this.stateSubject.value;
  }
}
