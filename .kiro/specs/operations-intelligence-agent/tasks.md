# Implementation Plan: Operations Intelligence Agent

## Overview

Implement the Operations Intelligence Agent feature in two parallel tracks — backend (ASP.NET Core 8) and frontend (Angular 17) — then wire them together. The backend introduces a new `OperationsBedrockService`, a new `PriorityRecommendationDto`, extended `ForecastDto`, two new endpoints, and a Node.js Lambda. The frontend adds a debounced priority suggestion to the task creation form and a prediction panel at the top of the dashboard.

---

## Tasks

- [x] 1. Extend domain types

  - [x] 1.1 Create `PriorityRecommendationDto` and extend `ForecastDto`
    - Add `HospitalityAI.Domain/Dtos/PriorityRecommendationDto.cs` with `Priority` (string) and `Reason` (string) fields.
    - Add three fields to `HospitalityAI.Domain/Dtos/ForecastDto.cs`: `RecommendedMaintenanceStaff` (int), `RecommendedFoodBeverageStaff` (int), `OperationalOutlook` (string).
    - _Requirements: 1.1, 2.7, 3.2_

- [ ] 2. Implement `OperationsBedrockService`

  - [x] 2.1 Create `OperationsBedrockService` skeleton and configuration
    - Create `HospitalityAI.Api/Services/OperationsBedrockService.cs`.
    - Read `OperationsBedrock:Region`, `OperationsBedrock:AgentArn`, `OperationsBedrock:AgentAliasId`, `OperationsBedrock:AccessKey`, `OperationsBedrock:SecretKey` from `IConfiguration`.
    - Apply the same credential fallback pattern as `BedrockAgentService` (explicit credentials if keys present, else default AWS credential chain).
    - Throw `InvalidOperationException` at constructor time if `AgentArn` is absent or empty.
    - Add `OperationsBedrock` section placeholder to `appsettings.json`.
    - _Requirements: 3.1, 3.5, 3.6_

  - [x] 2.2 Implement `ParsePriority` and `BuildSessionId` static helpers
    - Add `internal static string ParsePriority(string agentResponse)` — scans case-insensitively for "critical", "high", "medium", "low" in that precedence order; returns "Medium" if none found.
    - Add `internal static string BuildSessionId(string prefix, DateTimeOffset now)` — returns `"{prefix}-{now:yyyyMMdd}"`.
    - _Requirements: 1.3, 3.4_

  - [-] 2.3 Write property tests for `ParsePriority` and `BuildSessionId`
    - Use FsCheck (add NuGet package `FsCheck.Xunit` to `HospitalityAI.Tests`).
    - **Property 1: Priority Parser Always Returns a Valid TaskPriority** — for any arbitrary string, `ParsePriority` returns one of { "Low", "Medium", "High", "Critical" } and never throws. _Requirements: 1.3_
    - **Property 2: Priority Parser Defaults to Medium on Unrecognized Input** — for any string not containing a priority token, result is "Medium". _Requirements: 1.3_
    - **Property 4: Session ID Format Is Always Well-Formed** — for any `DateTimeOffset`, `BuildSessionId("ops-priority", date)` matches `^ops-priority-\d{8}$` and similarly for "ops-forecast". _Requirements: 3.4_
    - Tag each test: `// Feature: operations-intelligence-agent, Property N: ...`

  - [-] 2.4 Implement `GetPriorityRecommendationAsync`
    - Build a prompt instructing the agent to analyze the task description and return the priority level.
    - Use session ID `BuildSessionId("ops-priority", DateTimeOffset.UtcNow)`.
    - Call the Bedrock agent via the shared `InvokeAgentAsync` helper (mirror `BedrockAgentService` pattern).
    - Call `ParsePriority` on the raw response to extract the priority.
    - Extract the remainder of the agent response (after the priority token) as the `Reason`; if no token is found, set `Reason` to the full response.
    - Return `PriorityRecommendationDto { Priority, Reason }`.
    - _Requirements: 1.2, 1.3, 3.2_

  - [-] 2.5 Implement `GetOperationsForecastAsync`
    - Build a prompt that explicitly tells the agent to call the `operations-data-lookup` action group to retrieve historical data, then return staffing recommendations for Housekeeping, Front Desk, Maintenance, and Food & Beverage, inventory restock items, and an operational outlook sentence.
    - Use session ID `BuildSessionId("ops-forecast", DateTimeOffset.UtcNow)`.
    - Parse the agent response into an enriched `ForecastDto` (extract numeric staffing counts and inventory items from the structured text response).
    - Return the enriched `ForecastDto`.
    - _Requirements: 2.2, 3.3_

  - [~] 2.6 Register `OperationsBedrockService` in `Program.cs`
    - Add `builder.Services.AddSingleton<OperationsBedrockService>();` after the existing `BedrockAgentService` registration.
    - _Requirements: 3.5_

