import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StaffService } from '../../../core/services/staff.service';
import { ForecastSummary } from '../../../core/models/app.models';

@Component({
  selector: 'app-staff-forecast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card" style="display:grid; gap: 16px;">
      <div style="display:flex; justify-content:space-between; align-items:center;">
        <div>
          <h2>Demand Forecast</h2>
          <p>AI-calculated staffing and inventory guidance.</p>
        </div>
        <button type="button" (click)="refreshForecast()">Refresh</button>
      </div>

      <div *ngIf="forecast; else loading" style="display:grid; gap: 12px; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));">
        <div class="card"><strong>Forecast date</strong><div>{{ forecast.forDate }}</div></div>
        <div class="card"><strong>Predicted occupancy</strong><div>{{ forecast.predictedOccupancyPercent }}%</div></div>
        <div class="card"><strong>Room-service orders</strong><div>{{ forecast.predictedRoomServiceOrders }}</div></div>
        <div class="card"><strong>Housekeeping staff</strong><div>{{ forecast.recommendedHousekeepingStaff }}</div></div>
        <div class="card"><strong>Front desk staff</strong><div>{{ forecast.recommendedFrontDeskStaff }}</div></div>
      </div>

      <ng-template #loading>
        <p>Loading forecast…</p>
      </ng-template>

      <div *ngIf="forecast?.notes" class="card">
        <h3>Notes</h3>
        <p>{{ forecast?.notes }}</p>
      </div>
    </div>
  `
})
export class StaffForecastComponent implements OnInit {
  private readonly staffService = inject(StaffService);
  forecast: ForecastSummary | null = null;

  ngOnInit(): void {
    this.refreshForecast();
  }

  refreshForecast(): void {
    this.staffService.getForecast().subscribe((result) => (this.forecast = result));
  }
}
