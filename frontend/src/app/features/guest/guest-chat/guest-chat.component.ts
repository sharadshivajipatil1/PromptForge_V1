import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { GuestService } from '../../../core/services/guest.service';
import { GuestBotWidgetComponent } from '../guest-bot-widget/guest-bot-widget.component';

interface ChatMessage {
  sender: 'bot' | 'guest';
  text: string;
}

@Component({
  selector: 'app-guest-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, GuestBotWidgetComponent],
  template: `
    <div class="page-shell">
      <header class="topbar">
        <div class="brand">
          <span class="brand-mark">✦</span>
          <div>
            <h1>SmartStay</h1>
            <p>Guest chat</p>
          </div>
        </div>
        <nav class="nav-links">
          <a routerLink="/guest/dashboard">Dashboard</a>
          <a routerLink="/guest/experience">Experience</a>
        </nav>
      </header>

      <main class="hero">
        <section class="chat-intro">
          <div class="hero-visual" aria-hidden="true">
            <div class="visual-glow"></div>
            <div class="visual-window"></div>
            <div class="visual-lounge"></div>
            <div class="visual-lamp"></div>
            <div class="visual-badge">Experience nearby</div>
          </div>

          <div>
            <p class="eyebrow">StaySmart</p>
            <h2>Meet your in-stay Chaat assistant</h2>
            <p class="description">Ask about dining, lounge access, room preferences, or your next best experience.</p>
          </div>

          <div class="experience-tags">
            <button type="button" class="tag" (click)="selectPrompt('Nearby dining')">Nearby dining</button>
            <button type="button" class="tag" (click)="selectPrompt('Spa & wellness')">Spa & wellness</button>
            <button type="button" class="tag" (click)="selectPrompt('City highlights')">City highlights</button>
            <button type="button" class="tag" (click)="selectPrompt('Room service')">Room service</button>
            <button type="button" class="tag" (click)="selectPrompt('Sunset lounge')">Sunset lounge</button>
          </div>
        </section>
      </main>

      <footer class="footer">
        <span>Fast • Thoughtful • Elevated</span>
        <span>Need anything else? Ask your Chaat.</span>
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
        position: relative;
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

      .chat-intro {
        width: min(100%, 720px);
        padding: 2rem;
        border-radius: 24px;
        background: rgba(255,255,255,0.84);
        box-shadow: 0 20px 50px rgba(15,23,42,0.12);
        border: 1px solid rgba(148,163,184,0.22);
        overflow: hidden;
      }

      .hero-visual {
        position: relative;
        height: 220px;
        border-radius: 22px;
        margin-bottom: 1.25rem;
        background: linear-gradient(135deg, #10233d 0%, #244469 55%, #3d6188 100%);
        border: 1px solid rgba(255,255,255,0.18);
        overflow: hidden;
      }

      .visual-glow {
        position: absolute;
        inset: 0;
        background: radial-gradient(circle at 18% 20%, rgba(255,255,255,0.32), transparent 28%);
      }

      .visual-window {
        position: absolute;
        top: 28px;
        left: 28px;
        width: 58%;
        height: 58%;
        border-radius: 18px;
        background: linear-gradient(135deg, rgba(255,255,255,0.24), rgba(255,255,255,0.06));
        border: 1px solid rgba(255,255,255,0.18);
        box-shadow: inset 0 0 0 8px rgba(255,255,255,0.05);
      }

      .visual-lounge {
        position: absolute;
        right: 28px;
        bottom: 24px;
        width: 38%;
        height: 45%;
        border-radius: 20px;
        background: linear-gradient(135deg, #e4d0aa, #bd9050);
        box-shadow: inset 0 -10px 20px rgba(0,0,0,0.18);
      }

      .visual-lounge::before {
        content: '';
        position: absolute;
        inset: 14px;
        border-radius: 14px;
        border: 2px solid rgba(255,255,255,0.3);
      }

      .visual-lamp {
        position: absolute;
        left: 44%;
        bottom: 24px;
        width: 16px;
        height: 74px;
        border-radius: 999px;
        background: linear-gradient(180deg, #fef3c7, #d6a372);
        box-shadow: 0 0 24px rgba(254,243,199,0.18);
      }

      .visual-badge {
        position: absolute;
        top: 18px;
        right: 18px;
        padding: 0.45rem 0.75rem;
        border-radius: 999px;
        background: rgba(6, 12, 24, 0.74);
        color: #fef3c7;
        font-size: 0.78rem;
        font-weight: 700;
        letter-spacing: 0.08em;
        text-transform: uppercase;
      }

      .eyebrow {
        margin: 0 0 0.5rem;
        text-transform: uppercase;
        letter-spacing: 0.18em;
        font-size: 0.72rem;
        color: #2563eb;
        font-weight: 700;
      }

      h2 {
        margin: 0 0 0.6rem;
        font-size: 2rem;
      }

      .description {
        margin: 0;
        color: #475569;
        line-height: 1.7;
      }

      .experience-tags {
        display: flex;
        flex-wrap: wrap;
        gap: 0.65rem;
        margin-top: 1.1rem;
      }

      .tag {
        border: 1px solid rgba(37, 99, 235, 0.18);
        background: rgba(37, 99, 235, 0.08);
        color: #123c92;
        border-radius: 999px;
        padding: 0.55rem 0.85rem;
        font-weight: 700;
        cursor: pointer;
      }

      .bot-launcher-wrap {
        position: fixed;
        right: 1.25rem;
        bottom: 1.25rem;
      }

      .bot-launcher {
        border: 0;
        width: 66px;
        height: 66px;
        border-radius: 50%;
        cursor: pointer;
        font-size: 1.55rem;
        background: linear-gradient(135deg, #2563eb, #7c3aed);
        color: white;
        box-shadow: 0 18px 45px rgba(37, 99, 235, 0.35);
      }

      .chat-panel {
        position: absolute;
        right: 0;
        bottom: 82px;
        width: 360px;
        display: grid;
        grid-template-rows: auto 1fr auto;
        gap: 0;
        background: rgba(8, 16, 32, 0.96);
        color: #f8fbff;
        border-radius: 22px;
        border: 1px solid rgba(148,163,184,0.22);
        overflow: hidden;
        opacity: 0;
        pointer-events: none;
        transform: translateY(14px) scale(0.98);
        transition: all 0.22s ease;
        box-shadow: 0 18px 56px rgba(2, 8, 23, 0.42);
      }

      .chat-panel.open {
        opacity: 1;
        pointer-events: auto;
        transform: translateY(0) scale(1);
      }

      .chat-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 0.95rem 1rem;
        background: rgba(255,255,255,0.08);
        border-bottom: 1px solid rgba(255,255,255,0.08);
      }

      .chat-header strong {
        display: block;
        font-size: 0.98rem;
      }

      .chat-header span {
        display: block;
        margin-top: 0.18rem;
        color: #a7c0e4;
        font-size: 0.76rem;
      }

      .close-btn {
        border: 0;
        background: transparent;
        color: white;
        font-size: 1.3rem;
        cursor: pointer;
      }

      .chat-body {
        max-height: 320px;
        overflow: auto;
        padding: 0.9rem;
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
      }

      .message-row {
        display: flex;
      }

      .message-row.guest {
        justify-content: flex-end;
      }

      .bubble {
        max-width: 88%;
        padding: 0.75rem 0.9rem;
        border-radius: 16px;
        line-height: 1.55;
        font-size: 0.92rem;
      }

      .bubble.bot {
        background: rgba(255,255,255,0.09);
        border: 1px solid rgba(255,255,255,0.12);
      }

      .bubble.guest {
        background: linear-gradient(135deg, #60a5fa, #8b5cf6);
        color: white;
      }

      .chat-input-row {
        display: grid;
        grid-template-columns: 1fr auto;
        gap: 0.65rem;
        padding: 0.9rem;
        border-top: 1px solid rgba(255,255,255,0.08);
        background: rgba(255,255,255,0.04);
      }

      .footer {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1rem 2rem 1.5rem;
        color: #56708f;
        font-size: 0.92rem;
      }

      textarea {
        border: 1px solid rgba(148,163,184,0.2);
        border-radius: 12px;
        padding: 0.75rem;
        resize: none;
        background: rgba(255,255,255,0.92);
        color: #172033;
        font: inherit;
      }

      .send-btn {
        border: 0;
        border-radius: 12px;
        padding: 0 1rem;
        background: linear-gradient(135deg, #60a5fa, #7c3aed);
        color: white;
        font-weight: 700;
        cursor: pointer;
      }

      @media (max-width: 800px) {
        .topbar,
        .footer {
          flex-direction: column;
          gap: 0.7rem;
          align-items: flex-start;
        }

        .hero {
          padding: 1rem;
        }
      }

      @media (max-width: 600px) {
        .chat-panel {
          width: min(92vw, 360px);
        }
      }
    `
  ]
})
export class GuestChatComponent {
  private readonly guestService = inject(GuestService);
  message = '';
  isOpen = true;
  messages: ChatMessage[] = [
    {
      sender: 'bot',
      text: 'Hi! I’m your Hospitality AI chaat assistant. Ask me about your stay, dining, or room preferences.'
    }
  ];

  togglePanel(): void {
    this.isOpen = !this.isOpen;
  }

  selectPrompt(prompt: string): void {
    this.message = prompt;
    this.isOpen = true;
  }

  sendMessage(): void {
    const trimmed = this.message.trim();
    if (!trimmed) {
      return;
    }

    this.messages.push({ sender: 'guest', text: trimmed });
    this.message = '';

    this.guestService.sendChat(trimmed).subscribe({
      next: (response) => {
        this.messages.push({ sender: 'bot', text: response.replyInGuestLanguage });
      },
      error: () => {
        this.messages.push({ sender: 'bot', text: 'I’m here to help. Please try a simpler request or check your connection.' });
      }
    });
  }
}
