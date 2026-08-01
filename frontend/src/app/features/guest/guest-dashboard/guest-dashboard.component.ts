import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GuestService } from '../../../core/services/guest.service';
import { AuthService } from '../../../core/auth/auth.service';
import { GuestBotWidgetComponent } from '../guest-bot-widget/guest-bot-widget.component';
import { GuestTicketSummary, RecommendationItem } from '../../../core/models/app.models';

@Component({
  selector: 'app-guest-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, GuestBotWidgetComponent],
  template: `
    <div class="page-shell">
      <header class="topbar">
        <div class="brand">
          <span class="brand-mark">✦</span>
          <div>
            <h1>StaySmart</h1>
            <p>Your personalized hotel experience.</p>
          </div>
        </div>
        <button type="button" class="logout-btn" (click)="logout()">Log out</button>
      </header>

      <main class="hero">
        <section class="dashboard-card">
          <div class="hero-copy">
            <p class="eyebrow">Your stay essentials</p>
            <h2>Welcome back</h2>
            <p class="description">
              From dining and wellness to airport transfers and local experiences, your stay can be curated with a few taps.
            </p>

            <div class="stats-grid">
              <div class="stat-pill">
                <span class="stat-label">Arrival</span>
                <strong>Checked in</strong>
              </div>
              <div class="stat-pill">
                <span class="stat-label">Services</span>
                <strong>6 Hotels essentials</strong>
              </div>
              <div class="stat-pill">
                <span class="stat-label">Support</span>
                <strong>Chat concierge</strong>
              </div>
            </div>
          </div>

          <div class="content-panel">
            <div class="list-header">
              <div class="agent-header">
                <h3>AI concierge thinking</h3>
                <button type="button" class="collapse-btn" (click)="toggleAgentPanel()">
                  {{ isAgentPanelOpen ? 'Hide' : 'Show' }}
                </button>
              </div>
              <span>Personalised by AI concierge</span>
            </div>

            <div class="agent-panel" [class.closed]="!isAgentPanelOpen">
              <p *ngIf="!loadingRecommendations && greeting" class="rec-greeting">{{ greeting }}</p>

              <div *ngIf="loadingRecommendations" class="rec-loading">
                <div class="rec-spinner"></div>
                <span>Fetching your personalised recommendations…</span>
              </div>

              <ul *ngIf="!loadingRecommendations" class="recommendations-list">
                <li *ngFor="let item of recommendations" class="rec-card">
                  <div class="rec-card-inner">
                    <span class="rec-category" [attr.data-cat]="item.category">{{ item.category }}</span>
                    <strong class="rec-title">{{ item.title }}</strong>
                    <p class="rec-body">{{ item.description }}</p>
                  </div>
                </li>
              </ul>
            </div>
          </div>
        </section>

        <section class="services-section">
          <div class="section-heading">
            <div>
              <p class="eyebrow">Make the Most of Your Stay</p>
              <h3>Discover services and experiences designed just for you.</h3>
            </div>
            <span class="section-note">We're Here for You!</span>
          </div>

          <div class="booking-modal-backdrop" *ngIf="isBookingModalOpen" (click)="closeBookingModal()">
            <div class="booking-modal" role="dialog" aria-modal="true" (click)="$event.stopPropagation()">
              <div class="modal-header">
                <div>
                  <p class="eyebrow">Booking request</p>
                  <h4>{{ activeBookingServiceTitle }}</h4>
                </div>
                <button type="button" class="modal-close" (click)="closeBookingModal()">✕</button>
              </div>

              <div class="modal-body">
                <label class="booking-field">
                  <span>Start time</span>
                  <input type="datetime-local" [(ngModel)]="bookingDraft.startTime" name="bookingStart" />
                </label>
              </div>

              <div class="modal-actions">
                <button type="button" class="secondary-btn" (click)="closeBookingModal()">Cancel</button>
                <button type="button" class="book-btn" (click)="confirmBooking()">Book request</button>
              </div>
            </div>
          </div>

          <div class="services-grid">
            <article class="service-card" *ngFor="let service of hotelServices">
              <div class="service-icon">{{ service.icon }}</div>
              <h4>{{ service.title }}</h4>
              <p>{{ service.description }}</p>

              <div class="booking-actions">
                <button type="button" class="book-btn" (click)="openBookingModal(service.title)" [disabled]="getBookingState(service.title).isSubmitting">
                  {{ getBookingState(service.title).isSubmitting ? 'Sending…' : 'Book now' }}
                </button>
                <p class="booking-feedback" [class.success]="getBookingState(service.title).feedbackType === 'success'" [class.error]="getBookingState(service.title).feedbackType === 'error'">
                  {{ getBookingState(service.title).feedback }}
                </p>
              </div>
            </article>
          </div>
        </section>

        <section class="tickets-section">
          <div class="section-heading">
            <div>
              <p class="eyebrow">We've Got It Covered</p>
              <h3>Your Service Requests</h3>
            </div>
          </div>

          <div class="tickets-list" *ngIf="guestTickets.length; else noTickets">
            <article class="ticket-item" *ngFor="let ticket of guestTickets">
              <div class="ticket-head">
                <span class="ticket-status">{{ ticket.status }}</span>
                <!-- <span class="ticket-created-by">Created by: {{ ticket.createdBy || 'Guest' }}</span>-->
              </div>
              <strong>{{ ticket.message }}</strong>
              <p class="ticket-priority-reason" *ngIf="ticket.priorityReason">Tracking reason: {{ ticket.priorityReason }}</p>
              <p class="ticket-remark" *ngIf="ticket.remark">Remark: {{ ticket.remark }}</p>
              <div class="ticket-meta">
                <span>Room {{ ticket.roomNumber || '—' }}</span>
                <span>{{ ticket.createdAt | date:'medium' }}</span>
              </div>
            </article>
          </div>

          <ng-template #noTickets>
            <div class="empty-ticket-state">
              No guest-created tickets yet. Booking a service will add one here for your reference.
            </div>
          </ng-template>
        </section>

        <section class="journey-section">
          <div class="section-heading">
            <div>
              <p class="eyebrow">End-to-end flow</p>
              <h3>How the guest journey works</h3>
            </div>
          </div>

          <div class="journey-grid">
            <div class="journey-step" *ngFor="let step of journeySteps; let i = index">
              <span class="step-number">0{{ i + 1 }}</span>
              <strong>{{ step.title }}</strong>
              <p>{{ step.description }}</p>
            </div>
          </div>
        </section>
      </main>

      <footer class="footer">
        <span>Need help? Contact the concierge desk.</span>
        <span>Secure • Thoughtful • Tailored</span>
        <app-guest-bot-widget></app-guest-bot-widget>
      </footer>

      
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-height: 100vh;
        background: linear-gradient(135deg, #f7f9ff 0%, #eef5ff 65%, #f8fbff 100%);
        color: #1f2937;
        font-family: Inter, 'Segoe UI', sans-serif;
      }

      .page-shell {
        min-height: 100vh;
        display: flex;
        flex-direction: column;
      }

      .topbar {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1.25rem 2rem;
        background: rgba(255, 255, 255, 0.84);
        backdrop-filter: blur(12px);
        border-bottom: 1px solid rgba(15, 23, 42, 0.08);
      }

      .brand {
        display: flex;
        align-items: center;
        gap: 0.85rem;
      }

      .brand-mark {
        display: grid;
        place-items: center;
        width: 2.5rem;
        height: 2.5rem;
        border-radius: 999px;
        background: linear-gradient(135deg, #2563eb, #7c3aed);
        color: white;
        font-size: 1.1rem;
      }

      .brand h1 {
        margin: 0;
        font-size: 1rem;
        font-weight: 700;
      }

      .brand p {
        margin: 0;
        font-size: 0.8rem;
        color: #64748b;
      }

      .logout-btn {
        border: none;
        border-radius: 999px;
        padding: 0.7rem 1rem;
        background: linear-gradient(135deg, #ef4444, #dc2626);
        color: white;
        font-weight: 700;
        cursor: pointer;
      }

      .hero {
        flex: 1;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 1.5rem;
        padding: 2rem;
      }

      .dashboard-card {
        width: min(100%, 1080px);
        display: grid;
        grid-template-columns: 0.95fr 1.05fr;
        gap: 2rem;
        background: rgba(255, 255, 255, 0.94);
        border: 1px solid rgba(148, 163, 184, 0.24);
        border-radius: 24px;
        box-shadow: 0 24px 60px rgba(15, 23, 42, 0.12);
        overflow: hidden;
      }

      .hero-copy {
        padding: 3rem;
        background: linear-gradient(135deg, #eff6ff 0%, #f8fafc 100%);
        display: flex;
        flex-direction: column;
        justify-content: center;
      }

      .eyebrow {
        margin: 0 0 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.24em;
        font-size: 0.78rem;
        font-weight: 700;
        color: #4f46e5;
      }

      .hero-copy h2 {
        margin: 0 0 0.75rem;
        font-size: 2rem;
        color: #0f172a;
      }

      .description {
        margin: 0;
        color: #475569;
        line-height: 1.7;
      }

      .stats-grid {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 0.8rem;
        margin-top: 1.4rem;
      }

      .stat-pill {
        background: linear-gradient(135deg, rgba(255, 255, 255, 0.95), rgba(240, 247, 255, 0.95));
        border: 1px solid rgba(148, 163, 184, 0.24);
        border-radius: 16px;
        padding: 0.85rem 0.9rem;
        box-shadow: 0 10px 24px rgba(15, 23, 42, 0.04);
      }

      .stat-label {
        display: block;
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0.14em;
        color: #64748b;
        margin-bottom: 0.25rem;
      }

      .stat-pill strong {
        color: #0f172a;
        font-size: 0.96rem;
      }

      .content-panel {
        padding: 3rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .list-header,
      .section-heading {
        display: flex;
        justify-content: space-between;
        align-items: flex-end;
        gap: 1rem;
      }

      .header-copy {
        display: flex;
        flex-direction: column;
        gap: 0.3rem;
      }

      .title-row {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        flex-wrap: wrap;
      }

      .list-header h3,
      .section-heading h3 {
        margin: 0;
        color: #0f172a;
      }

      .agent-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        width: 100%;
      }

      .collapse-btn {
        border: none;
        background: linear-gradient(135deg, #2563eb, #4f46e5);
        color: white;
        border-radius: 999px;
        padding: 0.4rem 0.8rem;
        font-size: 0.78rem;
        font-weight: 700;
        cursor: pointer;
      }

      .agent-panel {
        display: flex;
        flex-direction: column;
        gap: 0.85rem;
        overflow: hidden;
        transition: max-height 0.25s ease, opacity 0.2s ease;
        max-height: 800px;
        opacity: 1;
      }

      .agent-panel.closed {
        max-height: 0;
        opacity: 0;
      }

      .list-header span,
      .section-note {
        color: #64748b;
        font-size: 0.9rem;
      }

      .live-pill {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        padding: 0.3rem 0.6rem;
        border-radius: 999px;
        background: #e0f2fe;
        color: #0369a1;
        font-size: 0.74rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.12em;
      }

      .live-pill.live-ready {
        background: #dcfce7;
        color: #166534;
      }

      .location-banner {
        display: flex;
        align-items: flex-start;
        gap: 0.75rem;
        padding: 0.9rem 1rem;
        border-radius: 16px;
        background: linear-gradient(135deg, #f8fbff 0%, #eef6ff 100%);
        border: 1px solid rgba(59, 130, 246, 0.18);
        box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.6);
      }

      .location-banner.loading {
        background: linear-gradient(135deg, #fcfdff 0%, #f5f9ff 100%);
      }

      .location-icon {
        width: 2.2rem;
        height: 2.2rem;
        border-radius: 999px;
        display: grid;
        place-items: center;
        background: linear-gradient(135deg, #dbeafe, #ede9fe);
        font-size: 1rem;
      }

      .location-banner strong {
        display: block;
        margin-bottom: 0.2rem;
        color: #0f172a;
      }

      .location-banner p {
        margin: 0;
        color: #64748b;
        line-height: 1.5;
      }

      .recommendations-list {
        list-style: none;
        padding: 0;
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: 0.85rem;
      }

      .recommendations-list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
      }

      .rec-greeting {
        font-size: 0.95rem;
        color: #4f46e5;
        font-style: italic;
        margin: 0 0 1rem;
        padding: 0.75rem 1rem;
        background: linear-gradient(135deg, #eef2ff, #f5f3ff);
        border-left: 3px solid #6366f1;
        border-radius: 0 10px 10px 0;
        line-height: 1.55;
      }

      .rec-card {
        background: linear-gradient(135deg, #ffffff 0%, #f8faff 100%);
        border: 1px solid #e0e7ff;
        border-radius: 16px;
        overflow: hidden;
        transition: transform 0.18s ease, box-shadow 0.18s ease;
        box-shadow: 0 2px 8px rgba(79, 70, 229, 0.06);
      }

      .rec-card:hover {
        transform: translateY(-2px);
        box-shadow: 0 8px 24px rgba(79, 70, 229, 0.14);
      }

      .rec-card-inner {
        padding: 1rem 1.1rem;
        display: flex;
        flex-direction: column;
        gap: 0.3rem;
        border-left: 4px solid #6366f1;
      }

      .rec-category {
        display: inline-block;
        font-size: 0.68rem;
        font-weight: 700;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        padding: 2px 9px;
        border-radius: 999px;
        background: #ede9fe;
        color: #6d28d9;
        width: fit-content;
      }

      .rec-category[data-cat="Dining"]           { background: #fef3c7; color: #92400e; }
      .rec-category[data-cat="Spa & Wellness"]    { background: #d1fae5; color: #065f46; }
      .rec-category[data-cat="Activities"]        { background: #dbeafe; color: #1e40af; }
      .rec-category[data-cat="Room Service"]      { background: #fce7f3; color: #9d174d; }
      .rec-category[data-cat="Local Experience"]  { background: #e0f2fe; color: #0c4a6e; }

      .rec-title {
        font-size: 0.95rem;
        font-weight: 700;
        color: #111827;
        line-height: 1.3;
      }

      .rec-body {
        margin: 0;
        font-size: 0.85rem;
        color: #4b5563;
        line-height: 1.6;
      }

      .rec-loading {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        color: #6b7280;
        font-size: 0.9rem;
        padding: 1.25rem 0;
      }

      .rec-spinner {
        width: 18px;
        height: 18px;
        border: 2px solid #e5e7eb;
        border-top-color: #6366f1;
        border-radius: 50%;
        animation: spin 0.7s linear infinite;
        flex-shrink: 0;
      }

      @keyframes spin {
        to { transform: rotate(360deg); }
      }

      .services-section,
      .journey-section,
      .tickets-section {
        width: min(100%, 1080px);
        background: rgba(255, 255, 255, 0.9);
        border: 1px solid rgba(148, 163, 184, 0.24);
        border-radius: 24px;
        padding: 1.75rem;
        box-shadow: 0 18px 50px rgba(15, 23, 42, 0.08);
      }

      .services-grid,
      .tickets-list,
      .journey-grid {
        margin-top: 1.25rem;
        display: grid;
        gap: 1rem;
      }

      .services-grid {
        grid-template-columns: repeat(3, minmax(0, 1fr));
      }

      .ticket-item {
        background: linear-gradient(135deg, #f8fbff 0%, #eef5ff 100%);
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 18px;
        padding: 1rem;
        display: flex;
        flex-direction: column;
        gap: 0.6rem;
      }

      .ticket-head,
      .ticket-meta {
        display: flex;
        justify-content: space-between;
        gap: 0.8rem;
        align-items: center;
        flex-wrap: wrap;
      }

      .ticket-status {
        display: inline-flex;
        align-items: center;
        border-radius: 999px;
        background: #dbeafe;
        color: #1d4ed8;
        font-size: 0.75rem;
        font-weight: 700;
        padding: 0.35rem 0.7rem;
      }

      .ticket-created-by,
      .ticket-meta {
        font-size: 0.78rem;
        color: #64748b;
      }

      .ticket-item strong {
        color: #0f172a;
      }

      .ticket-remark {
        margin: 0;
        color: #475569;
        line-height: 1.6;
      }

      .empty-ticket-state {
        margin-top: 1rem;
        border: 1px dashed rgba(148, 163, 184, 0.8);
        border-radius: 16px;
        padding: 1rem;
        color: #64748b;
        background: rgba(248, 250, 252, 0.7);
      }

      .service-card,
      .journey-step {
        background: linear-gradient(135deg, #fcfdff 0%, #f5f9ff 100%);
        border: 1px solid rgba(15, 23, 42, 0.08);
        border-radius: 20px;
        padding: 1rem;
        box-shadow: 0 12px 30px rgba(15, 23, 42, 0.05);
        transition: transform 0.2s ease, box-shadow 0.2s ease, border-color 0.2s ease, background 0.2s ease;
        position: relative;
        overflow: hidden;
      }

      .service-card {
        display: flex;
        flex-direction: column;
        min-height: 100%;
      }

      .service-card::before,
      .journey-step::before {
        content: '';
        position: absolute;
        inset: 0;
        background: linear-gradient(120deg, rgba(59, 130, 246, 0.06), transparent 55%);
        pointer-events: none;
      }

      .service-card:hover,
      .journey-step:hover {
        transform: translateY(-4px);
        border-color: rgba(37, 99, 235, 0.45);
        box-shadow: 0 18px 40px rgba(37, 99, 235, 0.16);
        background: linear-gradient(135deg, #f8fbff 0%, #eef6ff 100%);
      }

      .service-icon {
        width: 2.6rem;
        height: 2.6rem;
        display: grid;
        place-items: center;
        border-radius: 12px;
        background: linear-gradient(135deg, #dbeafe, #ede9fe);
        font-size: 1.2rem;
        margin-bottom: 0.85rem;
      }

      .service-card h4,
      .journey-step strong {
        margin: 0 0 0.4rem;
        color: #0f172a;
      }

      .service-card p,
      .journey-step p {
        margin: 0;
        color: #64748b;
        line-height: 1.6;
      }

      .booking-actions {
        margin-top: auto;
        padding-top: 1rem;
        display: flex;
        flex-direction: column;
        gap: 0.7rem;
      }

      .booking-field {
        display: flex;
        flex-direction: column;
        gap: 0.3rem;
      }

      .booking-field span {
        font-size: 0.76rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.14em;
        color: #64748b;
      }

      .booking-field input {
        width: 100%;
        box-sizing: border-box;
        border: 1px solid rgba(148, 163, 184, 0.35);
        border-radius: 12px;
        padding: 0.7rem 0.8rem;
        font: inherit;
        background: rgba(255, 255, 255, 0.9);
      }

      .book-btn,
      .secondary-btn,
      .modal-close {
        border: none;
        border-radius: 999px;
        padding: 0.7rem 0.95rem;
        font-weight: 700;
        cursor: pointer;
      }

      .book-btn {
        background: linear-gradient(135deg, #2563eb, #4f46e5);
        color: white;
      }

      .secondary-btn {
        background: #e2e8f0;
        color: #334155;
      }

      .modal-close {
        background: transparent;
        padding: 0.3rem 0.55rem;
        color: #475569;
      }

      .book-btn:disabled {
        opacity: 0.75;
        cursor: wait;
      }

      .booking-feedback {
        min-height: 1.2rem;
        margin: 0;
        font-size: 0.82rem;
        color: #64748b;
      }

      .booking-feedback.success {
        color: #166534;
      }

      .booking-feedback.error {
        color: #b91c1c;
      }

      .booking-modal-backdrop {
        position: fixed;
        inset: 0;
        background: rgba(15, 23, 42, 0.62);
        display: grid;
        place-items: center;
        z-index: 1200;
        padding: 1rem;
      }

      .booking-modal {
        width: min(100%, 460px);
        max-width: 100%;
        box-sizing: border-box;
        background: white;
        border-radius: 24px;
        padding: 1.25rem;
        box-shadow: 0 24px 60px rgba(15, 23, 42, 0.2);
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .modal-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 0.75rem;
      }

      .modal-header h4 {
        margin: 0;
        color: #0f172a;
      }

      .modal-body {
        display: flex;
        flex-direction: column;
        gap: 0.8rem;
      }

      .modal-actions {
        display: flex;
        justify-content: flex-end;
        gap: 0.7rem;
      }

      .journey-grid {
        grid-template-columns: repeat(3, minmax(0, 1fr));
      }

      .step-number {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 2.2rem;
        height: 2.2rem;
        border-radius: 999px;
        background: #2563eb;
        color: white;
        font-size: 0.82rem;
        margin-bottom: 0.8rem;
      }

      .footer {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 1rem;
        flex-wrap: wrap;
        padding: 1rem 2rem 1.5rem;
        color: #64748b;
        font-size: 0.92rem;
      }

      .footer > span {
        white-space: nowrap;
      }

      .footer app-guest-bot-widget {
        margin-left: auto;
      }

      @media (max-width: 900px) {
        .dashboard-card,
        .services-grid,
        .journey-grid {
          grid-template-columns: 1fr;
        }

        .stats-grid {
          grid-template-columns: 1fr;
        }
      }

      @media (max-width: 800px) {
        .hero {
          padding: 1rem;
        }

        .dashboard-card {
          grid-template-columns: 1fr;
        }

        .topbar,
        .section-heading,
        .list-header {
          flex-direction: column;
          gap: 0.7rem;
          align-items: flex-start;
        }

        .footer {
          flex-direction: column;
          align-items: flex-start;
        }

        .footer app-guest-bot-widget {
          margin-left: 0;
          width: 100%;
        }

        .hero-copy,
        .content-panel,
        .services-section,
        .journey-section {
          padding: 1.5rem;
        }
      }
    `
  ]
})
export class GuestDashboardComponent implements OnInit {
  private readonly guestService = inject(GuestService);
  private readonly authService = inject(AuthService);
  recommendations: RecommendationItem[] = [];
  greeting = '';
  loadingRecommendations = true;
  isAgentPanelOpen = true;
  bookingStates: Record<string, { startTime: string; isSubmitting: boolean; feedback: string; feedbackType: 'idle' | 'success' | 'error' }> = {};
  guestTickets: GuestTicketSummary[] = [];
  isBookingModalOpen = false;
  activeBookingServiceTitle: string | null = null;
  bookingDraft = { startTime: '' };