- [~] 3. Checkpoint — Build and verify backend compiles
  - Ensure the solution builds with `dotnet build`. Ask the user if any questions arise.

- [ ] 4. Extend `StaffController`

  - [~] 4.1 Add optional `Priority` field to `CreateTaskRequest` and update `POST /api/dashboard/tasks`
    - Add `public string? Priority { get; set; }` to the existing `CreateTaskRequest` inner class.
    - In `CreateTask`, parse the `Priority` field: if it is a valid `TaskPriority` enum value (case-insensitive), use it; otherwise keep `TaskPriority.Medium`.
    - _Requirements: 7.1, 7.2, 7.3_

  - [~] 4.2 Write property tests for `CreateTask` priority handling
    - **Property 5: Valid Priority Values Are Preserved Through Task Creation** — for each valid TaskPriority name string, a POST to `/api/dashboard/tasks` with that `priority` value saves a task with that exact priority. Use `WebApplicationFactory` with an in-memory store.
    - **Property 6: Unrecognized Priority Values Fall Back to Medium** — FsCheck generator produces arbitrary strings; POST to `/api/dashboard/tasks`; assert persisted task has `Medium`.
    - _Requirements: 7.2, 7.3_

  - [~] 4.3 Add `POST /api/dashboard/task-priority` endpoint
    - Add `[HttpPost("task-priority")]` action accepting `TaskPriorityRequest { [Required] string Description }`.
    - If description is null/whitespace, return `BadRequest`.
    - Call `OperationsBedrockService.GetPriorityRecommendationAsync(description, ct)`.
    - On exception, log and return `StatusCode(500)`.
    - Return `Ok(new { priority = dto.Priority, reason = dto.Reason })`.
    - _Requirements: 1.1, 1.4, 1.5, 1.6_

  - [~] 4.4 Update `GET /api/dashboard/forecast` to use `OperationsBedrockService` with fallback
    - Inject `OperationsBedrockService` into `StaffController`.
    - In the `Forecast` action, call `OperationsBedrockService.GetOperationsForecastAsync(ct)` inside a try/catch.
    - On exception, log the error and call `_operationsAgent.GenerateForecastAsync("staff dashboard", ct)` as fallback, then populate `RecommendedMaintenanceStaff = 2`, `RecommendedFoodBeverageStaff = 2`, `OperationalOutlook = "Forecast generated from rule-based model."`.
    - Return the enriched forecast (all existing fields plus the three new fields).
    - _Requirements: 2.1, 2.5, 2.6_

- [x] 5. Implement `operations-data-lookup` Lambda

  - [x] 5.1 Create Lambda function code
    - Create `lambda/operations-data-lookup/index.mjs` (ES module, Node.js 20).
    - Embed the full historical dataset as a `const DATASET` object containing: `occupancyByDayOfWeek` (Monday–Sunday averages), `taskVolumeByTypeAndDay` (Housekeeping / Maintenance / GuestRequest / RoomService by day), `inventoryConsumptionRatePerRoom` (5 item categories), `currentInventoryLevels` (same 5 categories), `seasonalAdjustments` (Summer: 1.25, December: 1.35, Default: 1.0).
    - Export a `handler` function that returns `{ statusCode: 200, body: JSON.stringify(DATASET) }` for any invocation.
    - _Requirements: 4.1, 4.2, 4.3, 4.4_

  - [x] 5.2 Write a unit test for the Lambda handler
    - Verify `handler({})` returns HTTP 200 and a body that parses to a valid JSON object containing all five required top-level keys.
    - Use Node.js built-in `assert` or Jest (whichever is simpler given no existing Lambda test setup).
    - _Requirements: 4.1, 4.2_

