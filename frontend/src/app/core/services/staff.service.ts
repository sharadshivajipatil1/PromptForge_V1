import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { EnrichedForecastSummary, ForecastSummary, PriorityRecommendation, TaskSummary, TicketSummary } from '../models/app.models';

const API_BASE_URL = 'http://localhost:5189/api';

@Injectable({ providedIn: 'root' })
export class StaffService {
  private readonly http = inject(HttpClient);

  getTasks(): Observable<TaskSummary[]> {
    return this.http.get<TaskSummary[]>(`${API_BASE_URL}/dashboard/tasks`);
  }

  createTask(payload: {
    description: string;
    type: string;
    roomNumber?: string;
    slaMinutes?: number;
    assignedTo?: string;
    department?: string;
    priority?: string;
  }): Observable<TaskSummary> {
    return this.http.post<TaskSummary>(`${API_BASE_URL}/dashboard/tasks`, payload);
  }

  completeTask(taskId: string): Observable<TaskSummary> {
    return this.http.post<TaskSummary>(`${API_BASE_URL}/dashboard/tasks/${taskId}/complete`, {});
  }

  updateTaskStatus(taskId: string, status: 'Open' | 'Pending' | 'Completed' | number): Observable<TaskSummary> {
    return this.http.patch<TaskSummary>(`${API_BASE_URL}/dashboard/tasks/${taskId}/status`, { status });
  }

  getForecast(): Observable<ForecastSummary> {
    return this.http.get<ForecastSummary>(`${API_BASE_URL}/dashboard/forecast`);
  }

  getProfile(): Observable<{ fullName: string; department: string; role: string }> {
    return this.http.get<{ fullName: string; department: string; role: string }>(`${API_BASE_URL}/dashboard/profile`);
  }

  updateProfile(payload: { fullName: string; department: string; role: string }): Observable<{ fullName: string; department: string; role: string }> {
    return this.http.put<{ fullName: string; department: string; role: string }>(`${API_BASE_URL}/dashboard/profile`, payload);
  }

  getStaffingForecast(): Observable<any> {
    return this.http.get<any>(`${API_BASE_URL}/dashboard/forecast`);
  }

  getOperationsForecast(): Observable<any> {
    return this.http.get<any>(`${API_BASE_URL}/dashboard/forecast`);
  }

  getPriorityRecommendation(description: string): Observable<any> {
    return this.http.post<any>(`${API_BASE_URL}/dashboard/priority-recommendation`, { description });
  }

  getTickets(): Observable<TicketSummary[]> {
    return this.http.get<TicketSummary[]>(`${API_BASE_URL}/dashboard/tickets`);
  }

  resolveTicket(ticketId: string): Observable<TicketSummary> {
    return this.http.post<TicketSummary>(`${API_BASE_URL}/dashboard/tickets/${ticketId}/resolve`, {});
  }
}
