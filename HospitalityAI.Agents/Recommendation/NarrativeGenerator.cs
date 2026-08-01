namespace HospitalityAI.Agents.Recommendation;

using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;

public class NarrativeGenerator
{
    private readonly ILlmClient _llmClient;

    public NarrativeGenerator(ILlmClient llmClient)
    {
        _llmClient = llmClient;
    }

    public Task<string> GenerateAsync(Guest guest, string season, IEnumerable<string> recommendationSummaries, CancellationToken ct = default)
    {
        var prompt = "You are the Concierge agent narrating this guest's journey so far. Write 2-3 warm, story-like sentences that connect the guest's profile and the chosen recommendations.";
        var userPrompt = $"Guest: {guest.FullName}\nLoyalty: {guest.LoyaltyTier}\nProfession: {guest.Profession}\nSeason: {season}\nRecommendations: {string.Join(" | ", recommendationSummaries)}";
        return _llmClient.CompleteAsync(prompt, userPrompt, ct);
    }
}
