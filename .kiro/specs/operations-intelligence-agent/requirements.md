# Requirements Document

## Introduction

The Operations Intelligence Agent extends the HospitalityAI hotel management system with two AI-driven capabilities for hotel staff:

1. **AI Task Priority Assignment** — When a staff member types a task description in the dashboard, the system sends it to a dedicated AWS Bedrock OperationsAgent before saving. The agent analyzes the description and returns a recommended priority (Low / Medium / High / Critical) with a brief reason. The staff member sees the AI suggestion inline in the task creation form and can accept or override it before saving.

2. **Staff and Inventory Prediction Panel** — When the staff dashboard loads, a prediction panel is displayed showing recommended staffing levels by department, inventory items that need restocking, and an operational outlook for the day. These predictions come from the OperationsAgent, which calls a Lambda function (`operations-data-lookup`) that returns embedded historical hotel data (occupancy rates, task volumes, seasonal patterns, inventory consumption rates).

The feature introduces a new dedicated AWS Bedrock agent separate from the existing ConciergeAgent, a new Lambda function for historical data retrieval, two new backend API endpoints, and UI changes to the Angular StaffTasksComponent.

---

## Glossary

- **OperationsAgent (Bedrock)**: The new AWS Bedrock agent dedicated to operations intelligence, distinct from the existing ConciergeAgent. Backed by the Nova Lite model and equipped with the `operations-data-lookup` action group.
- **OperationsBedrockService**: The new ASP.NET Core service that wraps calls to the OperationsAgent (Bedrock), mirroring the pattern of `BedrockAgentService` for the ConciergeAgent.
- **operations-data-lookup Lambda**: The new AWS Lambda function that returns an embedded JSON dataset containing historical hotel occupancy, task volumes, seasonal patterns, and inventory consumption rates.
- **Priority Recommendation**: An AI-generated assessment consisting of a `TaskPriority` value (Low / Medium / High / Critical) and a brief plain-text reason string, returned before the staff member saves a new task.
- **Prediction Panel**: The UI section rendered at the top of the staff dashboard (`/staff/tasks`) that displays staffing level recommendations by department, inventory restock recommendations, and an operational outlook.
- **StaffTasksComponent**: The existing Angular 17 standalone component at `/staff/tasks` that renders the staff operations dashboard.
- **StaffService**: The existing Angular service that communicates with the ASP.NET Core `/api/dashboard` endpoints.
- **StaffController**: The existing ASP.NET Core controller at `/api/dashboard` that handles staff task CRUD, profile, and forecast operations.
- **ForecastDto**: The existing data transfer object that carries staffing and inventory forecast data from the backend to the frontend.
- **TaskPriority**: The existing enum (Low / Medium / High / Critical) used throughout the system to classify task urgency.
- **IDataStore**: The existing in-memory data store interface with `SaveTaskAsync` and `GetTasksAsync`.

---

## Requirements

### Requirement 1: AI Priority Recommendation Endpoint

**User Story:** As a staff member, I want the system to recommend a task priority before I save a new task, so that I can make better-prioritization decisions without having to classify urgency manually.

#### Acceptance Criteria

1. WHEN a POST request is made to `/api/dashboard/task-priority` with a non-empty `description` field, THE StaffController SHALL invoke the OperationsBedrockService and return a JSON response containing a `priority` string (one of: Low, Medium, High, Critical) and a `reason` string.
2. WHEN the OperationsBedrockService receives a task description, THE OperationsBedrockService SHALL send the description to the OperationsAgent (Bedrock) using a session scoped to the operations domain.
3. WHEN the OperationsAgent (Bedrock) returns a response, THE OperationsBedrockService SHALL parse the response and extract a valid `TaskPriority` value; IF no valid priority token is found in the response, THE OperationsBedrockService SHALL default to `Medium`.
4. IF the POST body `description` field is empty or whitespace, THEN THE StaffController SHALL return HTTP 400 with a descriptive error message.
5. IF the OperationsBedrockService call throws an exception, THEN THE StaffController SHALL return HTTP 500 with an error message and SHALL log the exception.
6. THE `/api/dashboard/task-priority` endpoint SHALL require the `Staff` role JWT claim, consistent with all other endpoints on StaffController.

