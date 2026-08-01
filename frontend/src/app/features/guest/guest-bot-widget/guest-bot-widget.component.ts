import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { GuestService } from '../../../core/services/guest.service';

interface ChatMessage {
  sender: 'bot' | 'guest';
  text: string;
}

@Component({
  selector: 'app-guest-bot-widget',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="bot-launcher-wrap">
      <button class="bot-launcher" type="button" (click)="togglePanel()" aria-label="Open chaat panel">
        <span>🤖</span>
      </button>

      <div class="chat-panel" [class.open]="isOpen">
        <div class="chat-header">
          <div>
            <strong>Nova</strong>
            <span>Your stay companion</span>
          </div>
          <button class="close-btn" type="button" (click)="togglePanel()">×</button>
        </div>

        <div class="chat-body">
          <div class="message-row" *ngFor="let item of messages" [class.guest]="item.sender === 'guest'">
            <div class="bubble" [class.bot]="item.sender === 'bot'" [class.guest]="item.sender === 'guest'">
              {{ item.text }}
            </div>
          </div>
        </div>

        <div class="chat-input-row">
          <textarea [(ngModel)]="message" rows="2" placeholder="Type your request..."></textarea>
          <button class="send-btn" type="button" (click)="sendMessage()">Send</button>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        position: fixed;
        right: 1.35rem;
        bottom: 1.35rem;
        z-index: 1000;
      }

      .bot-launcher-wrap {
        position: relative;
      }

      .bot-launcher {
        display: grid;
        place-items: center;
        width: 3.35rem;
        height: 3.35rem;
        border: none;
        border-radius: 999px;
        background: linear-gradient(135deg, #2563eb, #7c3aed);
        color: white;
        box-shadow: 0 16px 32px rgba(37, 99, 235, 0.35);
        cursor: pointer;
        font-size: 1.2rem;
      }

      .bot-launcher:hover {
        transform: translateY(-1px);
      }

      .chat-panel {
        position: absolute;
        right: 0;
        bottom: 4.5rem;
        width: min(360px, calc(100vw - 2rem));
        background: rgba(255, 255, 255, 0.96);
        border: 1px solid rgba(148, 163, 184, 0.32);
        border-radius: 18px;
        box-shadow: 0 24px 50px rgba(15, 23, 42, 0.16);
        overflow: hidden;
        opacity: 0;
        transform: translateY(12px) scale(0.96);
        pointer-events: none;
        transition: all 0.22s ease;
      }

      .chat-panel.open {
        opacity: 1;
        transform: translateY(0) scale(1);
        pointer-events: auto;
      }

      .chat-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 0.9rem 1rem;
        background: linear-gradient(135deg, #eff6ff, #f5f3ff);
        border-bottom: 1px solid #e2e8f0;
      }

      .chat-header strong {
        display: block;
        color: #0f172a;
      }

      .chat-header span {
        color: #64748b;
        font-size: 0.76rem;
      }

      .close-btn {
        border: none;
        background: transparent;
        color: #475569;
        font-size: 1.25rem;
        cursor: pointer;
      }

      .chat-body {
        max-height: 300px;
        overflow: auto;
        padding: 0.9rem;
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
        background: #f8fbff;
      }

      .message-row {
        display: flex;
      }

      .message-row.guest {
        justify-content: flex-end;
      }

      .bubble {
        max-width: 88%;
        padding: 0.7rem 0.8rem;
        border-radius: 14px;
        line-height: 1.5;
        color: #0f172a;
        background: #e2e8f0;
      }

      .bubble.bot {
        background: #dbeafe;
      }

      .bubble.guest {
        background: #ede9fe;
      }

      .chat-input-row {
        display: grid;
        grid-template-columns: 1fr auto;
        gap: 0.6rem;
        padding: 0.9rem;
        border-top: 1px solid #e2e8f0;
        background: white;
      }

      textarea {
        width: 100%;
        resize: none;
        border: 1px solid #cbd5e1;
        border-radius: 12px;
        padding: 0.7rem 0.8rem;
        font: inherit;
      }

      .send-btn {
        border: none;
        border-radius: 12px;
        padding: 0.78rem 1rem;
        background: #2563eb;
        color: white;
        font-weight: 700;
        cursor: pointer;
      }
    `
  ]
})
export class GuestBotWidgetComponent {
  private readonly guestService = inject(GuestService);
  isOpen = false;
  message = '';
  messages: ChatMessage[] = [
    {
      sender: 'bot',
      text: 'Hi! I’m Nova. Ask me about your stay, dining, or room preferences and more.'
    }
  ];

  togglePanel(): void {
    this.isOpen = !this.isOpen;
  }

  selectPrompt(prompt: string): void {
    this.message = prompt;
  }

  sendMessage(): void {
    const text = this.message.trim();
    if (!text) {
      return;
    }

    this.messages.push({ sender: 'guest', text });
    this.message = '';

    this.guestService.sendChat(text).subscribe({
      next: (response) => {
        this.messages.push({ sender: 'bot', text: response.replyInGuestLanguage || 'I can help with dining, transfers, and stay preferences.' });
      },
      error: () => {
        this.messages.push({ sender: 'bot', text: 'I’m here to help with your stay. Try asking about dining, spa, or local experiences.' });
      }
    });
  }
}
