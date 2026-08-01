import { Injectable, inject } from '@angular/core';
import { Subject } from 'rxjs';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { AuthService } from '../auth/auth.service';
import { ActivityEvent } from '../models/app.models';

@Injectable({ providedIn: 'root' })
export class GuestSignalRService {
  private readonly authService = inject(AuthService);

  private readonly activitySubject = new Subject<ActivityEvent>();
  /** Emits each agentActivity event pushed from the server for this guest. */
  readonly activity$ = this.activitySubject.asObservable();

  private connection?: HubConnection;

  async connect(): Promise<void> {
    const token = this.authService.getToken();
    if (!token) {
      return;
    }

    if (!this.connection) {
      this.connection = new HubConnectionBuilder()
        .withUrl('/hubs/agent-activity', {
          accessTokenFactory: () => this.authService.getToken() ?? ''
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();

      this.connection.on('agentActivity', (event: ActivityEvent) => {
        this.activitySubject.next(event);
      });
    }

    if (this.connection.state === 'Disconnected') {
      await this.connection.start();
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection && this.connection.state === 'Connected') {
      await this.connection.stop();
    }
  }
}