### Requirement 2: Enhanced Forecast Endpoint

**User Story:** As a staff member, I want the dashboard forecast to include department-level staffing recommendations and inventory restock items powered by historical data, so that I can plan the day's operations proactively.

#### Acceptance Criteria

1. WHEN a GET request is made to `/api/dashboard/forecast`, THE StaffController SHALL invoke the OperationsBedrockService to request a prediction based on historical hotel data and return an enriched `ForecastDto` including `recommendedHousekeepingStaff`, `recommendedFrontDeskStaff`, `recommendedMaintenanceStaff`, `recommendedFoodBeverageStaff`, `inventoryRecommendations`, and an `operationalOutlook` string.
2. WHEN the OperationsBedrockService calls the OperationsAgent (Bedrock) for a forecast, THE OperationsBedrockService SHALL include a prompt that instructs the agent to use the `operations-data-lookup` action group to retrieve historical data before generating predictions.
3. WHEN the OperationsAgent (Bedrock) invokes the `operations-data-lookup` Lambda, THE Lambda SHALL return a JSON payload containing: historical occupancy rates by day of week, average task volumes by type and day, inventory consumption rates, and current inventory levels.
4. WHEN the `operations-data-lookup` Lambda is invoked, THE Lambda SHALL return data within 5 seconds.
5. IF the OperationsBedrockService call for forecast throws an exception, THEN THE StaffController SHALL fall back to the existing rule-based `OperationsAgent.GenerateForecastAsync` result and SHALL log the exception.
6. THE `/api/dashboard/forecast` endpoint SHALL continue to require the `Staff` role JWT claim.
7. THE ForecastDto SHALL be extended to include `recommendedMaintenanceStaff` (integer), `recommendedFoodBeverageStaff` (integer), and `operationalOutlook` (string) fields in addition to the existing fields.

### Requirement 3: OperationsBedrockService

**User Story:** As a developer, I want a dedicated service for communicating with the OperationsAgent (Bedrock), so that operations concerns are cleanly separated from the guest-facing ConciergeAgent Bedrock integration.

#### Acceptance Criteria

1. THE OperationsBedrockService SHALL read its agent configuration from `appsettings.json` using the keys `OperationsBedrock:Region`, `OperationsBedrock:AgentArn`, `OperationsBedrock:AgentAliasId`, `OperationsBedrock:AccessKey`, and `OperationsBedrock:SecretKey`, following the same credential fallback pattern as `BedrockAgentService`.
2. THE OperationsBedrockService SHALL expose a `GetPriorityRecommendationAsync(string description, CancellationToken ct)` method that returns a `PriorityRecommendationDto` containing `priority` and `reason` fields.
3. THE OperationsBedrockService SHALL expose a `GetOperationsForecastAsync(CancellationToken ct)` method that returns an enriched `ForecastDto`.
4. THE OperationsBedrockService SHALL use a stable session ID format of `ops-priority-{date}` for priority requests and `ops-forecast-{date}` for forecast requests, where `{date}` is the current UTC date in `yyyyMMdd` format.
5. THE OperationsBedrockService SHALL be registered in `Program.cs` as a singleton, consistent with the `BedrockAgentService` registration pattern.
6. WHERE the `OperationsBedrock:AgentArn` configuration key is absent or empty, THE OperationsBedrockService SHALL throw an `InvalidOperationException` at startup with a descriptive message.

### Requirement 4: operations-data-lookup Lambda

**User Story:** As a developer, I want a Lambda function that provides historical hotel operations data, so that the OperationsAgent can ground its predictions in real patterns rather than relying solely on its training data.

#### Acceptance Criteria

1. THE operations-data-lookup Lambda SHALL be implemented as a Node.js 20 function and SHALL return a JSON response containing historical occupancy by day of week (Monday–Sunday), average task volumes by type (Housekeeping, Maintenance, GuestRequest, RoomService) and day, inventory consumption rates per occupied room per day, and current inventory levels for at least 5 item categories.
2. WHEN the operations-data-lookup Lambda is invoked by the Bedrock agent action group, THE Lambda SHALL return an HTTP 200 response with the JSON dataset in the response body.
3. THE operations-data-lookup Lambda SHALL embed its dataset as a constant within the function code — no external database or S3 calls are required.
4. THE operations-data-lookup Lambda SHALL include seasonal adjustment factors (peak season multipliers for Summer and December) within the embedded dataset.

