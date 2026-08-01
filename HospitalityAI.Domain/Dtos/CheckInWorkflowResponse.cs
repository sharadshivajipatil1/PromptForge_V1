namespace HospitalityAI.Domain.Dtos;

public class CheckInWorkflowResponse
{
    public string RequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public bool DigitalKeyIssued { get; set; }
    public string VerificationSummary { get; set; } = string.Empty;
    public List<RecommendationDto>? Recommendations { get; set; }
    public bool IsCheckedIn { get; set; }
    public string? FolioSummary { get; set; }
}
