namespace HospitalityAI.Agents.Operations;

using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Models;

public sealed class PriorityCalculator
{
    private readonly KeywordMatcher _keywordMatcher;
    private readonly SafetyTriageService _safetyTriageService;
    private readonly PriorityReasonGenerator _priorityReasonGenerator;

    public PriorityCalculator(KeywordMatcher keywordMatcher, SafetyTriageService safetyTriageService, PriorityReasonGenerator priorityReasonGenerator)
    {
        _keywordMatcher = keywordMatcher;
        _safetyTriageService = safetyTriageService;
        _priorityReasonGenerator = priorityReasonGenerator;
    }

    public async Task<IReadOnlyList<StaffTask>> PrioritizeAsync(IEnumerable<StaffTask> tasks, DateTimeOffset now, CancellationToken ct = default)
    {
        var openTasks = tasks.Where(task => task.Status != TaskStatus.Completed).ToList();

        foreach (var task in openTasks)
        {
            var minutesElapsed = Math.Max(0, (now - task.CreatedAt).TotalMinutes);
            var slaRemainingRatio = 1 - (minutesElapsed / Math.Max(task.SlaMinutes, 1));
            var keywordMatch = _keywordMatcher.Match(task.Description);
            var aiTriage = await _safetyTriageService.TriageAsync(task.Description, ct);

            var priority = TaskPriority.Low;
            var aiFlaggedCritical = aiTriage.AiFlaggedCritical;
            var aiFlaggedUrgent = aiTriage.AiFlaggedUrgent;

            if (keywordMatch.MatchedHazard is not null || aiFlaggedCritical)
            {
                priority = TaskPriority.Critical;
                task.Priority = priority;
                task.PriorityReason = _priorityReasonGenerator.Generate(task, keywordMatch.MatchedHazard, keywordMatch.MatchedUrgent, aiFlaggedCritical, aiFlaggedUrgent, 0, slaRemainingRatio, priority);
                continue;
            }

            var typeSeverity = GetTypeSeverity(task.Type, keywordMatch.MatchedUrgent is not null || aiFlaggedUrgent);
            var urgencyScore = (1 - Math.Max(slaRemainingRatio, -1)) * 2 + typeSeverity;

            if (urgencyScore >= 4.5)
            {
                priority = TaskPriority.Critical;
            }
            else if (urgencyScore >= 3.0)
            {
                priority = TaskPriority.High;
            }
            else if (urgencyScore >= 1.8)
            {
                priority = TaskPriority.Medium;
            }
            else
            {
                priority = TaskPriority.Low;
            }

            if ((keywordMatch.MatchedUrgent is not null || aiFlaggedUrgent) && priority < TaskPriority.High)
            {
                priority = TaskPriority.High;
            }

            if (task.Type == TaskType.GuestRequest && priority < TaskPriority.High)
            {
                priority = TaskPriority.High;
            }

            task.Priority = priority;
            task.PriorityReason = _priorityReasonGenerator.Generate(task, keywordMatch.MatchedHazard, keywordMatch.MatchedUrgent, aiFlaggedCritical, aiFlaggedUrgent, urgencyScore, slaRemainingRatio, priority);
        }

        return openTasks.OrderByDescending(task => task.Priority).ThenBy(task => task.CreatedAt).ToList();
    }

    private static double GetTypeSeverity(TaskType type, bool hasUrgentMatch)
    {
        var baseSeverity = type switch
        {
            TaskType.Maintenance => 2,
            TaskType.GuestRequest => 2.5,
            TaskType.RoomService => 1,
            _ => 1
        };

        return hasUrgentMatch ? Math.Max(baseSeverity, 3) : baseSeverity;
    }
}
