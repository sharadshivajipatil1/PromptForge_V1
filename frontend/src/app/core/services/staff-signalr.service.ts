import { Injectable, inject } from '@angular/core';
import { Subject } from 'rxjs';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { AuthService } from '../auth/auth.service';
import { ActivityEvent, TaskSummary, TicketSummary } from '../models/app.models';

@Injectable({ providedIn: 'root' })
export class StaffSignalRService {
  private readonly authService = inject(AuthService);

  private readonly tasksSubject = new Subject<TaskSummary[]>();
  readonly tasks$ = this.tasksSubject.asObservable();

  private readonly activitySubject = new Subject<ActivityEvent[]>();
  readonly activity$ = this.activitySubject.asObservable();

  private readonly ticketsSubject = new Subject<TicketSummary[]>();
  readonly tickets$ = this.ticketsSubject.asObservable();

  private tasksConnection?: HubConnection;
  private activityConnection?: HubConnection;
  private activityEvents: ActivityEvent[] = [];

  async connect(): Promise<void> {
    const token = this.authService.getToken();
    if (!token) {
      return;
    }

    if (!this.tasksConnection) {
      this.tasksConnection = new HubConnectionBuilder()
        .withUrl('/hubs/dashboard', {
          accessTokenFactory: () => this.authService.getToken() ?? ''
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Information)
        .build();

      this.tasksConnection.on('tasksUpdated', (tasks: TaskSummary[]) => {
        this.tasksSubject.next(tasks);
      });

      // Listen for ticket updates pushed when a guest escalates in chat
      this.tasksConnection.on('ticketsUpdated', (tickets: TicketSummary[]) => {
        this.ticketsSubject.next(tickets);
      });
    }

    if (this.tasksConnection.state === 'Disconnected') {
      await this.tasksConnection.start();
    }

    if (!this.activityConnection) {
      this.activityConnection = new HubConnectionBuilder()
        .withUrl('/hubs/agent-activity', {
          accessTokenFactory: () => this.authService.getToken() ?? ''
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Information)
        .build();

      this.activityConnection.on('agentActivity', (activity: ActivityEvent) => {
        this.activityEvents = [activity, ...this.activityEvents].slice(0, 10);
        this.activitySubject.next(this.activityEvents);
      });
    }

    if (this.activityConnection.state === 'Disconnected') {
      await this.activityConnection.start();
    }
  }

  async disconnect(): Promise<void> {
    if (this.tasksConnection && this.tasksConnection.state === 'Connected') {
      await this.tasksConnection.stop();
    }

    if (this.activityConnection && this.activityConnection.state === 'Connected') {
      await this.activityConnection.stop();
    }
  }
}
