namespace HospitalityAI.Agents.Operations;

using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Models;

public sealed class PriorityReasonGenerator
{
    public string Generate(StaffTask task, string? matchedHazard, string? matchedUrgent, bool aiFlaggedCritical, bool aiFlaggedUrgent, double urgencyScore, double slaRemainingRatio, TaskPriority priority)
    {
        if (matchedHazard is not null || aiFlaggedCritical)
        {
            return matchedHazard is not null
                ? $"Flagged as a hazard ('{matchedHazard}') - urgent escalation to Critical priority regardless of SLA timing."
                : $"Flagged as a hazard by AI triage ('{task.Description}') - urgent escalation to Critical priority regardless of SLA timing.";
        }

        if (matchedUrgent is not null || aiFlaggedUrgent || (matchedHazard is not null && LooksUrgent(matchedHazard)))
        {
            return matchedUrgent is not null
                ? $"Flagged as urgent ('{matchedUrgent}') - held to at least High priority regardless of SLA timing."
                : matchedHazard is not null
                    ? $"Flagged as urgent ('{matchedHazard}') - held to at least High priority regardless of SLA timing."
                    : $"Flagged as urgent by AI triage - held to at least High priority regardless of SLA timing.";
        }

        if (task.Type == TaskType.GuestRequest && priority < TaskPriority.High)
        {
            return "Guest-reported requests came directly from a guest via chat and must never wait behind routine tasks.";
        }

        if (priority == TaskPriority.Critical)
        {
            return $"Urgency score {urgencyScore:F1} exceeded the Critical threshold.";
        }

        if (priority == TaskPriority.High)
        {
            return $"Urgency score {urgencyScore:F1} reached High priority.";
        }

        if (priority == TaskPriority.Medium)
        {
            return $"Urgency score {urgencyScore:F1} reached Medium priority.";
        }

        var minutesElapsed = Math.Max(0, (DateTimeOffset.UtcNow - task.CreatedAt).TotalMinutes);
        var slaMinutes = Math.Max(task.SlaMinutes, 1);
        if (minutesElapsed >= slaMinutes)
        {
            return $"SLA breached by {(int)Math.Ceiling(minutesElapsed - slaMinutes)} min ({task.Type})";
        }

        return $"{slaRemainingRatio:P0} of {slaMinutes}-min SLA remaining ({task.Type})";
    }

    private static bool LooksUrgent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("leak", StringComparison.OrdinalIgnoreCase)
            || value.Contains("flood", StringComparison.OrdinalIgnoreCase)
            || value.Contains("power", StringComparison.OrdinalIgnoreCase)
            || value.Contains("glass", StringComparison.OrdinalIgnoreCase)
            || value.Contains("exit", StringComparison.OrdinalIgnoreCase);
    }
}