- [~] 6. Checkpoint — Verify Lambda works locally
  - Run `node -e "import('./lambda/operations-data-lookup/index.mjs').then(m => m.handler({}).then(console.log))"` from the workspace root (or run the Jest test if set up). Ensure all 5 top-level keys are present. Ask the user if any questions arise.

- [x] 7. Update Angular models and service

  - [x] 7.1 Add `PriorityRecommendation` and `EnrichedForecastSummary` to `app.models.ts`
    - Add `PriorityRecommendation` interface: `{ priority: 'Low' | 'Medium' | 'High' | 'Critical'; reason: string; }`.
    - Add `InventoryItem` interface: `{ item: string; recommendedUnits: number; reason: string; }`.
    - Add `EnrichedForecastSummary` interface extending `ForecastSummary` with `recommendedMaintenanceStaff`, `recommendedFoodBeverageStaff`, `operationalOutlook`, and optional `inventoryRecommendations: InventoryItem[]`.
    - _Requirements: 5.6, 6.7, 6.8_

  - [x] 7.2 Add `getPriorityRecommendation()` and update `getForecast()` in `StaffService`
    - Add `getPriorityRecommendation(description: string): Observable<PriorityRecommendation>` — POST to `/api/dashboard/task-priority` with `{ description }`.
    - Change `getForecast()` return type from `Observable<ForecastSummary>` to `Observable<EnrichedForecastSummary>`.
    - _Requirements: 5.6, 6.7_

- [ ] 8. Update `StaffTasksComponent` — AI priority suggestion

  - [~] 8.1 Add debounced priority suggestion to the task creation form
    - Import `Subject`, `debounceTime`, `filter`, `switchMap`, `catchError`, `of`, `finalize` from `rxjs` and `rxjs/operators`.
    - Add `descriptionInput$ = new Subject<string>()` and subscribe in `ngOnInit` with `debounceTime(800)`, `filter(v => v.length >= 10)`, `switchMap(v => this.staffService.getPriorityRecommendation(v).pipe(catchError(() => of(null)), finalize(() => this.isSuggestionLoading = false)))`.
    - Set `this.isSuggestionLoading = true` before the switchMap emits.
    - Store result in `prioritySuggestion: PriorityRecommendation | null = null`.
    - Wire `(ngModelChange)` on the description input to emit into `descriptionInput$`.
    - In `createTask()`, include `priority: this.prioritySuggestion?.priority` in the payload if a suggestion exists.
    - Unsubscribe in `ngOnDestroy`.
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [~] 8.2 Add priority suggestion UI to the task creation form template
    - Below the description `<input>`, add a `<div class="priority-suggestion">` that shows conditionally:
      - Loading spinner (`*ngIf="isSuggestionLoading"`) with text "Getting AI priority suggestion…"
      - Suggestion card (`*ngIf="prioritySuggestion && !isSuggestionLoading"`) displaying a colored priority badge (reuse existing `.priority-pill` classes) and the `reason` text.
    - Match the existing visual style: glassmorphism cards, Inter font, existing color tokens for priority levels.
    - _Requirements: 5.2, 5.4_

  - [~] 8.3 Write property and example tests for priority suggestion rendering
    - **Property 7: Priority Recommendation Is Rendered Completely** — for any `PriorityRecommendation { priority, reason }`, after setting `prioritySuggestion` on the component, the fixture's native element text content contains both the priority string and the reason string. Use Jasmine/Jest parameterized tests over all 4 priority values × a set of reason strings.
    - Example test: loading state shows spinner text and hides suggestion card.
    - Example test: error from service hides suggestion area.
    - _Requirements: 5.2, 5.4, 5.5_

