import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { GuestService } from '../../../core/services/guest.service';

interface Recommendation {
  category: string;
  title: string;
  description: string;
}

@Component({
  selector: 'app-guest-check-in',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
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
        <nav class="nav-links">
          <a routerLink="/guest/dashboard">Dashboard</a>
          <a routerLink="/guest/chat">Chat</a>
        </nav>
      </header>

      <main class="hero">
        <section class="checkin-card">
          <div class="intro-panel">
            <div class="hero-graphic" aria-hidden="true">
              <div class="graphic-core"></div>
              <div class="graphic-ring ring-one"></div>
              <div class="graphic-ring ring-two"></div>
              <div class="graphic-orb orb-one"></div>
              <div class="graphic-orb orb-two"></div>
            </div>

            <p class="eyebrow">Smart arrival flow</p>
            <h2>Complete your check-in in minutes</h2>
            <p class="description">Use your reservation code to receive a secure one-time password and finish arrival with confidence.</p>

            <div class="step-list">
              <div class="step-item">
                <span class="step-badge">1</span>
                <div>
                  <strong>Verify reservation</strong>
                  <p>Confirm your stay details instantly.</p>
                </div>
              </div>
              <div class="step-item">
                <span class="step-badge">2</span>
                <div>
                  <strong>Secure OTP</strong>
                  <p>Receive a demo verification code for your session.</p>
                </div>
              </div>
              <div class="step-item">
                <span class="step-badge">3</span>
                <div>
                  <strong>Finish check-in</strong>
                  <p>Move into your personalized stay experience.</p>
                </div>
              </div>
            </div>
          </div>

          <div class="form-panel">
            <h3>Arrival assistance</h3>
            <p class="panel-copy">Enter your reservation code, verify the secure OTP, and finish check-in with personalized recommendations.</p>

            <label>Reservation code</label>
            <input
              [(ngModel)]="reservationCode"
              name="reservationCode"
              placeholder="Reservation code"
              autocomplete="off"
              autocapitalize="off"
              spellcheck="false"
              type="text"
              [disabled]="otpVerified" />

            <button class="secondary" (click)="sendOtp()" [disabled]="!reservationCode.trim() || otpSent || otpVerified">
              {{ otpSent ? 'OTP Sent' : 'Send OTP' }}
            </button>

            <label>OTP</label>
            <input
              [(ngModel)]="otp"
              name="otp"
              placeholder="OTP"
              autocomplete="one-time-code"
              autocapitalize="off"
              spellcheck="false"
              inputmode="numeric"
              type="text"
              [disabled]="otpVerified" />

            <button class="secondary" (click)="verifyOtp()" [disabled]="!otp.trim() || otpVerified">
              {{ otpVerified ? '✓ Verified' : 'Verify OTP' }}
            </button>
            <button class="primary" (click)="checkIn()" [disabled]="!otpVerified || loading">
              {{ loading ? 'Checking in…' : 'Complete Check-In' }}
            </button>

            <div class="message-box" *ngIf="statusMessage || error || message">
              <span>✦</span>
              <p>{{ error || statusMessage || message }}</p>
            </div>

            <div class="success-banner" *ngIf="checkedIn">
              <span class="icon">✓</span>
              <div>
                <strong>Welcome! You are checked in.</strong>
                <span *ngIf="roomNumber"> Room {{ roomNumber }}</span>
              </div>
            </div>

            <div class="recommendations-section" *ngIf="checkedIn">
              <h4>Personalised Recommendations</h4>

              <div class="loading-state" *ngIf="loadingRecommendations">
                <span class="spinner"></span> Fetching personalised recommendations…
              </div>

              <div *ngIf="!loadingRecommendations && recommendations.length > 0">
                <div class="rec-card" *ngFor="let rec of recommendations">
                  <span class="rec-badge">{{ rec.category }}</span>
                  <strong>{{ rec.title }}</strong>
                  <p>{{ rec.description }}</p>
                </div>
              </div>

              <div class="agent-reply" *ngIf="!loadingRecommendations && recommendations.length === 0 && agentReply">
                <p>{{ agentReply }}</p>
              </div>

              <p class="message info" *ngIf="!loadingRecommendations && recommendations.length === 0 && !agentReply">
                No recommendations available at this time.
              </p>
            </div>
          </div>
        </section>
      </main>

      <footer class="footer">
        <span>Need help? Ask the concierge team.</span>
        <span>Fast • Secure • Guided</span>
      </footer>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-height: 100vh;
        background: linear-gradient(135deg, #f8fbff 0%, #eef6ff 60%, #f9f7ff 100%);
        color: #1f2937;
        font-family: Inter, 'Segoe UI', sans-serif;
      }

      .page-shell { min-height: 100vh; display: flex; flex-direction: column; }
      .topbar { display: flex; justify-content: space-between; align-items: center; padding: 1.25rem 2rem; background: rgba(255, 255, 255, 0.84); backdrop-filter: blur(12px); border-bottom: 1px solid rgba(15, 23, 42, 0.08); }
      .brand { display: flex; align-items: center; gap: 0.85rem; }
      .brand-mark { display: grid; place-items: center; width: 2.5rem; height: 2.5rem; border-radius: 999px; background: linear-gradient(135deg, #2563eb, #7c3aed); color: white; font-size: 1.15rem; }
      .brand h1 { margin: 0; font-size: 1rem; font-weight: 700; }
      .brand p { margin: 0; font-size: 0.8rem; color: #64748b; }
      .nav-links { display: flex; gap: 1rem; }
      .nav-links a { text-decoration: none; color: #2563eb; font-weight: 600; }
      .hero { flex: 1; display: flex; align-items: center; justify-content: center; padding: 2rem; }
      .checkin-card { width: min(100%, 1120px); display: grid; grid-template-columns: 1fr 0.95fr; gap: 2rem; background: rgba(255, 255, 255, 0.94); border: 1px solid rgba(148, 163, 184, 0.2); border-radius: 24px; box-shadow: 0 24px 60px rgba(15, 23, 42, 0.12); overflow: hidden; }
      .intro-panel { position: relative; padding: 3rem; background: linear-gradient(135deg, #eff6ff 0%, #f8fafc 100%); overflow: hidden; }
      .hero-graphic { position: absolute; right: 1rem; top: 1rem; width: 180px; height: 180px; border-radius: 28px; background: linear-gradient(135deg, rgba(37, 99, 235, 0.12), rgba(124, 58, 237, 0.16)); backdrop-filter: blur(8px); border: 1px solid rgba(255, 255, 255, 0.5); box-shadow: 0 18px 40px rgba(37, 99, 235, 0.12); animation: pulsePanel 4.5s ease-in-out infinite; }
      .graphic-core { position: absolute; inset: 34px; border-radius: 50%; background: linear-gradient(135deg, #2563eb, #7c3aed); box-shadow: inset 0 0 0 10px rgba(255,255,255,0.25); animation: spinSlow 8s linear infinite; }
      .graphic-ring { position: absolute; border-radius: 50%; border: 2px solid rgba(37, 99, 235, 0.35); animation: spinReverse 6s linear infinite; }
      .ring-one { inset: 18px; }
      .ring-two { inset: 48px; border-color: rgba(124, 58, 237, 0.4); animation-duration: 7s; }
      .graphic-orb { position: absolute; width: 18px; height: 18px; border-radius: 50%; background: linear-gradient(135deg, #60a5fa, #8b5cf6); box-shadow: 0 0 24px rgba(96, 165, 250, 0.4); }
      .orb-one { top: 24px; right: 28px; animation: bob 2.6s ease-in-out infinite; }
      .orb-two { bottom: 26px; left: 26px; animation: bob 2.8s ease-in-out infinite reverse; }
      .intro-panel::before, .intro-panel::after { content: ''; position: absolute; border-radius: 999px; filter: blur(1px); animation: floatOrbs 8s ease-in-out infinite; pointer-events: none; }
      .intro-panel::before { width: 220px; height: 220px; right: -60px; top: -70px; background: radial-gradient(circle, rgba(37, 99, 235, 0.18), rgba(37, 99, 235, 0)); }
      .intro-panel::after { width: 180px; height: 180px; left: -40px; bottom: -50px; background: radial-gradient(circle, rgba(124, 58, 237, 0.16), rgba(124, 58, 237, 0)); animation-delay: -3s; }
      .intro-panel > * { position: relative; z-index: 1; }
      .eyebrow { margin: 0 0 0.75rem; text-transform: uppercase; letter-spacing: 0.24em; font-size: 0.78rem; font-weight: 700; color: #4f46e5; }
      .intro-panel h2 { margin: 0 0 0.75rem; font-size: 2.15rem; line-height: 1.15; color: #0f172a; letter-spacing: -0.02em; }
      .description { margin: 0 0 1.5rem; color: #475569; line-height: 1.7; }
      .step-list { display: flex; flex-direction: column; gap: 0.95rem; }
      .step-item { display: flex; align-items: flex-start; gap: 0.8rem; padding: 0.95rem 1rem; background: linear-gradient(135deg, rgba(255,255,255,0.95), rgba(248,250,252,0.9)); border-radius: 14px; border: 1px solid rgba(148, 163, 184, 0.18); box-shadow: 0 10px 24px rgba(15, 23, 42, 0.06); animation: slideInUp 0.6s ease both; }
      .step-item:nth-child(2) { animation-delay: 0.15s; }
      .step-item:nth-child(3) { animation-delay: 0.3s; }
      .step-badge { display: grid; place-items: center; width: 2rem; height: 2rem; border-radius: 999px; background: linear-gradient(135deg, #2563eb, #4f46e5); color: white; font-weight: 700; flex-shrink: 0; }
      .step-item strong { display: block; margin-bottom: 0.2rem; color: #111827; }
      .step-item p { margin: 0; color: #64748b; font-size: 0.95rem; }
      .form-panel { padding: 3rem; display: flex; flex-direction: column; gap: 0.8rem; background: linear-gradient(145deg, #ffffff 0%, #f9fbff 100%); }
      .form-panel h3 { margin: 0; color: #0f172a; }
      .panel-copy { margin: 0 0 0.2rem; color: #64748b; line-height: 1.6; }
      label { font-weight: 600; color: #334155; margin-top: 0.2rem; }
      input { padding: 0.9rem 1rem; border: 1px solid #cbd5e1; border-radius: 12px; font-size: 1rem; outline: none; transition: border-color 0.2s ease, box-shadow 0.2s ease; }
      input:focus { border-color: #2563eb; box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.16); }
      button { border: none; border-radius: 12px; padding: 0.95rem 1rem; font-size: 1rem; font-weight: 700; cursor: pointer; transition: transform 0.2s ease, box-shadow 0.2s ease; }
      button:hover { transform: translateY(-2px); box-shadow: 0 10px 20px rgba(79, 70, 229, 0.16); }
      .secondary { background: linear-gradient(135deg, #eaf2ff, #e8ebff); color: #1d4ed8; }
      .primary { background: linear-gradient(135deg, #2563eb, #4f46e5); color: white; box-shadow: 0 14px 28px rgba(79, 70, 229, 0.18); }
      .message-box { display: flex; align-items: flex-start; gap: 0.65rem; margin-top: 0.4rem; padding: 0.85rem 0.95rem; border-radius: 12px; background: #f8fafc; color: #334155; border: 1px solid #e2e8f0; }
      .success-banner { display: flex; align-items: center; gap: 1rem; background: #d1fae5; border-radius: 10px; padding: 1rem 1.25rem; margin: 0.75rem 0 1.25rem; }
      .success-banner .icon { font-size: 1.75rem; color: #059669; }
      .recommendations-section { margin-top: 0.35rem; }
      .recommendations-section h4 { margin: 0 0 0.75rem; color: #111827; }
      .rec-card { border: 1px solid #e5e7eb; border-radius: 10px; padding: 1rem 1.25rem; margin-bottom: .75rem; background: #fff; }
      .rec-badge { display: inline-block; background: #ede9fe; color: #7c3aed; font-size: .75rem; font-weight: 700; padding: 2px 8px; border-radius: 999px; margin-bottom: .4rem; text-transform: uppercase; }
      .rec-card strong { display: block; font-size: 1rem; margin-bottom: .3rem; color: #111827; }
      .rec-card p { margin: 0; color: #4b5563; font-size: .9rem; line-height: 1.5; }
      .agent-reply { background: #f8fafc; border-left: 3px solid #4f46e5; padding: 1rem 1.25rem; border-radius: 0 8px 8px 0; }
      .agent-reply p { margin: 0; color: #374151; font-size: .9rem; line-height: 1.6; white-space: pre-wrap; }
      .loading-state { display: flex; align-items: center; gap: .75rem; color: #6b7280; font-size: .9rem; padding: 1rem 0; }
      .spinner { width: 18px; height: 18px; border: 2px solid #e5e7eb; border-top-color: #4f46e5; border-radius: 50%; animation: spin .7s linear infinite; display: inline-block; }
      .message.info { color: #6b7280; }
      @keyframes floatOrbs { 0%, 100% { transform: translate3d(0, 0, 0) scale(1); } 50% { transform: translate3d(10px, 18px, 0) scale(1.06); } }
      @keyframes pulsePanel { 0%, 100% { transform: scale(1); box-shadow: 0 18px 40px rgba(37, 99, 235, 0.12); } 50% { transform: scale(1.03); box-shadow: 0 24px 48px rgba(37, 99, 235, 0.18); } }
      @keyframes spinSlow { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
      @keyframes spinReverse { from { transform: rotate(360deg); } to { transform: rotate(0deg); } }
      @keyframes bob { 0%, 100% { transform: translateY(0); } 50% { transform: translateY(-8px); } }
      @keyframes slideInUp { from { opacity: 0; transform: translateY(16px); } to { opacity: 1; transform: translateY(0); } }
      @keyframes spin { to { transform: rotate(360deg); } }
      .footer { display: flex; justify-content: space-between; align-items: center; padding: 1rem 2rem 1.5rem; color: #64748b; font-size: 0.92rem; }
      @media (max-width: 800px) { .checkin-card { grid-template-columns: 1fr; } .topbar, .footer { flex-direction: column; gap: 0.7rem; align-items: flex-start; } .intro-panel, .form-panel { padding: 1.5rem; } }
    `
  ]
})
export class GuestCheckInComponent {
  private readonly guestService = inject(GuestService);
  private readonly router = inject(Router);

  reservationCode = '';
  otp = '';

  otpSent = false;
  otpVerified = false;
  loading = false;
  checkedIn = false;
  loadingRecommendations = false;

  roomNumber = '';
  agentReply = '';
  recommendations: Recommendation[] = [];
  statusMessage = '';
  error = '';
  message = '';

  sendOtp(): void {
    const reservationCode = this.reservationCode.trim();
    if (!reservationCode) {
      this.error = 'Please enter your reservation code first.';
      return;
    }

    this.error = '';
    this.statusMessage = '';
    this.guestService.sendOtp(reservationCode).subscribe({
      next: (response) => {
        this.otpSent = true;
        this.statusMessage = response.demoOtp ? `Demo OTP: ${response.demoOtp}` : 'OTP sent to your registered number.';
        this.message = this.statusMessage;
      },
      error: () => {
        this.error = 'Failed to send OTP. Please check the reservation code.';
        this.message = this.error;
      }
    });
  }

  verifyOtp(): void {
    const otp = this.otp.trim();
    if (!otp) {
      this.error = 'Please enter your OTP.';
      return;
    }

    this.error = '';
    this.guestService.verifyOtp(otp).subscribe({
      next: (response) => {
        this.otpVerified = response.verified;
        if (response.verified) {
          this.statusMessage = 'OTP verified. You can now complete check-in.';
          this.message = this.statusMessage;
        } else {
          this.error = 'OTP verification failed. Please try again.';
          this.message = this.error;
        }
      },
      error: () => {
        this.error = 'OTP verification failed.';
        this.message = this.error;
      }
    });
  }

  checkIn(): void {
    const reservationCode = this.reservationCode.trim();
    if (!reservationCode) {
      this.error = 'Please enter your reservation code first.';
      this.message = this.error;
      return;
    }

    if (!this.otpVerified) {
      this.error = 'Please verify the OTP before completing check-in.';
      this.message = this.error;
      return;
    }

    this.error = '';
    this.statusMessage = '';
    this.message = '';
    this.loading = true;
    this.loadingRecommendations = true;

    this.guestService.checkIn(reservationCode).subscribe({
      next: (response) => {
        this.loading = false;

        if (!response.isCheckedIn) {
          this.error = 'Check-in failed. Please try again.';
          this.message = this.error;
          this.loadingRecommendations = false;
          return;
        }

        this.checkedIn = true;
        this.roomNumber = response.roomNumber ?? '';
        this.agentReply = response.agentReply ?? '';
        this.recommendations = response.recommendations ?? [];

        this.loadingRecommendations = false;
        this.statusMessage = 'Check-in completed successfully.';
        this.message = this.statusMessage;

        this.router.navigate(['/guest/dashboard']);
      },
      error: (err) => {
        this.loading = false;
        this.loadingRecommendations = false;
        this.error = err?.error?.message ?? 'Check-in failed. Please try again.';
        this.message = this.error;
      }
    });
  }
}
