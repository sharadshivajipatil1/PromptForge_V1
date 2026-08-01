namespace HospitalityAI.Agents.Operations;

using HospitalityAI.Domain.Dtos;
using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;

public class OperationsAgent
{
    private readonly IDataStore _dataStore;
    private readonly ILlmClient _llmClient;

    public OperationsAgent(IDataStore dataStore, ILlmClient llmClient)
    {
        _dataStore = dataStore;
        _llmClient = llmClient;
    }

    public async Task<OperationsSnapshotDto> BuildSnapshotAsync(CancellationToken ct = default)
    {
        var openTasks = await _dataStore.GetOpenTasksAsync(ct);
        var forecasts = await _dataStore.GetForecastsAsync(page: 1, pageSize: 5, ct);
        var now = DateTimeOffset.UtcNow;
        var calculator = new PriorityCalculator(new KeywordMatcher(), new SafetyTriageService(_llmClient), new PriorityReasonGenerator());
        var prioritizedTasks = await calculator.PrioritizeAsync(openTasks, now, ct);
        var flaggedTasks = prioritizedTasks.Where(task => task.Priority is TaskPriority.High or TaskPriority.Critical).ToList();
        var triagedTasks = flaggedTasks.Select(task => new TriageResult(task, ClassifySafety(task))).ToList();

        var activitySnapshot = new OperationsSnapshotDto
        {
            GeneratedAt = now,
            OpenTaskCount = prioritizedTasks.Count,
            CriticalTaskCount = triagedTasks.Count(task => task.Result == SafetyResult.Critical),
            HighPriorityTaskCount = triagedTasks.Count(task => task.Result == SafetyResult.High),
            PendingTasks = prioritizedTasks.Select(MapTask).ToList(),
            TriageResults = triagedTasks.Select(MapTriage).ToList(),
            Forecasts = forecasts.Items.Select(MapForecast).ToList()
        };

        return activitySnapshot;
    }

    public virtual async Task<ForecastDto> GenerateForecastAsync(string? focusArea = null, CancellationToken ct = default)
    {
        var history = new List<OccupancyHistory>();
        var forecastCalculator = new ForecastCalculator();
        var result = forecastCalculator.Calculate(history);
        var now = DateTimeOffset.UtcNow;

        var notes = focusArea is null
            ? $"Forecast generated for {now:yyyy-MM-dd}. Occupancy is projected at {result.PredictedOccupancy:F0}% with {result.PredictedOrders} orders."
            : $"Forecast generated for {focusArea} on {now:yyyy-MM-dd}. Occupancy is projected at {result.PredictedOccupancy:F0}% with {result.PredictedOrders} orders.";

        var forecast = new ForecastRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            ForDate = now.Date.ToString("yyyy-MM-dd"),
            PredictedOccupancyPercent = result.PredictedOccupancy,
            PredictedRoomServiceOrders = result.PredictedOrders,
            RecommendedHousekeepingStaff = result.RecommendedHousekeeping,
            RecommendedFrontDeskStaff = result.RecommendedFrontDesk,
            RecommendedInventory = result.Inventory.Select(item => new HospitalityAI.Domain.ValueObjects.InventoryRecommendation { Item = item.Item, RecommendedUnits = item.RecommendedUnits, Reason = item.Reason }).ToList(),
            Notes = notes,
            GeneratedAt = now
        };

        await _dataStore.SaveForecastAsync(forecast, ct);

        return MapForecast(forecast);
    }

    public async Task<IReadOnlyList<StaffTask>> PrioritizeTasksAsync(CancellationToken ct = default)
    {
        var tasks = await _dataStore.GetOpenTasksAsync(ct);
        var calculator = new PriorityCalculator(new KeywordMatcher(), new SafetyTriageService(_llmClient), new PriorityReasonGenerator());
        return await calculator.PrioritizeAsync(tasks, DateTimeOffset.UtcNow, ct);
    }

    public async Task<string> GetOperationsNarrativeAsync(CancellationToken ct = default)
    {
        var snapshot = await BuildSnapshotAsync(ct);
        var prompt = $"Summarize these operations signals for a hotel operations lead in a concise executive brief. Include the count of open tasks, critical/high priority issues, and the forecast summary.\n\n{snapshot}";
        return await _llmClient.CompleteAsync("You are a hotel operations analyst.", prompt, ct);
    }

    private static SafetyResult ClassifySafety(StaffTask task)
    {
        var description = task.Description.ToLowerInvariant();
        if (description.Contains("injury") || description.Contains("bleeding") || description.Contains("fire") || description.Contains("gas") || description.Contains("seizure") || description.Contains("choking"))
        {
            return SafetyResult.Critical;
        }

        if (description.Contains("security") || description.Contains("unauthorized") || description.Contains("intruder") || description.Contains("assault") || description.Contains("theft") || description.Contains("overflow") || description.Contains("mold"))
        {
            return SafetyResult.High;
        }

        return SafetyResult.None;
    }

    private static OperationsTaskDto MapTask(StaffTask task)
    {
        return new OperationsTaskDto
        {
            Id = task.Id,
            Description = task.Description,
            Type = task.Type.ToString(),
            Priority = task.Priority.ToString(),
            PriorityReason = task.PriorityReason,
            CreatedAt = task.CreatedAt,
            SlaMinutes = task.SlaMinutes
        };
    }

    private static TriageResultDto MapTriage(TriageResult triage)
    {
        return new TriageResultDto
        {
            TaskId = triage.Task.Id,
            Result = triage.Result.ToString(),
            Reason = triage.Reason
        };
    }

    private static ForecastDto MapForecast(ForecastRecord forecast)
    {
        return new ForecastDto
        {
            Id = forecast.Id,
            ForDate = forecast.ForDate,
            PredictedOccupancyPercent = forecast.PredictedOccupancyPercent,
            PredictedRoomServiceOrders = forecast.PredictedRoomServiceOrders,
            RecommendedHousekeepingStaff = forecast.RecommendedHousekeepingStaff,
            RecommendedFrontDeskStaff = forecast.RecommendedFrontDeskStaff,
            RecommendedInventory = forecast.RecommendedInventory.Select(item => new InventoryRecommendationDto
            {
                Item = item.Item,
                RecommendedUnits = item.RecommendedUnits,
                Reason = item.Reason
            }).ToList(),
            Notes = forecast.Notes,
            GeneratedAt = forecast.GeneratedAt
        };
    }

    private sealed class TriageResult
    {
        public TriageResult(StaffTask task, SafetyResult result)
        {
            Task = task;
            Result = result;
            Reason = result switch
            {
                SafetyResult.Critical => "Critical safety issue detected by keyword-based triage.",
                SafetyResult.High => "High-risk safety issue detected by keyword-based triage.",
                _ => "No safety escalation required."
            };
        }

        public StaffTask Task { get; }
        public SafetyResult Result { get; }
        public string Reason { get; }
    }

    private enum SafetyResult
    {
        None,
        High,
        Critical
    }
}