  hotelServices = [
    {
      icon: '🍽️',
      title: 'Signature Dining',
      description: 'Reserve breakfast, chef-led tasting menus, and in-room dining with a direct concierge request.'
    },
    {
      icon: '🧖',
      title: 'Spa & Wellness',
      description: 'Book massage, sauna, and rejuvenation treatments designed for a luxury stay reset.'
    },
    {
      icon: '🚗',
      title: 'Airport Transfers',
      description: 'Arrange safe, seamless pickup and drop-off to keep arrival and departure effortless.'
    },
    {
      icon: '🏊',
      title: 'Pool & Lounge',
      description: 'Access the rooftop pool, cabanas, and social lounge spaces for a relaxed afternoon.'
    },
    {
      icon: '🛎️',
      title: 'Room Service',
      description: 'Order late-night snacks, premium beverages, and comfort essentials from your room.'
    },
    {
      icon: '📍',
      title: 'Local Experiences',
      description: 'Discover nearby landmarks, cultural stops, and curated city moments with one-click guidance.'
    }
  ];

  journeySteps = [
    {
      title: 'Log in with your reservation',
      description: 'Start from the reservation code experience and unlock your personalized stay profile.'
    },
    {
      title: 'Explore hotel services',
      description: 'Browse dining, spa, transfer, and room convenience options from one premium dashboard.'
    },
    {
      title: 'Chat with Nova for help',
      description: 'Continue the flow in chat to request recommendations, plan your day, and resolve service needs.'
    }
  ];

