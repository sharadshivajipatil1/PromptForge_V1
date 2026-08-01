import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-staff-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="page-shell">
      <section class="experience-panel">
        <div class="brand-area">
          <div class="brand-badge">✦</div>
          <div>
            <p class="eyebrow">StaySmart</p>
            <h1>Staff operations command center</h1>
          </div>
        </div>

        <div class="experience-art" aria-hidden="true">
          <div class="art-glow"></div>
          <div class="art-card">
            <span class="art-icon">✦</span>
            <div class="art-lines">
              <span></span>
              <span></span>
              <span></span>
            </div>
          </div>
          <span class="art-caption">Luxury service intelligence</span>
        </div>

        <div class="hero-copy">
          <p class="lead">
            Welcome back to the team. Review priorities, monitor live service flow, and keep every guest touchpoint sharp.
          </p>

          <div class="info-grid">
            <div class="info-card">
              <span>Live</span>
              <strong>Task orchestration</strong>
            </div>
            <div class="info-card">
              <span>Guest</span>
              <strong>Service visibility</strong>
            </div>
            <div class="info-card">
              <span>Support</span>
              <strong>Rapid response</strong>
            </div>
          </div>
        </div>
      </section>

      <section class="login-card">
        <div class="login-head">
          <p class="eyebrow alt">Secure access</p>
          <h2>Staff Login</h2>
          <p class="subcopy">Sign in with your team credentials to access the live operations workspace.</p>
        </div>

        <form class="login-form" (ngSubmit)="submit()">
          <label class="field">
            <span>Username</span>
            <input [(ngModel)]="username" name="username" placeholder="Enter username" />
          </label>

          <label class="field">
            <span>Password</span>
            <input [(ngModel)]="password" name="password" type="password" placeholder="Enter password" />
          </label>

          <button class="submit-btn" type="submit">Sign in</button>
        </form>

        <div class="footer-link-row">
          <a routerLink="/guest/login">Guest login</a>
        </div>
      </section>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-height: 100vh;
        background:
          radial-gradient(circle at top left, rgba(96, 165, 250, 0.3), transparent 24%),
          radial-gradient(circle at bottom right, rgba(139, 92, 246, 0.24), transparent 26%),
          linear-gradient(140deg, #f8fbff 0%, #eef5ff 58%, #f9fbff 100%);
        color: #0f172a;
        font-family: Inter, 'Segoe UI', sans-serif;
      }

      .page-shell {
        min-height: 100vh;
        display: grid;
        grid-template-columns: 1.05fr 0.95fr;
        align-items: center;
        gap: 2rem;
        padding: 2rem;
      }

      .experience-panel,
      .login-card {
        background: rgba(255, 255, 255, 0.9);
        border: 1px solid rgba(148, 163, 184, 0.24);
        border-radius: 28px;
        box-shadow: 0 24px 70px rgba(15, 23, 42, 0.12);
      }

      .experience-panel {
        padding: 2rem;
        min-height: 600px;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        background:
          linear-gradient(140deg, rgba(239, 246, 255, 0.92), rgba(248, 250, 252, 0.95));
      }

      .brand-area {
        display: flex;
        align-items: center;
        gap: 1rem;
      }

      .brand-badge {
        display: grid;
        place-items: center;
        width: 3.2rem;
        height: 3.2rem;
        border-radius: 999px;
        background: linear-gradient(135deg, #2563eb, #7c3aed);
        color: white;
        font-size: 1.25rem;
        box-shadow: 0 16px 30px rgba(37, 99, 235, 0.28);
      }

      .eyebrow {
        margin: 0 0 0.35rem;
        text-transform: uppercase;
        letter-spacing: 0.24em;
        font-size: 0.74rem;
        font-weight: 700;
        color: #4f46e5;
      }

      .eyebrow.alt {
        color: #2563eb;
      }

      .brand-area h1,
      .login-head h2 {
        margin: 0;
      }

      .brand-area h1 {
        font-size: 2rem;
        letter-spacing: -0.04em;
      }

      .experience-art {
        position: relative;
        display: grid;
        place-items: center;
        margin: 1.25rem 0 1.4rem;
        min-height: 260px;
      }

      .art-glow {
        position: absolute;
        width: 76%;
        height: 76%;
        border-radius: 30px;
        background: radial-gradient(circle, rgba(96, 165, 250, 0.28), rgba(124, 58, 237, 0.18), transparent 72%);
        filter: blur(2px);
      }

      .art-card {
        position: relative;
        display: grid;
        place-items: center;
        width: min(280px, 80%);
        height: 220px;
        border-radius: 28px;
        background: linear-gradient(135deg, rgba(255,255,255,0.9), rgba(224, 231, 255, 0.92));
        border: 1px solid rgba(148, 163, 184, 0.35);
        box-shadow: 0 18px 45px rgba(37, 99, 235, 0.16);
        overflow: hidden;
      }

      .art-card::before,
      .art-card::after {
        content: '';
        position: absolute;
        inset: auto auto 1rem 1rem;
        width: 84px;
        height: 84px;
        border-radius: 18px;
        background: rgba(37, 99, 235, 0.08);
      }

      .art-card::after {
        inset: 1rem 1rem auto auto;
        background: rgba(124, 58, 237, 0.08);
      }

      .art-icon {
        display: grid;
        place-items: center;
        width: 5.5rem;
        height: 5.5rem;
        border-radius: 20px;
        background: linear-gradient(135deg, #2563eb, #7c3aed);
        color: white;
        font-size: 1.8rem;
        box-shadow: 0 18px 38px rgba(79, 70, 229, 0.3);
        position: relative;
        z-index: 1;
      }

      .art-lines {
        position: absolute;
        bottom: 1.2rem;
        left: 50%;
        transform: translateX(-50%);
        display: flex;
        gap: 0.45rem;
      }

      .art-lines span {
        display: block;
        width: 42px;
        height: 8px;
        border-radius: 999px;
        background: linear-gradient(90deg, #60a5fa, #8b5cf6);
      }

      .art-caption {
        position: absolute;
        bottom: 0.35rem;
        left: 50%;
        transform: translateX(-50%);
        background: rgba(255, 255, 255, 0.9);
        border-radius: 999px;
        padding: 0.45rem 0.9rem;
        font-size: 0.74rem;
        letter-spacing: 0.14em;
        text-transform: uppercase;
        font-weight: 700;
        color: #0f172a;
      }

      .hero-copy {
        display: flex;
        flex-direction: column;
        gap: 1.25rem;
      }

      .lead {
        max-width: 34rem;
        margin: 0;
        font-size: 1.05rem;
        line-height: 1.8;
        color: #475569;
      }

      .info-grid {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 0.9rem;
      }

      .info-card {
        padding: 1rem;
        background: rgba(255, 255, 255, 0.72);
        border: 1px solid rgba(191, 219, 254, 0.8);
        border-radius: 18px;
      }

      .info-card span {
        display: block;
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0.16em;
        color: #64748b;
      }

      .info-card strong {
        display: block;
        margin-top: 0.35rem;
        color: #0f172a;
      }

      .login-card {
        padding: 2rem;
      }

      .login-head {
        margin-bottom: 1.4rem;
      }

      .subcopy {
        margin: 0.5rem 0 0;
        color: #64748b;
        line-height: 1.7;
      }

      .login-form {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .field {
        display: flex;
        flex-direction: column;
        gap: 0.45rem;
      }

      .field span {
        color: #334155;
        font-weight: 700;
      }

      .field input {
        width: 100%;
        border: 1px solid #cbd5e1;
        border-radius: 14px;
        padding: 0.95rem 1rem;
        font: inherit;
        background: #f8fbff;
        color: #0f172a;
        box-sizing: border-box;
      }

      .field input:focus {
        outline: 2px solid rgba(37, 99, 235, 0.18);
        border-color: #60a5fa;
      }

      .submit-btn {
        margin-top: 0.5rem;
        border: none;
        border-radius: 14px;
        padding: 0.95rem 1.1rem;
        background: linear-gradient(135deg, #2563eb, #7c3aed);
        color: white;
        font-weight: 700;
        font-size: 1rem;
        cursor: pointer;
        box-shadow: 0 18px 35px rgba(37, 99, 235, 0.26);
      }

      .footer-link-row {
        margin-top: 1rem;
      }

      .footer-link-row a {
        color: #2563eb;
        text-decoration: none;
        font-weight: 700;
      }

      @media (max-width: 900px) {
        .page-shell {
          grid-template-columns: 1fr;
        }

        .info-grid {
          grid-template-columns: 1fr;
        }
      }
    `
  ]
})
export class StaffLoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  username = '';
  password = '';

  submit(): void {
    this.auth.staffLogin(this.username, this.password).subscribe({
      next: () => this.router.navigate(['/staff/tasks']),
      error: () => alert('Invalid staff credentials')
    });
  }
}
