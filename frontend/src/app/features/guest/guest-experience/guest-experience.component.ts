import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { GuestService } from '../../../core/services/guest.service';
import { GuestBotWidgetComponent } from '../guest-bot-widget/guest-bot-widget.component';

@Component({
  selector: 'app-guest-experience',
  standalone: true,
  imports: [CommonModule, RouterModule, GuestBotWidgetComponent],
  template: `
    <div class="page-shell">
      <header class="topbar">
        <div class="brand">
          <span class="brand-mark">✦</span>
          <div>
            <h1>StaySmart</h1>
            <p>Guest experience</p>
          </div>
        </div>
        <nav class="nav-links">
          <a routerLink="/guest/dashboard">Dashboard</a>
          <a routerLink="/guest/chat">Chaat</a>
        </nav>
      </header>

      <main class="hero">
        <section class="experience-card">
          <div class="hero-panel">
            <div class="pill">Checked in • Welcome aboard</div>
            <h2>Your stay is now beautifully set up</h2>
            <p class="description">
              A softer, more elevated arrival experience with your essentials organized, recommendations ready, and Chaat support at your fingertips.
            </p>

            <div class="stats-grid">
              <div class="stat-card">
                <span class="stat-label">Arrival</span>
                <strong>Complete</strong>
              </div>
              <div class="stat-card">
                <span class="stat-label">Digital key</span>
                <strong>Active</strong>
              </div>
              <div class="stat-card">
                <span class="stat-label">Support</span>
                <strong>Online</strong>
              </div>
            </div>

            <div class="cta-row">
              <a class="primary-btn" routerLink="/guest/dashboard">See recommendations</a>
              <a class="secondary-btn" routerLink="/guest/chat">Open Chaat</a>
            </div>
          </div>

          <div class="showcase-panel">
            <div class="showcase-inner">
              <div class="hotel-preview" aria-hidden="true">
                <div class="preview-glow"></div>
                <div class="preview-window"></div>
                <div class="preview-lounge"></div>
                <div class="preview-lamp"></div>
                <div class="preview-badge">Luxury stay</div>
              </div>

              <div class="mini-card-list">
                <div class="mini-card">
                  <span>Tonight</span>
                  <strong>Signature lounge</strong>
                </div>
                <div class="mini-card">
                  <span>Quick help</span>
                  <strong>Room & dining support</strong>
                </div>
                <div class="mini-card">
                  <span>Recommended</span>
                  <strong>
                    <span *ngFor="let item of getRecommendationsList(); let last = last">
                      {{ item.title }}<span *ngIf="!last"> • </span>
                    </span>
                  </strong>
                </div>
              </div>
            </div>
          </div>
        </section>
      </main>

      <footer class="footer">
        <span>Fast • Thoughtful • Elevated</span>
        <span>Need anything else? Your Chaat is ready.</span>
      </footer>

      <app-guest-bot-widget></app-guest-bot-widget>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-height: 100vh;
        background: linear-gradient(135deg, #f8fbff 0%, #eef6ff 60%, #fdfbff 100%);
        color: #172033;
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
        background: rgba(255, 255, 255, 0.82);
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
        width: 2.6rem;
        height: 2.6rem;
        border-radius: 999px;
        background: linear-gradient(135deg, #60a5fa, #8b5cf6);
        color: white;
        font-size: 1.15rem;
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

      .nav-links {
        display: flex;
        gap: 1rem;
      }

      .nav-links a {
        text-decoration: none;
        color: #2563eb;
        font-weight: 600;
      }

      .hero {
        flex: 1;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 2rem;
      }

      .experience-card {
        width: min(100%, 980px);
        display: grid;
        grid-template-columns: 1.05fr 0.95fr;
        gap: 1rem;
        background: rgba(255,255,255,0.9);
        border: 1px solid rgba(148, 163, 184, 0.24);
        border-radius: 24px;
        box-shadow: 0 20px 50px rgba(15, 23, 42, 0.08);
        overflow: hidden;
      }

      .hero-panel {
        padding: 2.4rem;
        background: linear-gradient(135deg, #f8fbff, #eef5ff);
      }

      .pill {
        display: inline-flex;
        padding: 0.45rem 0.8rem;
        border-radius: 999px;
        background: rgba(37, 99, 235, 0.08);
        color: #123c92;
        font-size: 0.82rem;
        font-weight: 700;
        letter-spacing: 0.08em;
        text-transform: uppercase;
      }

      .hero-panel h2 {
        margin: 1rem 0 0.8rem;
        font-size: 2rem;
        line-height: 1.1;
        color: #0f172a;
      }

      .description {
        margin: 0 0 1.2rem;
        color: #475569;
        line-height: 1.75;
      }

      .stats-grid {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 0.85rem;
        margin-bottom: 1.2rem;
      }

      .stat-card {
        padding: 0.9rem;
        border-radius: 16px;
        background: rgba(37, 99, 235, 0.05);
        border: 1px solid rgba(37, 99, 235, 0.12);
      }

      .stat-label {
        display: block;
        margin-bottom: 0.35rem;
        color: #62748e;
        font-size: 0.78rem;
        text-transform: uppercase;
        letter-spacing: 0.08em;
      }

      .stat-card strong {
        font-size: 1rem;
        color: #0f172a;
      }

      .cta-row {
        display: flex;
        gap: 0.8rem;
        flex-wrap: wrap;
      }

      .primary-btn,
      .secondary-btn {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        padding: 0.9rem 1rem;
        border-radius: 999px;
        text-decoration: none;
        font-weight: 700;
      }

      .primary-btn {
        background: linear-gradient(135deg, #2563eb, #7c3aed);
        color: white;
      }

      .secondary-btn {
        color: #123c92;
        border: 1px solid rgba(37, 99, 235, 0.18);
        background: rgba(37, 99, 235, 0.06);
      }

      .showcase-panel {
        padding: 1.1rem;
        display: flex;
        align-items: stretch;
        background: linear-gradient(180deg, #f6f9ff, #ffffff);
      }

      .showcase-inner {
        width: 100%;
        display: grid;
        gap: 0.9rem;
      }

      .hotel-preview {
        position: relative;
        height: 220px;
        border-radius: 20px;
        border: 1px solid rgba(37, 99, 235, 0.12);
        overflow: hidden;
        background: linear-gradient(135deg, #10233d 0%, #234469 58%, #4b6f95 100%);
        box-shadow: inset 0 0 0 1px rgba(255,255,255,0.08);
      }

      .preview-glow {
        position: absolute;
        inset: 0;
        background: radial-gradient(circle at 18% 18%, rgba(255,255,255,0.28), transparent 26%);
      }

      .preview-window {
        position: absolute;
        top: 24px;
        left: 24px;
        width: 56%;
        height: 56%;
        border-radius: 18px;
        background: linear-gradient(135deg, rgba(255,255,255,0.24), rgba(255,255,255,0.06));
        border: 1px solid rgba(255,255,255,0.18);
        box-shadow: inset 0 0 0 8px rgba(255,255,255,0.05);
      }

      .preview-lounge {
        position: absolute;
        right: 24px;
        bottom: 22px;
        width: 38%;
        height: 45%;
        border-radius: 20px;
        background: linear-gradient(135deg, #e6d2a8, #bc8d50);
        box-shadow: inset 0 -10px 20px rgba(0,0,0,0.18);
      }

      .preview-lounge::before {
        content: '';
        position: absolute;
        inset: 14px;
        border-radius: 14px;
        border: 2px solid rgba(255,255,255,0.28);
      }

      .preview-lamp {
        position: absolute;
        left: 43%;
        bottom: 22px;
        width: 16px;
        height: 68px;
        border-radius: 999px;
        background: linear-gradient(180deg, #fef3c7, #d6a372);
        box-shadow: 0 0 24px rgba(254,243,199,0.18);
      }

      .preview-badge {
        position: absolute;
        top: 14px;
        right: 14px;
        padding: 0.42rem 0.68rem;
        border-radius: 999px;
        background: rgba(6, 12, 24, 0.76);
        color: #fef3c7;
        font-size: 0.74rem;
        font-weight: 700;
        letter-spacing: 0.08em;
        text-transform: uppercase;
      }

      .mini-card-list {
        width: 100%;
        display: grid;
        gap: 0.9rem;
      }

      .mini-card {
        padding: 1rem;
        border-radius: 16px;
        background: rgba(255,255,255,0.95);
        border: 1px solid rgba(148, 163, 184, 0.24);
        box-shadow: 0 10px 30px rgba(15, 23, 42, 0.06);
      }

      .mini-card span {
        display: block;
        color: #62748e;
        font-size: 0.78rem;
        text-transform: uppercase;
        letter-spacing: 0.08em;
        margin-bottom: 0.4rem;
      }

      .mini-card strong {
        display: inline-block;
        font-size: 0.98rem;
        color: #0f172a;
        line-height: 1.5;
      }

      .footer {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1rem 2rem 1.5rem;
        color: #56708f;
        font-size: 0.92rem;
      }

      @media (max-width: 900px) {
        .experience-card {
          grid-template-columns: 1fr;
        }

        .topbar,
        .footer {
          flex-direction: column;
          gap: 0.7rem;
          align-items: flex-start;
        }
      }
    `
  ]
})
export class GuestExperienceComponent {
  private readonly guestService = inject(GuestService);
  recommendations: Array<{ id: string; title: string; description: string; category: string }> = [];

  ngOnInit(): void {
    this.guestService.getRecommendations().subscribe((response) => {
      this.recommendations = response.recommendations ?? [];
    });
  }

  getRecommendationsList(): Array<{ id: string; title: string; description: string; category: string }> {
    return Array.isArray(this.recommendations) ? this.recommendations.slice(0, 3) : [];
  }
}
