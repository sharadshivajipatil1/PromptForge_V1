namespace HospitalityAI.Domain.Dtos;

public class RecommendationDto
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset SuggestedTime { get; set; }
    public double Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? BookingRefId { get; set; }
}
