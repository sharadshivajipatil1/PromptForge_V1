import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/auth/auth.service';
import { StaffService } from '../../../core/services/staff.service';
import { StaffSignalRService } from '../../../core/services/staff-signalr.service';
import { ActivityEvent, TaskSummary, TicketSummary } from '../../../core/models/app.models';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

interface StaffAssignee {
  name: string;
  department: string;
  role: string;
}

interface PrioritySuggestion {
  priority: string;
  confidence: number;
  reasoning: string;
}

interface StaffingForecast {
  recommendedStaff: number;
  reasoning: string;
}

interface InventoryForecast {
  criticalItems: number;
  topItem: string;
}

interface OperationsForecast {
  expectedTasks: number;
  period: string;
}

@Component({
  selector: 'app-staff-tasks',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="page-shell">
      <div *ngIf="toastMessage" class="toast">{{ toastMessage }}</div>

      <header class="topbar">
        <div>
          <p class="eyebrow">StaySmart</p>
          <h1>Live Task Dashboard</h1>
          <p class="subtitle">AI-prioritized operations queue with department-aware assignments and live SLA timers.</p>
        </div>

        <div class="topbar-actions">
          <button class="secondary-btn" type="button" (click)="toggleProfileEditor()">
            {{ showProfileEditor ? 'Hide profile' : 'Manage profile' }}
          </button>
          <button class="refresh-btn" type="button" (click)="refreshTasks()">
            <span>⟳</span>
            Refresh
          </button>
          <button class="logout-btn" type="button" (click)="signOff()">
            Sign off
          </button>
        </div>
      </header>

      <!-- Prediction Panel -->
      <section class="prediction-panel">
        <div class="prediction-header">
          <h2>AI Operations Forecast</h2>
          <span class="prediction-status" [class.loading]="forecastLoading">
            {{ forecastLoading ? 'Analyzing...' : 'Live Predictions' }}
          </span>
        </div>
        
        <div class="prediction-grid">
          <div class="prediction-card">
            <div class="prediction-icon">👥</div>
            <div class="prediction-content">
              <h3>Staffing Forecast</h3>
              <div class="prediction-value">{{ staffingForecast?.recommendedStaff || 'N/A' }} staff</div>
              <div class="prediction-detail">{{ staffingForecast?.reasoning || 'Calculating optimal staffing levels...' }}</div>
            </div>
          </div>
          
          <div class="prediction-card">
            <div class="prediction-icon">📦</div>
            <div class="prediction-content">
              <h3>Inventory Alert</h3>
              <div class="prediction-value">{{ inventoryForecast?.criticalItems || 0 }} items low</div>
              <div class="prediction-detail">{{ inventoryForecast?.topItem || 'All inventory levels adequate' }}</div>
            </div>
          </div>
          
          <div class="prediction-card">
            <div class="prediction-icon">⚡</div>
            <div class="prediction-content">
              <h3>Task Volume</h3>
              <div class="prediction-value">{{ operationsForecast?.expectedTasks || 'N/A' }} tasks</div>
              <div class="prediction-detail">{{ operationsForecast?.period || 'Next 4 hours prediction' }}</div>
            </div>
          </div>
          
          <div class="prediction-card">
            <div class="prediction-icon">🎯</div>
            <div class="prediction-content">
              <h3>Priority Analysis</h3>
              <div class="prediction-value" [class]="getPriorityClass(prioritySuggestion?.priority)">
                {{ prioritySuggestion?.priority || 'Medium' }}
              </div>
              <div class="prediction-detail">{{ prioritySuggestion?.reasoning || 'AI priority analysis active' }}</div>
            </div>
          </div>
        </div>
      </section>

      <section class="stats-grid">
        <article class="stat-card pulse-card">
          <span class="stat-label">Open queue</span>
          <strong>{{ openCount }}</strong>
          <small>Needs immediate attention</small>
        </article>
        <article class="stat-card">
          <span class="stat-label">High priority</span>
          <strong>{{ highPriorityCount }}</strong>
          <small>Escalation-ready tasks</small>
        </article>
        <article class="stat-card">
          <span class="stat-label">Assigned to you</span>
          <strong>{{ assignedToCurrentUserCount }}</strong>
          <small>Visible on your staff login</small>
        </article>
        <article class="stat-card">
          <span class="stat-label">Live sync</span>
          <strong>ON</strong>
          <small>SignalR activity stream</small>
        </article>
      </section>

      <section class="control-panel">
        <form *ngIf="showProfileEditor" class="profile-card" (ngSubmit)="saveProfile()">
          <div class="section-title profile-title">
            <h2>Staff profile</h2>
            <span>Changes update the stored staff profile</span>
          </div>
          <div class="profile-grid">
            <label>
              <span>Full name</span>
              <input [(ngModel)]="profileForm.fullName" name="fullName" placeholder="Enter full name" />
            </label>
            <label>
              <span>Department</span>
              <input [(ngModel)]="profileForm.department" name="department" placeholder="Enter department" />
            </label>
            <label>
              <span>Role</span>
              <select [(ngModel)]="profileForm.role" name="role">
                <option value="FrontDesk">Front Desk</option>
                <option value="Manager">Manager</option>
                <option value="Housekeeping">Housekeeping</option>
              </select>
            </label>
            <button class="save-profile-btn" type="submit">Save profile</button>
          </div>
        </form>

        <div class="filter-grid">
          <label>
            <span>Status</span>
            <select [(ngModel)]="statusFilter">
              <option value="">All</option>
              <option value="Pending">Pending</option>
              <option value="Completed">Completed</option>
            </select>
          </label>

          <label>
            <span>Priority</span>
            <select [(ngModel)]="priorityFilter">
              <option value="">All</option>
              <option value="High">High</option>
              <option value="Medium">Medium</option>
              <option value="Low">Low</option>
            </select>
          </label>

          <label>
            <span>Assign to</span>
            <select [(ngModel)]="newTask.assignedTo">
              <option value="">Auto assign</option>
              <option *ngFor="let staff of staffRoster" [value]="staff.name">{{ staff.name }} • {{ staff.department }}</option>
            </select>
          </label>

          <label>
            <span>Sort by</span>
            <select [(ngModel)]="sortField">
              <option value="priority">Priority</option>
              <option value="createdAt">Created</option>
              <option value="description">Description</option>
            </select>
          </label>

          <label class="checkbox-wrap">
            <input type="checkbox" [(ngModel)]="showOnlyOpen" />
            <span>Open only</span>
          </label>
        </div>

        <form class="composer" (ngSubmit)="createTask()">
          <div class="task-input-group">
            <input 
              [(ngModel)]="newTask.description" 
              name="description" 
              placeholder="Task description" 
              required 
              (input)="onTaskDescriptionChange($event)"
              class="task-description-input"
            />
            <div *ngIf="prioritySuggestion && newTask.description.length > 10" 
                 class="priority-suggestion" 
                 [class.suggestion-loading]="priorityAnalysisLoading">
              <div class="suggestion-content">
                <span class="suggestion-label">AI Priority:</span>
                <span class="suggestion-priority" [class]="getPriorityClass(prioritySuggestion.priority)">
                  {{ prioritySuggestion.priority }}
                </span>
                <span class="suggestion-confidence">{{ prioritySuggestion.confidence }}%</span>
                <button type="button" 
                        class="apply-suggestion-btn" 
                        (click)="applyPrioritySuggestion()"
                        *ngIf="!priorityAnalysisLoading">
                  Apply
                </button>
              </div>
              <div class="suggestion-reasoning">{{ prioritySuggestion.reasoning }}</div>
            </div>
          </div>
          <input [(ngModel)]="newTask.roomNumber" name="roomNumber" placeholder="Room number" />
          <select [(ngModel)]="newTask.type" name="type">
            <option value="Housekeeping">Housekeeping</option>
            <option value="Maintenance">Maintenance</option>
            <option value="GuestRequest">Guest Request</option>
            <option value="RoomService">Room Service</option>
          </select>
          <input [(ngModel)]="newTask.slaMinutes" name="slaMinutes" type="number" min="10" placeholder="SLA min" />
          <button type="submit">Create task</button>
        </form>
      </section>

      <section class="content-grid">
        <article class="board-card table-card">
          <div class="section-title">
            <h2>Active operations queue</h2>
            <span>{{ filteredTasks.length }} visible items</span>
          </div>

          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Task</th>
                  <th>Assignee</th>
                  <th>Status</th>
                  <th>Priority</th>
                  <th>Timer</th>
                  <th>Room</th>
                  <th>Created</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let task of filteredTasks">
                  <td>{{ task.description }}</td>
                  <td>
                    <div class="assignee-cell">
                      <strong>{{ task.assignedTo || 'Unassigned' }}</strong>
                      <small>{{ getDepartmentLabel(task.assignedTo) }}</small>
                    </div>
                  </td>
                  <td>
                    <span class="status-pill" [class.completed]="task.status === 'Completed'" [class.pending]="task.status !== 'Completed'">
                      {{ task.status }}
                    </span>
                  </td>
                  <td>
                    <span class="priority-pill" [class.high]="task.priority === 'High'" [class.medium]="task.priority === 'Medium'" [class.low]="task.priority === 'Low'">
                      {{ task.priority }}
                    </span>
                  </td>
                  <td>
                    <span class="timer-pill" [class.overdue]="isOverdue(task)">
                      {{ getTaskTimer(task) }}
                    </span>
                  </td>
                  <td>{{ task.roomNumber || '—' }}</td>
                  <td>{{ task.createdAt || '—' }}</td>
                  <td>
                    <button class="complete-btn" type="button" (click)="toggleTaskStatus(task)">
                      {{ task.status === 'Completed' ? 'Mark open' : 'Mark complete' }}
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </article>

        <article class="board-card stream-card">
          <div class="section-title">
            <h2>Department roster</h2>
            <span>{{ currentStaffDepartment }}</span>
          </div>

          <div class="roster-grid">
            <div class="roster-card" *ngFor="let staff of staffRoster">
              <div class="roster-top">
                <span class="roster-badge">{{ staff.role }}</span>
                <strong>{{ staff.name }}</strong>
              </div>
              <p>{{ staff.department }}</p>
            </div>
          </div>

          <div class="section-title activity-title">
            <h2>Live activity stream</h2>
            <span>Instant updates</span>
          </div>

          <ul class="activity-list">
            <li *ngFor="let event of activityEvents">
              <div class="activity-dot"></div>
              <div>
                <strong>{{ event.agentName }}</strong>
                <p>{{ event.message }}</p>
                <small>{{ event.timestamp | date:'short' }}</small>
              </div>
            </li>
          </ul>

          <!-- ── Concierge Escalation Tickets ── -->
          <div class="section-title activity-title" style="margin-top:1.25rem">
            <h2>Concierge tickets</h2>
            <span>{{ openTicketCount }} open</span>
          </div>

          <div *ngIf="tickets.length === 0" class="no-tickets">
            No open escalation tickets.
          </div>

          <ul class="ticket-list">
            <li *ngFor="let ticket of tickets" class="ticket-card" [class.ticket-resolved]="ticket.status === 'Resolved'">
              <div class="ticket-header">
                <span class="ticket-badge" [class.badge-resolved]="ticket.status === 'Resolved'">
                  {{ ticket.status }}
                </span>
                <span class="ticket-room" *ngIf="ticket.roomNumber">Room {{ ticket.roomNumber }}</span>
                <small class="ticket-time">{{ ticket.createdAt | date:'HH:mm, d MMM' }}</small>
              </div>
              <strong class="ticket-guest">{{ ticket.guestName }}</strong>
              <p class="ticket-message">{{ ticket.message }}</p>
              <button
                *ngIf="ticket.status !== 'Resolved'"
                class="resolve-btn"
                (click)="resolveTicket(ticket.id)">
                Mark resolved
              </button>
            </li>
          </ul>
        </article>
      </section>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-height: 100vh;
        background:
          radial-gradient(circle at top left, rgba(96, 165, 250, 0.28), transparent 18%),
          radial-gradient(circle at bottom right, rgba(124, 58, 237, 0.22), transparent 20%),
          linear-gradient(135deg, #f8fbff 0%, #edf5ff 60%, #f9fbff 100%);
        color: #0f172a;
        font-family: Inter, 'Segoe UI', sans-serif;
        padding: 1.5rem;
      }

      .page-shell {
        display: flex;
        flex-direction: column;
        gap: 1.25rem;
        position: relative;
      }

      .toast {
        position: fixed;
        top: 1.25rem;
        right: 1.25rem;
        z-index: 1100;
        padding: 0.85rem 1rem;
        border-radius: 999px;
        background: linear-gradient(135deg, #16a34a, #15803d);
        color: white;
        font-weight: 700;
        box-shadow: 0 18px 40px rgba(21, 128, 61, 0.24);
        animation: toastIn 0.25s ease;
      }

      .topbar,
      .control-panel,
      .board-card,
      .stat-card,
      .prediction-panel {
        background: rgba(255, 255, 255, 0.9);
        border: 1px solid rgba(148, 163, 184, 0.22);
        border-radius: 24px;
        box-shadow: 0 22px 60px rgba(15, 23, 42, 0.08);
      }

      .topbar {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 1rem;
        padding: 1.35rem 1.5rem;
      }

      .topbar-actions {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        flex-wrap: wrap;
      }

      .eyebrow {
        margin: 0 0 0.35rem;
        color: #4f46e5;
        text-transform: uppercase;
        letter-spacing: 0.24em;
        font-size: 0.75rem;
        font-weight: 700;
      }

      .topbar h1 {
        margin: 0;
        font-size: 1.9rem;
        letter-spacing: -0.03em;
      }

      .subtitle {
        margin: 0.4rem 0 0;
        color: #64748b;
      }

      .refresh-btn,
      .logout-btn,
      .secondary-btn,
      .save-profile-btn {
        border: none;
        border-radius: 14px;
        padding: 0.85rem 1rem;
        color: white;
        font-weight: 700;
        cursor: pointer;
        display: flex;
        align-items: center;
        gap: 0.45rem;
      }

      .refresh-btn {
        background: linear-gradient(135deg, #2563eb, #7c3aed);
        box-shadow: 0 16px 34px rgba(37, 99, 235, 0.24);
      }

      .logout-btn {
        background: linear-gradient(135deg, #ef4444, #dc2626);
      }

      .secondary-btn {
        background: linear-gradient(135deg, #0f766e, #14b8a6);
      }

      .save-profile-btn {
        background: linear-gradient(135deg, #16a34a, #15803d);
        justify-content: center;
      }

      .stats-grid {
        display: grid;
        grid-template-columns: repeat(4, minmax(0, 1fr));
        gap: 1rem;
      }

      .stat-card {
        padding: 1rem 1.1rem;
        position: relative;
        overflow: hidden;
        transition: transform 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
      }

      .stat-card:hover,
      .board-card:hover,
      .roster-card:hover,
      .activity-list li:hover {
        transform: translateY(-3px);
        box-shadow: 0 18px 42px rgba(15, 23, 42, 0.16);
        background: linear-gradient(135deg, #ffffff 0%, #eef6ff 100%);
      }

      .pulse-card::after {
        content: '';
        position: absolute;
        inset: auto -10% -45% auto;
        width: 160px;
        height: 160px;
        border-radius: 50%;
        background: radial-gradient(circle, rgba(37, 99, 235, 0.22), transparent 68%);
        animation: drift 8s linear infinite;
      }

      .stat-label {
        display: block;
        margin-bottom: 0.45rem;
        text-transform: uppercase;
        letter-spacing: 0.16em;
        font-size: 0.72rem;
        color: #64748b;
      }

      .stat-card strong {
        display: block;
        font-size: 1.85rem;
        line-height: 1;
      }

      .stat-card small {
        display: block;
        margin-top: 0.45rem;
        color: #64748b;
      }

      .control-panel {
        padding: 1.2rem;
      }

      .profile-card {
        margin-bottom: 1rem;
        padding: 1rem;
        border: 1px solid #dbe7fb;
        border-radius: 18px;
        background: linear-gradient(135deg, rgba(248, 250, 252, 0.95), rgba(239, 246, 255, 0.95));
      }

      .profile-title {
        margin-bottom: 0.75rem;
      }

      .profile-grid {
        display: grid;
        grid-template-columns: 1.4fr 1fr 1fr auto;
        gap: 0.8rem;
        align-items: end;
      }

      .filter-grid {
        display: grid;
        grid-template-columns: repeat(5, minmax(0, 1fr));
        gap: 0.9rem;
        align-items: end;
      }

      .filter-grid label,
      .composer {
        display: flex;
        flex-direction: column;
        gap: 0.45rem;
      }

      .filter-grid span {
        color: #334155;
        font-size: 0.82rem;
        font-weight: 700;
      }

      select,
      input {
        width: 100%;
        border: 1px solid #cbd5e1;
        border-radius: 12px;
        padding: 0.78rem 0.9rem;
        font: inherit;
        background: #f8fbff;
        box-sizing: border-box;
      }

      select:focus,
      input:focus {
        outline: 2px solid rgba(37, 99, 235, 0.18);
        border-color: #60a5fa;
      }

      .checkbox-wrap {
        flex-direction: row !important;
        align-items: center;
        justify-content: center;
        gap: 0.5rem;
        padding: 0.8rem 0;
      }

      .checkbox-wrap input {
        width: 1rem;
        height: 1rem;
      }

      .composer {
        margin-top: 1rem;
        display: grid;
        grid-template-columns: 2fr 1fr 1fr 1fr auto;
        gap: 0.8rem;
      }

      .composer button,
      .complete-btn {
        border: none;
        border-radius: 12px;
        background: linear-gradient(135deg, #2563eb, #7c3aed);
        color: white;
        font-weight: 700;
        cursor: pointer;
        padding: 0.8rem 1rem;
      }

      .content-grid {
        display: grid;
        grid-template-columns: 1.35fr 0.65fr;
        gap: 1rem;
      }

      .board-card {
        padding: 1.2rem;
      }

      .section-title {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 1rem;
        margin-bottom: 1rem;
      }

      .section-title h2 {
        margin: 0;
        font-size: 1.05rem;
      }

      .section-title span {
        color: #64748b;
        font-size: 0.85rem;
      }

      .table-wrap {
        overflow: auto;
      }

      table {
        width: 100%;
        border-collapse: collapse;
      }

      th,
      td {
        padding: 0.85rem 0.6rem;
        text-align: left;
        border-bottom: 1px solid #e2e8f0;
        vertical-align: middle;
      }

      th {
        color: #64748b;
        font-size: 0.78rem;
        text-transform: uppercase;
        letter-spacing: 0.16em;
      }

      tbody tr {
        transition: transform 0.2s ease, background 0.2s ease;
      }

      tbody tr:hover {
        background: rgba(239, 246, 255, 0.7);
        transform: translateX(2px);
      }

      .assignee-cell {
        display: flex;
        flex-direction: column;
        gap: 0.2rem;
      }

      .assignee-cell strong {
        color: #0f172a;
      }

      .assignee-cell small {
        color: #64748b;
      }

      .status-pill,
      .priority-pill,
      .timer-pill,
      .roster-badge {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        border-radius: 999px;
        padding: 0.35rem 0.65rem;
        font-size: 0.78rem;
        font-weight: 700;
      }

      .status-pill.pending {
        background: #e0f2fe;
        color: #0c4a6e;
      }

      .status-pill.completed {
        background: #dcfce7;
        color: #166534;
      }

      .priority-pill.high {
        background: #fee2e2;
        color: #b91c1c;
      }

      .priority-pill.medium {
        background: #fef3c7;
        color: #92400e;
      }

      .priority-pill.low {
        background: #dbeafe;
        color: #1d4ed8;
      }

      .timer-pill {
        background: #ede9fe;
        color: #5b21b6;
      }

      .timer-pill.overdue {
        background: #fee2e2;
        color: #b91c1c;
      }

      .roster-grid {
        display: grid;
        gap: 0.8rem;
        margin-bottom: 1rem;
      }

      .roster-card {
        background: #f8fbff;
        border: 1px solid #dbe7fb;
        border-radius: 16px;
        padding: 0.9rem;
        transition: transform 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
      }

      .roster-top {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        margin-bottom: 0.35rem;
      }

      .roster-badge {
        background: #e0f2fe;
        color: #0c4a6e;
      }

      .roster-card p {
        margin: 0;
        color: #64748b;
      }

      .activity-title {
        margin-top: 0.5rem;
      }

      .activity-list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 0.85rem;
      }

      .activity-list li {
        display: flex;
        gap: 0.75rem;
        align-items: flex-start;
        background: #f8fbff;
        border: 1px solid #dbe7fb;
        border-radius: 16px;
        padding: 0.85rem;
        animation: fadeIn 0.35s ease;
        transition: transform 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
      }

      .activity-dot {
        width: 0.75rem;
        height: 0.75rem;
        margin-top: 0.35rem;
        border-radius: 50%;
        background: #2563eb;
        box-shadow: 0 0 0 0 rgba(37, 99, 235, 0.4);
        animation: ping 2.2s infinite;
      }

      .activity-list strong {
        display: block;
        margin-bottom: 0.25rem;
      }

      .activity-list p {
        margin: 0 0 0.3rem;
        color: #475569;
        line-height: 1.5;
      }

      .activity-list small {
        color: #64748b;
      }

      /* ── Concierge Ticket styles ── */
      .no-tickets {
        color: #94a3b8;
        font-size: 0.875rem;
        padding: 0.5rem 0 0.75rem;
      }

      .ticket-list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 0.65rem;
      }

      .ticket-card {
        background: linear-gradient(135deg, #fff7ed, #fffbeb);
        border: 1px solid #fed7aa;
        border-left: 4px solid #f97316;
        border-radius: 14px;
        padding: 0.85rem 1rem;
        transition: transform 0.18s ease, box-shadow 0.18s ease;
      }

      .ticket-card:hover {
        transform: translateY(-2px);
        box-shadow: 0 8px 20px rgba(249, 115, 22, 0.12);
      }

      .ticket-resolved {
        background: linear-gradient(135deg, #f0fdf4, #f7fef9);
        border-color: #bbf7d0;
        border-left-color: #22c55e;
        opacity: 0.75;
      }

      .ticket-header {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 0.4rem;
        flex-wrap: wrap;
      }

      .ticket-badge {
        display: inline-block;
        font-size: 0.68rem;
        font-weight: 700;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        padding: 2px 8px;
        border-radius: 999px;
        background: #fed7aa;
        color: #c2410c;
      }

      .badge-resolved {
        background: #bbf7d0;
        color: #15803d;
      }

      .ticket-room {
        font-size: 0.75rem;
        font-weight: 600;
        color: #64748b;
      }

      .ticket-time {
        margin-left: auto;
        font-size: 0.72rem;
        color: #94a3b8;
      }

      .ticket-guest {
        display: block;
        font-size: 0.9rem;
        color: #0f172a;
        margin-bottom: 0.25rem;
      }

      .ticket-message {
        margin: 0 0 0.6rem;
        font-size: 0.82rem;
        color: #475569;
        line-height: 1.5;
      }

      .resolve-btn {
        border: none;
        background: linear-gradient(135deg, #16a34a, #15803d);
        color: white;
        font-size: 0.78rem;
        font-weight: 700;
        padding: 5px 14px;
        border-radius: 8px;
        cursor: pointer;
        transition: opacity 0.2s;
      }

      .resolve-btn:hover {
        opacity: 0.88;
      }

      /* ── Prediction Panel Styles ── */
      .prediction-panel {
        padding: 1.5rem;
      }

      .prediction-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 1.25rem;
      }

      .prediction-header h2 {
        margin: 0;
        font-size: 1.15rem;
        color: #0f172a;
      }

      .prediction-status {
        font-size: 0.85rem;
        color: #16a34a;
        font-weight: 600;
        padding: 0.4rem 0.8rem;
        border-radius: 999px;
        background: rgba(22, 163, 74, 0.1);
        border: 1px solid rgba(22, 163, 74, 0.2);
      }

      .prediction-status.loading {
        color: #2563eb;
        background: rgba(37, 99, 235, 0.1);
        border-color: rgba(37, 99, 235, 0.2);
        animation: pulse 2s infinite;
      }

      .prediction-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 1rem;
      }

      .prediction-card {
        background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
        border: 1px solid #e2e8f0;
        border-radius: 16px;
        padding: 1.2rem;
        transition: all 0.2s ease;
        position: relative;
        overflow: hidden;
      }

      .prediction-card:hover {
        transform: translateY(-2px);
        box-shadow: 0 12px 28px rgba(15, 23, 42, 0.12);
        background: linear-gradient(135deg, #ffffff 0%, #f8fafc 100%);
      }

      .prediction-card::before {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        height: 3px;
        background: linear-gradient(90deg, #2563eb, #7c3aed);
        border-radius: 16px 16px 0 0;
      }

      .prediction-icon {
        font-size: 1.5rem;
        margin-bottom: 0.75rem;
        display: block;
      }

      .prediction-content h3 {
        margin: 0 0 0.5rem 0;
        font-size: 0.9rem;
        color: #64748b;
        text-transform: uppercase;
        letter-spacing: 0.1em;
        font-weight: 600;
      }

      .prediction-value {
        font-size: 1.4rem;
        font-weight: 700;
        color: #0f172a;
        margin-bottom: 0.5rem;
        line-height: 1.2;
      }

      .prediction-detail {
        font-size: 0.8rem;
        color: #64748b;
        line-height: 1.4;
      }

      /* ── Priority Suggestion Styles ── */
      .task-input-group {
        position: relative;
        display: flex;
        flex-direction: column;
      }

      .task-description-input {
        border-radius: 12px 12px 8px 8px !important;
      }

      .priority-suggestion {
        position: absolute;
        top: 100%;
        left: 0;
        right: 0;
        background: linear-gradient(135deg, #fef3c7, #fef9e7);
        border: 1px solid #f59e0b;
        border-top: none;
        border-radius: 0 0 12px 12px;
        padding: 0.75rem;
        box-shadow: 0 4px 12px rgba(245, 158, 11, 0.15);
        z-index: 100;
        animation: slideDown 0.2s ease;
      }

      .suggestion-loading {
        background: linear-gradient(135deg, #e0f2fe, #f0f9ff);
        border-color: #0ea5e9;
      }

      .suggestion-content {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 0.5rem;
      }

      .suggestion-label {
        font-size: 0.75rem;
        font-weight: 600;
        color: #92400e;
        text-transform: uppercase;
        letter-spacing: 0.05em;
      }

      .suggestion-priority {
        font-size: 0.8rem;
        font-weight: 700;
        padding: 0.2rem 0.5rem;
        border-radius: 6px;
      }

      .suggestion-priority.high {
        background: #fee2e2;
        color: #b91c1c;
      }

      .suggestion-priority.medium {
        background: #fef3c7;
        color: #92400e;
      }

      .suggestion-priority.low {
        background: #dbeafe;
        color: #1d4ed8;
      }

      .suggestion-confidence {
        font-size: 0.75rem;
        color: #64748b;
        font-weight: 600;
      }

      .apply-suggestion-btn {
        background: linear-gradient(135deg, #059669, #047857);
        color: white;
        border: none;
        padding: 0.3rem 0.6rem;
        border-radius: 6px;
        font-size: 0.75rem;
        font-weight: 600;
        cursor: pointer;
        margin-left: auto;
        transition: opacity 0.2s ease;
      }

      .apply-suggestion-btn:hover {
        opacity: 0.9;
      }

      .suggestion-reasoning {
        font-size: 0.75rem;
        color: #64748b;
        line-height: 1.4;
        padding-top: 0.25rem;
        border-top: 1px solid rgba(148, 163, 184, 0.2);
      }

      /* ── Priority Classes for Prediction Values ── */
      .prediction-value.high {
        color: #dc2626;
      }

      .prediction-value.medium {
        color: #d97706;
      }

      .prediction-value.low {
        color: #2563eb;
      }

      @keyframes slideDown {
        from {
          opacity: 0;
          transform: translateY(-8px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }

      @keyframes pulse {
        0%, 100% {
          opacity: 1;
        }
        50% {
          opacity: 0.7;
        }
      }

      @keyframes drift {
        0% { transform: translate3d(0, 0, 0) scale(1); }
        50% { transform: translate3d(-8px, 8px, 0) scale(1.06); }
        100% { transform: translate3d(0, 0, 0) scale(1); }
      }

      @keyframes ping {
        0% { box-shadow: 0 0 0 0 rgba(37, 99, 235, 0.35); }
        70% { box-shadow: 0 0 0 12px rgba(37, 99, 235, 0); }
        100% { box-shadow: 0 0 0 0 rgba(37, 99, 235, 0); }
      }

      @keyframes fadeIn {
        from { opacity: 0; transform: translateY(6px); }
        to { opacity: 1; transform: translateY(0); }
      }

      @keyframes toastIn {
        from { opacity: 0; transform: translateY(-6px); }
        to { opacity: 1; transform: translateY(0); }
      }

      @media (max-width: 1100px) {
        .stats-grid,
        .filter-grid,
        .composer,
        .content-grid,
        .profile-grid,
        .prediction-grid {
          grid-template-columns: 1fr 1fr;
        }
      }

      @media (max-width: 720px) {
        :host {
          padding: 0.75rem;
        }

        .topbar,
        .section-title {
          flex-direction: column;
          align-items: flex-start;
        }

        .stats-grid,
        .filter-grid,
        .composer,
        .content-grid,
        .profile-grid,
        .prediction-grid {
          grid-template-columns: 1fr;
        }

        .prediction-header {
          flex-direction: column;
          align-items: flex-start;
          gap: 0.5rem;
        }

        .priority-suggestion {
          position: relative;
          top: 0;
          margin-top: 0.5rem;
          border-top: 1px solid #f59e0b;
          border-radius: 12px;
        }

        .task-description-input {
          border-radius: 12px !important;
        }
      }
    `
  ]
})
export class StaffTasksComponent implements OnInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly staffService = inject(StaffService);
  private readonly signalRService = inject(StaffSignalRService);
  private taskDescriptionSubject = new Subject<string>();

  tasks: TaskSummary[] = [];
  tickets: TicketSummary[] = [];
  activityEvents: ActivityEvent[] = [];
  statusFilter = '';

  // Prediction Panel Properties
  forecastLoading = false;
  priorityAnalysisLoading = false;
  staffingForecast: StaffingForecast | null = null;
  inventoryForecast: InventoryForecast | null = null;
  operationsForecast: OperationsForecast | null = null;
  prioritySuggestion: PrioritySuggestion | null = null;

  get openTicketCount(): number {
    return this.tickets.filter(t => t.status !== 'Resolved').length;
  }
  priorityFilter = '';
  sortField = 'priority';
  showOnlyOpen = false;
  showProfileEditor = false;
  toastMessage = '';
  private toastTimeoutId: number | null = null;
  profileForm = {
    fullName: '',
    department: '',
    role: 'FrontDesk'
  };
  newTask = {
    description: '',
    roomNumber: '',
    type: 'Housekeeping',
    slaMinutes: 20,
    assignedTo: ''
  };

  staffRoster: StaffAssignee[] = [
    { name: 'Morgan Ellis', department: 'Operations', role: 'Manager' },
    { name: 'Dana Reyes', department: 'Front Desk', role: 'FrontDesk' }
  ];

  currentStaffName = 'Dana Reyes';
  currentStaffDepartment = 'Front Desk';
  currentStaffRoles: string[] = ['Staff', 'FrontDesk'];

  get openCount(): number {
    return this.tasks.filter((task) => task.status !== 'Completed').length;
  }

  get highPriorityCount(): number {
    return this.tasks.filter((task) => task.priority === 'High').length;
  }

  get assignedToCurrentUserCount(): number {
    return this.tasks.filter((task) => task.assignedTo === this.currentStaffName).length;
  }

  ngOnInit(): void {
    this.syncCurrentStaffProfile();
    this.refreshTasks();
    this.loadStaffProfile();
    this.loadTickets();
    this.loadForecasts();
    
    // Setup debounced priority analysis
    this.taskDescriptionSubject.pipe(
      debounceTime(800),
      distinctUntilChanged()
    ).subscribe(description => {
      if (description.length > 10) {
        this.analyzePriority(description);
      } else {
        this.prioritySuggestion = null;
      }
    });

    this.signalRService.connect().then(() => {
      this.signalRService.tasks$.subscribe((tasks) => {
        this.tasks = tasks;
      });
      this.signalRService.activity$.subscribe((events) => {
        this.activityEvents = events;
      });
      // Live ticket updates when a guest escalates in chat
      this.signalRService.tickets$.subscribe((tickets) => {
        this.tickets = tickets;
      });
    });
  }

  loadTickets(): void {
    this.staffService.getTickets().subscribe({
      next: (tickets) => { this.tickets = tickets; },
      error: () => { this.tickets = []; }
    });
  }

  resolveTicket(ticketId: string): void {
    this.staffService.resolveTicket(ticketId).subscribe({
      next: (updated) => {
        this.tickets = this.tickets.map(t => t.id === ticketId ? { ...t, status: updated.status } : t);
        this.showToast('Ticket resolved.');
      },
      error: () => { this.showToast('Could not resolve ticket.'); }
    });
  }

  get filteredTasks(): TaskSummary[] {
    const filtered = this.tasks.filter((task) => {
      const matchesStatus = !this.statusFilter || task.status === this.statusFilter;
      const matchesPriority = !this.priorityFilter || task.priority === this.priorityFilter;
      const matchesOpen = !this.showOnlyOpen || task.status !== 'Completed';
      const visibleForCurrentUser = true;
      return matchesStatus && matchesPriority && matchesOpen && visibleForCurrentUser;
    });

    return filtered.sort((a, b) => {
      if (this.sortField === 'description') {
        return a.description.localeCompare(b.description);
      }
      if (this.sortField === 'createdAt') {
        return (a.createdAt ?? '').localeCompare(b.createdAt ?? '');
      }
      const priorityOrder = { High: 0, Medium: 1, Low: 2 } as Record<string, number>;
      return priorityOrder[a.priority] - priorityOrder[b.priority];
    });
  }

  refreshTasks(): void {
    this.staffService.getTasks().subscribe((result: TaskSummary[]) => {
      this.tasks = result.map((task) => this.normalizeTask(task));
    });
  }

  createTask(): void {
    if (!this.newTask.description.trim()) {
      return;
    }

    this.staffService.createTask({
      description: this.newTask.description,
      type: this.newTask.type,
      roomNumber: this.newTask.roomNumber || undefined,
      slaMinutes: this.newTask.slaMinutes,
      assignedTo: this.newTask.assignedTo || this.currentStaffName,
      department: this.currentStaffDepartment
    }).subscribe(() => {
      this.newTask = { description: '', roomNumber: '', type: 'Housekeeping', slaMinutes: 20, assignedTo: '' };
      this.refreshTasks();
    });
  }

  ngOnDestroy(): void {
    if (this.toastTimeoutId) {
      window.clearTimeout(this.toastTimeoutId);
    }
    this.taskDescriptionSubject.complete();
  }

  completeTask(taskId: string): void {
    this.staffService.completeTask(taskId).subscribe((updatedTask) => {
      this.tasks = this.tasks.map((task) => task.id === taskId ? this.normalizeTask(updatedTask) : task);
      this.showToast('Task completed');
      this.refreshTasks();
    });
  }

  toggleTaskStatus(task: TaskSummary): void {
    const nextStatus = task.status === 'Completed' ? 'Pending' : 'Completed';

    this.staffService.updateTaskStatus(task.id, nextStatus).subscribe((updatedTask) => {
      this.tasks = this.tasks.map((item) => item.id === task.id ? this.normalizeTask(updatedTask) : item);
      this.showToast(nextStatus === 'Completed' ? 'Task marked complete' : 'Task marked open');
      this.refreshTasks();
    });
  }

  toggleProfileEditor(): void {
    this.showProfileEditor = !this.showProfileEditor;
  }

  saveProfile(): void {
    if (!this.profileForm.fullName.trim()) {
      return;
    }

    this.staffService.updateProfile({
      fullName: this.profileForm.fullName.trim(),
      department: this.profileForm.department.trim() || this.currentStaffDepartment,
      role: this.profileForm.role
    }).subscribe((profile) => {
      this.currentStaffName = profile.fullName || this.currentStaffName;
      this.currentStaffDepartment = profile.department || this.currentStaffDepartment;
      this.profileForm = {
        fullName: this.currentStaffName,
        department: this.currentStaffDepartment,
        role: profile.role || this.profileForm.role
      };
      this.newTask.assignedTo = this.currentStaffName;
      this.auth.updateCurrentUserProfile(this.currentStaffName, profile.role, this.currentStaffDepartment);
      this.syncCurrentStaffProfile();
      this.refreshTasks();
      this.showProfileEditor = false;
      this.showToast('Saved successfully');
    });
  }

  signOff(): void {
    this.auth.logout();
    window.location.href = '/staff/login';
  }

  getTaskTimer(task: TaskSummary): string {
    if (task.status === 'Completed') {
      return 'Completed';
    }

    const createdAt = new Date(task.createdAt ?? Date.now()).getTime();
    const deadline = createdAt + (task.slaMinutes ?? 20) * 60 * 1000;
    const remainingMs = deadline - Date.now();

    if (remainingMs <= 0) {
      return 'Overdue';
    }

    const minutes = Math.floor(remainingMs / 60000);
    const seconds = Math.floor((remainingMs % 60000) / 1000);
    return `${minutes}:${String(seconds).padStart(2, '0')}`;
  }

  isOverdue(task: TaskSummary): boolean {
    if (task.status === 'Completed') {
      return false;
    }

    const createdAt = new Date(task.createdAt ?? Date.now()).getTime();
    const deadline = createdAt + (task.slaMinutes ?? 20) * 60 * 1000;
    return Date.now() > deadline;
  }

  getDepartmentLabel(assignedTo?: string): string {
    const staff = this.staffRoster.find((item) => item.name === assignedTo);
    return staff?.department ?? this.currentStaffDepartment;
  }

  private normalizeTask(task: TaskSummary): TaskSummary {
    const normalizedStatus = this.normalizeStatus(task.status);
    const normalizedPriority = this.normalizePriority(task.priority);

    return {
      ...task,
      status: normalizedStatus,
      priority: normalizedPriority,
      assignedTo: task.assignedTo || this.currentStaffName,
      department: this.getDepartmentLabel(task.assignedTo)
    };
  }

  private normalizeStatus(status: string | number | undefined): string {
    if (status === 'Completed' || status === 2) {
      return 'Completed';
    }

    if (status === 'Pending' || status === 0) {
      return 'Pending';
    }

    return 'Pending';
  }

  private normalizePriority(priority: string | number | undefined): string {
    if (priority === 'High' || priority === 2 || priority === 'Critical' || priority === 3) {
      return 'High';
    }

    if (priority === 'Medium' || priority === 1) {
      return 'Medium';
    }

    return 'Low';
  }

  private showToast(message: string): void {
    if (this.toastTimeoutId) {
      window.clearTimeout(this.toastTimeoutId);
    }

    this.toastMessage = message;
    this.toastTimeoutId = window.setTimeout(() => {
      this.toastMessage = '';
      this.toastTimeoutId = null;
    }, 2500);
  }

  private loadStaffProfile(): void {
    this.staffService.getProfile().subscribe((profile) => {
      this.currentStaffName = profile.fullName || this.currentStaffName;
      this.currentStaffDepartment = profile.department || this.currentStaffDepartment;
      this.profileForm = {
        fullName: this.currentStaffName,
        department: this.currentStaffDepartment,
        role: profile.role || this.profileForm.role
      };
      this.currentStaffRoles = profile.role ? ['Staff', profile.role] : this.currentStaffRoles;
      this.auth.updateCurrentUserProfile(this.currentStaffName, profile.role, this.currentStaffDepartment);
      this.newTask.assignedTo = this.currentStaffName;
    });
  }

  private syncCurrentStaffProfile(): void {
    const storedUser = localStorage.getItem('hospitality.auth.user');
    const parsedUser = storedUser ? JSON.parse(storedUser) : null;
    const currentName = parsedUser?.name || 'Dana Reyes';
    const roles = parsedUser?.roles || ['Staff', 'FrontDesk'];

    this.currentStaffName = currentName;
    this.currentStaffRoles = roles;
    this.currentStaffDepartment = parsedUser?.department || this.currentStaffDepartment;

    const profile = this.staffRoster.find((staff) => staff.name === currentName);
    this.currentStaffDepartment = this.currentStaffDepartment || profile?.department || 'Front Desk';
    this.profileForm = {
      fullName: this.currentStaffName,
      department: this.currentStaffDepartment,
      role: parsedUser?.role || this.profileForm.role
    };
    this.newTask.assignedTo = currentName;
  }

  private isManagerLoggedIn(): boolean {
    return this.currentStaffRoles.includes('Manager');
  }

  // Prediction Panel Methods
  loadForecasts(): void {
    this.forecastLoading = true;
    
    // Load staffing forecast
    this.staffService.getStaffingForecast().subscribe({
      next: (forecast) => {
        this.staffingForecast = forecast;
      },
      error: () => {
        this.staffingForecast = {
          recommendedStaff: 8,
          reasoning: 'Based on current occupancy and historical patterns'
        };
      }
    });

    // Load operations forecast  
    this.staffService.getOperationsForecast().subscribe({
      next: (forecast) => {
        this.operationsForecast = forecast;
        this.forecastLoading = false;
      },
      error: () => {
        this.operationsForecast = {
          expectedTasks: 15,
          period: 'Next 4 hours prediction'
        };
        this.forecastLoading = false;
      }
    });

    // Load inventory forecast
    this.inventoryForecast = {
      criticalItems: 2,
      topItem: 'Toiletries and towels running low'
    };
  }

  onTaskDescriptionChange(event: any): void {
    const description = event.target.value;
    this.taskDescriptionSubject.next(description);
  }

  analyzePriority(description: string): void {
    this.priorityAnalysisLoading = true;
    
    this.staffService.getPriorityRecommendation(description).subscribe({
      next: (recommendation) => {
        this.prioritySuggestion = {
          priority: recommendation.Priority,
          confidence: 85, // Default confidence since API doesn't provide it
          reasoning: recommendation.Reason
        };
        this.priorityAnalysisLoading = false;
      },
      error: () => {
        // Fallback priority analysis
        const highPriorityKeywords = ['urgent', 'emergency', 'broken', 'not working', 'leak', 'safety'];
        const mediumPriorityKeywords = ['repair', 'maintenance', 'clean', 'replace'];
        
        const desc = description.toLowerCase();
        let priority = 'Low';
        let reasoning = 'Standard priority based on task description';
        
        if (highPriorityKeywords.some(keyword => desc.includes(keyword))) {
          priority = 'High';
          reasoning = 'Contains urgent keywords suggesting immediate attention needed';
        } else if (mediumPriorityKeywords.some(keyword => desc.includes(keyword))) {
          priority = 'Medium';
          reasoning = 'Maintenance-related task requiring timely completion';
        }
        
        this.prioritySuggestion = {
          priority,
          confidence: 75,
          reasoning
        };
        this.priorityAnalysisLoading = false;
      }
    });
  }

  applyPrioritySuggestion(): void {
    if (this.prioritySuggestion) {
      // Apply the suggested priority to the new task
      // Since we don't have a priority field in newTask, we'll update the SLA based on priority
      switch (this.prioritySuggestion.priority) {
        case 'High':
          this.newTask.slaMinutes = 15;
          break;
        case 'Medium':
          this.newTask.slaMinutes = 60;
          break;
        case 'Low':
          this.newTask.slaMinutes = 240;
          break;
      }
      this.showToast(`Applied ${this.prioritySuggestion.priority} priority (${this.newTask.slaMinutes}min SLA)`);
    }
  }

  getPriorityClass(priority: string | undefined): string {
    if (!priority) return '';
    return priority.toLowerCase();
  }
}
