namespace HospitalityAI.Agents;

using HospitalityAI.Agents.Operations;
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

    public async Task<IReadOnlyList<StaffTask>> PrioritizeTasksAsync(CancellationToken ct = default)
    {
        var tasks = await _dataStore.GetOpenTasksAsync(ct);
        var calculator = new PriorityCalculator(new KeywordMatcher(), new SafetyTriageService(_llmClient), new PriorityReasonGenerator());
        return await calculator.PrioritizeAsync(tasks, DateTimeOffset.UtcNow, ct);
    }

    public async Task<string> PrioritizeTasksAsync(string description, CancellationToken ct = default)
    {
        var task = new StaffTask
        {
            Description = description,
            Type = TaskType.GuestRequest,
            Status = TaskStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            SlaMinutes = 30
        };

        var prioritized = TaskPrioritizer.Prioritize(new[] { task }, DateTimeOffset.UtcNow);
        var result = prioritized.First();
        return $"{result.Priority} - {result.PriorityReason}";
    }

    public async Task<OperationsSnapshotDto> BuildSnapshotAsync(CancellationToken ct = default)
    {
        var snapshotAgent = new HospitalityAI.Agents.Operations.OperationsAgent(_dataStore, _llmClient);
        return await snapshotAgent.BuildSnapshotAsync(ct);
    }

    public virtual async Task<ForecastDto> GenerateForecastAsync(string? focusArea = null, CancellationToken ct = default)
    {
        var snapshotAgent = new HospitalityAI.Agents.Operations.OperationsAgent(_dataStore, _llmClient);
        return await snapshotAgent.GenerateForecastAsync(focusArea, ct);
    }

    public async Task<PriorityRecommendationDto> GetPriorityRecommendationAsync(string description, CancellationToken ct = default)
    {
        var priorityService = new HotelOperationsPriorityService();
        var assessment = priorityService.AssessPriority(description);
        
        return new PriorityRecommendationDto
        {
            Priority = assessment.Priority.ToString(),
            Reason = $"Score: {assessment.Score} - {assessment.Reason} (Response: {assessment.ResponseTime}, Dept: {assessment.Department})"
        };
    }
    
    public string GetPriorityAssessmentFormatted(string description, bool isVipGuest = false)
    {
        var priorityService = new HotelOperationsPriorityService();
        var assessment = priorityService.AssessPriority(description, isVipGuest: isVipGuest);
        
        // Return in the exact format specified
        return $"PRIORITY: {assessment.Priority}\n" +
               $"SCORE: {assessment.Score}\n" +
               $"REASON: {assessment.Reason}\n" +
               $"RESPONSE_TIME: {assessment.ResponseTime}\n" +
               $"DEPARTMENT: {assessment.Department}";
    }
}