  toggleAgentPanel(): void {
    this.isAgentPanelOpen = !this.isAgentPanelOpen;
  }

  logout(): void {
    this.authService.logout();
    window.location.href = '/guest/login';
  }

  getBookingState(title: string) {
    if (!this.bookingStates[title]) {
      this.bookingStates[title] = {
        startTime: '',
        isSubmitting: false,
        feedback: '',
        feedbackType: 'idle'
      };
    }

    return this.bookingStates[title];
  }

  openBookingModal(serviceTitle: string): void {
    this.activeBookingServiceTitle = serviceTitle;
    const state = this.getBookingState(serviceTitle);
    this.bookingDraft = { startTime: state.startTime };
    this.isBookingModalOpen = true;
  }

  closeBookingModal(): void {
    this.isBookingModalOpen = false;
    this.activeBookingServiceTitle = null;
    this.bookingDraft = { startTime: '' };
  }

  confirmBooking(): void {
    if (!this.activeBookingServiceTitle) {
      return;
    }

    const service = this.hotelServices.find((entry) => entry.title === this.activeBookingServiceTitle);
    if (!service) {
      return;
    }

    const state = this.getBookingState(service.title);
    if (!this.bookingDraft.startTime) {
      state.feedback = 'Choose a start time for this request.';
      state.feedbackType = 'error';
      return;
    }

    state.startTime = this.bookingDraft.startTime;
    state.isSubmitting = true;
    state.feedback = '';
    state.feedbackType = 'idle';

    this.guestService.bookService({
      category: service.title,
      slotId: service.title.toLowerCase().replace(/[^a-z0-9]+/g, '-'),
      title: service.title,
      startTime: state.startTime
    }).subscribe({
      next: (response) => {
        state.isSubmitting = false;
        state.feedback = response.message || 'Your request has been forwarded to our concierge team.';
        state.feedbackType = response.success ? 'success' : 'error';
        this.closeBookingModal();
      },
      error: () => {
        state.isSubmitting = false;
        state.feedback = 'We could not submit that request right now. Please try again.';
        state.feedbackType = 'error';
        this.closeBookingModal();
      }
    });
  }

