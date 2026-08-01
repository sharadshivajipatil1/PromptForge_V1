namespace HospitalityAI.Domain.Dtos;

public class PersonalizationResponse
{
    public string GuestId { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string LoyaltyTier { get; set; } = string.Empty;
    public List<HistoryMomentDto> RecentMoments { get; set; } = new();
    public List<RecommendationDto> Recommendations { get; set; } = new();
    public string AgentNarrative { get; set; } = string.Empty;
    public List<string> ReasoningSteps { get; set; } = new();
}
