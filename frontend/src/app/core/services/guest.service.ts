import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { RecommendationItem } from '../models/app.models';

const API_BASE_URL = 'http://localhost:5189/api';

interface CheckInResponse {
  isCheckedIn: boolean;
  roomNumber?: string;
  agentReply?: string;
  recommendations?: Array<{ category: string; title: string; description: string }>;
}

@Injectable({ providedIn: 'root' })
export class GuestService {
  private readonly http = inject(HttpClient);

  getRecommendations(): Observable<{ greeting: string; recommendations: RecommendationItem[] }> {
    return this.http.get<{ greeting: string; recommendations: RecommendationItem[] }>(`${API_BASE_URL}/personalization/me`);
  }

  sendChat(message: string): Observable<{ replyInGuestLanguage: string }> {
    return this.http.post<{ replyInGuestLanguage: string }>(`/api/chat`, { message });
  }

  sendOtp(reservationCode: string): Observable<{ demoOtp?: string }> {
    return this.http.post<{ demoOtp?: string }>(`${API_BASE_URL}/checkin/send-otp`, { reservationCode });
  }

  verifyOtp(otp: string): Observable<{ verified: boolean }> {
    return this.http.post<{ verified: boolean }>(`${API_BASE_URL}/checkin/verify-otp`, { otp });
  }

  checkIn(reservationCode: string): Observable<CheckInResponse> {
    return this.http.post<CheckInResponse>(`${API_BASE_URL}/checkin`, {
      reservationCode: reservationCode.trim()
    });
  }

  checkOut(): Observable<{ isCheckedIn: boolean }> {
    return this.http.post<{ isCheckedIn: boolean }>(`${API_BASE_URL}/checkin/checkout`, {});
  }

  getGuestHistory(reservationCode: string): Observable<{ reply: string }> {
    return this.http.post<{ reply: string }>(`${API_BASE_URL}/agent/guest-history`, { reservationCode });
  }

  getMyTickets(): Observable<Array<{
    id: string;
    guestId?: string;
    guestName: string;
    roomNumber: string;
    message: string;
    status: string;
    createdBy?: string;
    remark?: string;
    priorityReason?: string;
    createdAt?: string;
  }>> {
    return this.http.get<Array<{
      id: string;
      guestId?: string;
      guestName: string;
      roomNumber: string;
      message: string;
      status: string;
      createdBy?: string;
      remark?: string;
      priorityReason?: string;
      createdAt?: string;
    }>>(`${API_BASE_URL}/tickets/me`);
  }

  bookService(payload: { category: string; slotId: string; title: string; suggestedTime?: string; startTime?: string; endTime?: string }): Observable<{ success: boolean; message: string; confirmedTime?: string }> {
    return this.http.post<{ success: boolean; message: string; confirmedTime?: string }>(`${API_BASE_URL}/bookings`, payload);
  }
}