### Requirement 5: Frontend — AI Priority Suggestion in Task Creation Form

**User Story:** As a staff member, I want to see an AI-suggested priority while I am composing a new task, so that I can quickly decide the appropriate urgency before submitting.

#### Acceptance Criteria

1. WHEN a staff member has typed at least 10 characters into the task description input and pauses typing for 800 milliseconds, THE StaffTasksComponent SHALL call `StaffService.getPriorityRecommendation()` with the current description value.
2. WHEN `StaffService.getPriorityRecommendation()` returns a result, THE StaffTasksComponent SHALL display the recommended priority label and reason text inline beneath the description input field within the task creation form, before the staff member submits.
3. WHEN the staff member submits the task creation form, THE StaffTasksComponent SHALL include the AI-suggested priority value in the task payload sent to `POST /api/dashboard/tasks`; IF the staff member has not received a recommendation yet, THE StaffTasksComponent SHALL submit without a priority override (defaulting to the server-side value).
4. WHEN `StaffService.getPriorityRecommendation()` is loading, THE StaffTasksComponent SHALL display a loading indicator in the priority suggestion area.
5. IF `StaffService.getPriorityRecommendation()` returns an error, THEN THE StaffTasksComponent SHALL hide the suggestion area and allow the form to proceed normally without blocking task creation.
6. THE StaffService SHALL expose a `getPriorityRecommendation(description: string): Observable<PriorityRecommendation>` method that calls `POST /api/dashboard/task-priority`.

### Requirement 6: Frontend — Prediction Panel

**User Story:** As a staff member, I want to see a prediction panel at the top of the dashboard when the page loads, so that I can immediately understand staffing needs and inventory status for the day.

#### Acceptance Criteria

1. WHEN the StaffTasksComponent initializes (`ngOnInit`), THE StaffTasksComponent SHALL call `StaffService.getForecast()` and display the result in a prediction panel positioned above the statistics grid.
2. WHEN the forecast data is available, THE StaffTasksComponent SHALL display a staffing section showing recommended headcount for Housekeeping, Front Desk, Maintenance, and Food & Beverage departments.
3. WHEN the forecast data is available, THE StaffTasksComponent SHALL display an inventory section listing each item name and recommended unit count from `inventoryRecommendations`.
4. WHEN the forecast data is available, THE StaffTasksComponent SHALL display the `operationalOutlook` string in the prediction panel.
5. WHILE the forecast is loading, THE StaffTasksComponent SHALL display a skeleton loading placeholder in the prediction panel area.
6. IF `StaffService.getForecast()` returns an error, THEN THE StaffTasksComponent SHALL hide the prediction panel and SHALL NOT block the rest of the dashboard from loading.
7. THE StaffService `getForecast()` method SHALL be updated to return an `EnrichedForecastSummary` that includes `recommendedMaintenanceStaff`, `recommendedFoodBeverageStaff`, and `operationalOutlook` in addition to the existing `ForecastSummary` fields.
8. THE `app.models.ts` `ForecastSummary` interface SHALL be updated to `EnrichedForecastSummary` with the three additional fields added in Requirement 2.7.

### Requirement 7: Backend Task Creation Priority Override

**User Story:** As a developer, I want the task creation endpoint to accept an optional AI-suggested priority so that the staff member's confirmed priority recommendation is persisted without requiring a separate update call.

#### Acceptance Criteria

1. THE `CreateTaskRequest` model on StaffController SHALL include an optional `priority` field (string, nullable).
2. WHEN the `priority` field in a `CreateTaskRequest` is a valid `TaskPriority` enum value, THE StaffController SHALL use it as the task's priority instead of the default `TaskPriority.Medium`.
3. WHEN the `priority` field in a `CreateTaskRequest` is null, empty, or not a recognized `TaskPriority` value, THE StaffController SHALL default the task priority to `TaskPriority.Medium` and continue saving.
