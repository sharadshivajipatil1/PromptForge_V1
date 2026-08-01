import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-guest-login',
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
        <a class="staff-link" routerLink="/staff/login">Staff Login</a>
      </header>

      <main class="hero">
        <section class="welcome-card">
          <div class="welcome-copy">
            <p class="eyebrow">Seamless arrival experience</p>
            <h2>Welcome! We're glad you're here and wish you a wonderful stay.</h2>
            <p class="description">Use your reservation code to unlock your stay details, local recommendations, and concierge support.</p>
          </div>

          <div class="login-card">
            <h3>Guest Login</h3>
            <p>Enter your reservation code to continue.</p>
            <form (ngSubmit)="submit()">
              <label for="reservationCode">Reservation code</label>
              <input id="reservationCode" [(ngModel)]="reservationCode" name="reservationCode" placeholder="RES-8842" />
              <button type="submit">Sign in</button>
            </form>
          </div>
        </section>
      </main>

      <footer class="footer">
        <span>Secure • Fast • Personalized</span>
      </footer>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-height: 100vh;
        background: linear-gradient(135deg, #f5f7ff 0%, #eef4ff 55%, #f8fbff 100%);
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
        background: rgba(255, 255, 255, 0.8);
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

      .staff-link {
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

      .welcome-card {
        width: min(100%, 1100px);
        display: grid;
        grid-template-columns: 1.1fr 0.9fr;
        gap: 2rem;
        background: rgba(255, 255, 255, 0.9);
        border: 1px solid rgba(148, 163, 184, 0.2);
        border-radius: 24px;
        box-shadow: 0 25px 60px rgba(15, 23, 42, 0.12);
        overflow: hidden;
      }

      .welcome-copy {
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

      .welcome-copy h2 {
        margin: 0 0 0.75rem;
        font-size: 2rem;
        color: #0f172a;
      }

      .description {
        margin: 0;
        color: #475569;
        line-height: 1.7;
        font-size: 1rem;
      }

      .login-card {
        padding: 3rem;
        display: flex;
        flex-direction: column;
        justify-content: center;
      }

      .login-card h3 {
        margin: 0 0 0.4rem;
        font-size: 1.3rem;
        color: #0f172a;
      }

      .login-card p {
        margin: 0 0 1.2rem;
        color: #64748b;
      }

      form {
        display: flex;
        flex-direction: column;
        gap: 0.85rem;
      }

      label {
        font-weight: 600;
        color: #334155;
      }

      input {
        padding: 0.9rem 1rem;
        border: 1px solid #cbd5e1;
        border-radius: 12px;
        font-size: 1rem;
        outline: none;
        transition: border-color 0.2s ease, box-shadow 0.2s ease;
      }

      input:focus {
        border-color: #2563eb;
        box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.16);
      }

      button {
        border: none;
        border-radius: 12px;
        padding: 0.95rem 1rem;
        background: linear-gradient(135deg, #2563eb, #4f46e5);
        color: white;
        font-size: 1rem;
        font-weight: 700;
        cursor: pointer;
        transition: transform 0.2s ease, box-shadow 0.2s ease;
      }

      button:hover {
        transform: translateY(-1px);
        box-shadow: 0 12px 24px rgba(79, 70, 229, 0.24);
      }

      .footer {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1rem 2rem 1.5rem;
        color: #64748b;
        font-size: 0.92rem;
      }

      @media (max-width: 800px) {
        .welcome-card {
          grid-template-columns: 1fr;
        }

        .topbar,
        .footer {
          flex-direction: column;
          gap: 0.7rem;
          align-items: flex-start;
        }

        .welcome-copy,
        .login-card {
          padding: 1.5rem;
        }
      }
    `
  ]
})
export class GuestLoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  reservationCode = '';

  submit(): void {
    this.auth.guestLogin(this.reservationCode).subscribe({
      next: () => this.router.navigate(['/guest/check-in']),
      error: () => alert('Invalid reservation code')
    });
  }
}
