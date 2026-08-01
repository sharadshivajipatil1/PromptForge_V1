namespace HospitalityAI.Domain.Dtos;

using HospitalityAI.Domain.Enums;

public class CheckInRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string GuestId { get; set; } = string.Empty;
    public CheckInRequestType Type { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public string IdDocumentImageBase64 { get; set; } = string.Empty;
    public string SelfieImageBase64 { get; set; } = string.Empty;
    public CheckInRequestStatus Status { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public bool DigitalKeyIssued { get; set; }
    public string VerificationSummary { get; set; } = string.Empty;
}
