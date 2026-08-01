namespace HospitalityAI.Agents.Operations;

using HospitalityAI.Domain.Interfaces;

public sealed class SafetyTriageService
{
    private readonly ILlmClient _llmClient;

    public SafetyTriageService(ILlmClient llmClient)
    {
        _llmClient = llmClient;
    }

    public async Task<SafetyTriageResult> TriageAsync(string description, CancellationToken ct = default)
    {
        var response = await _llmClient.CompleteAsync("You are the Operations agent's AI safety-triage assistant for a hotel. Read the task description and decide whether it describes a life-safety hazard, an urgent (but non-life-safety) issue, or a routine task - even if it doesn't contain obvious keywords. Respond with exactly one line starting with 'CRITICAL: ', 'URGENT: ', or 'ROUTINE: ' followed by a short reason, and nothing else.", description, ct);
        var normalized = response?.Trim() ?? string.Empty;

        if (normalized.StartsWith("CRITICAL:", StringComparison.OrdinalIgnoreCase))
        {
            return new SafetyTriageResult(true, false, normalized);
        }

        if (normalized.StartsWith("URGENT:", StringComparison.OrdinalIgnoreCase))
        {
            return new SafetyTriageResult(false, true, normalized);
        }

        return new SafetyTriageResult(false, false, normalized);
    }
}

public sealed class SafetyTriageResult
{
    public SafetyTriageResult(bool flaggedCritical, bool flaggedUrgent, string rawResponse)
    {
        AiFlaggedCritical = flaggedCritical;
        AiFlaggedUrgent = flaggedUrgent;
        RawResponse = rawResponse;
    }

    public bool AiFlaggedCritical { get; }
    public bool AiFlaggedUrgent { get; }
    public string RawResponse { get; }
}