  ngOnInit(): void {
    this.loadingRecommendations = true;
    this.hotelServices.forEach((service) => this.getBookingState(service.title));
    this.guestService.getRecommendations().subscribe({
      next: (response) => {
        this.greeting = response.greeting ?? '';
        this.recommendations = response.recommendations?.length
          ? response.recommendations
          : this.getFallbackRecommendations();
        this.loadingRecommendations = false;
      },
      error: () => {
        this.recommendations = this.getFallbackRecommendations();
        this.loadingRecommendations = false;
      }
    });

    this.guestService.getMyTickets().subscribe({
      next: (tickets) => {
        this.guestTickets = tickets.map((ticket) => ({
          ...ticket,
          createdBy: ticket.createdBy || 'Guest',
          priorityReason: ticket.priorityReason || ticket.remark || 'Guest requested a service booking from the dashboard.',
          status: ticket.status || 'Open'
        }));
      },
      error: () => {
        this.guestTickets = [];
      }
    });
  }

  private getFallbackRecommendations(): RecommendationItem[] {
    return [
      { id: '1', category: 'Dining',           title: 'Signature dining experience',  description: 'Reserve a table at Olive Terrace for an unforgettable Mediterranean evening.' },
      { id: '2', category: 'Spa & Wellness',   title: 'Spa reset moment',             description: 'Enjoy a signature wellness experience with calming, low-light treatments.' },
      { id: '3', category: 'Local Experience', title: 'City highlights tour',          description: 'Ask the concierge for nearby museums, shopping, and cultural stops.' }
    ];
  }
}
