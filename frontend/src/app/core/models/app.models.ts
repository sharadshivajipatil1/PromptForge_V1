export interface AuthUser {
  id: string;
  name: string;
  roles: string[];
  token: string;
  department?: string;
  role?: string;
}

export interface LoginResponse {
  token: string;
  userId?: string;
  roles?: string[];
  fullName?: string;
  roomNumber?: string;
  reservationCode?: string;
  message?: string;
}

export interface RecommendationItem {
  id: string;
  title: string;
  category: string;
  description: string;
}

export interface TaskSummary {
  id: string;
  description: string;
  status: string;
  priority: string;
  roomNumber?: string;
  createdAt?: string;
  slaMinutes?: number;
  assignedTo?: string;
  department?: string;
}

export interface ForecastSummary {
  forDate: string;
  predictedOccupancyPercent: number;
  predictedRoomServiceOrders: number;
  recommendedHousekeepingStaff: number;
  recommendedFrontDeskStaff: number;
  notes?: string;
}

export interface ActivityEvent {
  agentName: string;
  message: string;
  timestamp: string;
}

export interface GuestTicketSummary {
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
}

export interface TicketSummary {
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
}

export interface EnrichedForecastSummary extends ForecastSummary {
  inventoryRecommendations?: string;
  recentHistory?: any[];
}

export interface PriorityRecommendation {
  priority: string;
  department: string;
  reason: string;
  score: number;
}