- [ ] 9. Update `StaffTasksComponent` — Prediction panel

  - [~] 9.1 Add forecast loading and data state to the component
    - Add `forecast: EnrichedForecastSummary | null = null` and `isForecastLoading = false`.
    - In `ngOnInit`, call `this.staffService.getForecast()` with `catchError(() => of(null))` and assign result to `this.forecast`; set loading flags appropriately.
    - _Requirements: 6.1, 6.5, 6.6_

  - [~] 9.2 Add prediction panel template above the stats grid
    - Add a `<section class="prediction-panel">` before `<section class="stats-grid">`.
    - Skeleton placeholder (`*ngIf="isForecastLoading"`) — three animated skeleton rows matching the card style.
    - Forecast content (`*ngIf="forecast && !isForecastLoading"`):
      - Staffing subsection: one row per department (Housekeeping, Front Desk, Maintenance, Food & Beverage) showing department name and recommended headcount.
      - Inventory subsection: `*ngFor` over `forecast.inventoryRecommendations`, showing item name and `recommendedUnits`.
      - Outlook sentence: `<p>{{ forecast.operationalOutlook }}</p>`.
    - The panel is hidden entirely when `!isForecastLoading && !forecast` (error case) — no `*ngIf` wrapper needed beyond what is already conditional.
    - _Requirements: 6.2, 6.3, 6.4, 6.5, 6.6_

  - [~] 9.3 Add prediction panel styles
    - Add `.prediction-panel` CSS to the component's `styles` array — use the same glassmorphism card style as the existing `.board-card` and `.stat-card`, with a subtle teal accent border (`#14b8a6`) to distinguish it visually.
    - Add `.skeleton-row` — animated gradient shimmer (`background: linear-gradient(90deg, #e2e8f0 25%, #f8fbff 50%, #e2e8f0 75%); animation: shimmer 1.5s infinite`).
    - _Requirements: 6.2, 6.5_

  - [~] 9.4 Write property and example tests for the prediction panel
    - **Property 8: Forecast Panel Renders All Four Departments** — for any `EnrichedForecastSummary` with varying staffing numbers, the rendered panel contains "Housekeeping", "Front Desk", "Maintenance", and "Food & Beverage". Use parameterized tests with randomized integer values.
    - **Property 9: All Inventory Items Appear in the Forecast Panel** — for any list of 0–20 `InventoryItem` objects, each item's `item` name appears exactly once in the rendered DOM.
    - Example test: skeleton visible when `isForecastLoading = true`.
    - Example test: panel hidden when `forecast = null` and `isForecastLoading = false`.
    - _Requirements: 6.2, 6.3, 6.5, 6.6_

- [~] 10. Final Checkpoint — Ensure all tests pass
  - Run `dotnet test` in the workspace root and `ng test --run` (or Jest equivalent) in the `frontend/` directory. Ensure all property tests and example tests pass. Ask the user if any questions arise.

---

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP.
- Property tests require [FsCheck.Xunit](https://www.nuget.org/packages/FsCheck.Xunit) on the backend and Angular TestBed + parameterized Jest tests on the frontend.
- The OperationsAgent Bedrock configuration (`OperationsBedrock:AgentArn`) must be populated with the real AWS ARN before the integration path works end-to-end. The `OperationsBedrockService` throws at startup if the ARN is missing, so fill in a placeholder value in `appsettings.Development.json` for local development.
- The `operations-data-lookup` Lambda must be deployed and registered as a Bedrock action group in the AWS console before `GetOperationsForecastAsync` returns live data. Until then, the forecast endpoint falls back to the existing rule-based `OperationsAgent`.
- The `ForecastSummary` type in `app.models.ts` is preserved for backward compatibility; `EnrichedForecastSummary` extends it, so existing usages of `ForecastSummary` (e.g., the staff-forecast component) continue to compile.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["2.1", "5.1", "7.1"] },
    { "id": 2, "tasks": ["2.2", "5.2", "7.2"] },
    { "id": 3, "tasks": ["2.3", "2.4", "2.5"] },
    { "id": 4, "tasks": ["2.6", "4.1", "4.3"] },
    { "id": 5, "tasks": ["4.2", "4.4"] },
    { "id": 6, "tasks": ["8.1", "9.1"] },
    { "id": 7, "tasks": ["8.2", "9.2", "9.3"] },
    { "id": 8, "tasks": ["8.3", "9.4"] }
  ]
}
```
