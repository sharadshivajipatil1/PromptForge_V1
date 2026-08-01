namespace HospitalityAI.Domain.Models;

using System.ComponentModel.DataAnnotations;
using HospitalityAI.Domain.Enums;

public class CheckInRequest
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string GuestId { get; set; } = string.Empty;

    public CheckInRequestType Type { get; set; } = CheckInRequestType.CheckIn;

    [Required]
    [MaxLength(32)]
    public string ReservationCode { get; set; } = string.Empty;

    [MaxLength(256)]
    public string IdDocumentImageBase64 { get; set; } = string.Empty;

    [MaxLength(256)]
    public string SelfieImageBase64 { get; set; } = string.Empty;

    public CheckInRequestStatus Status { get; set; } = CheckInRequestStatus.Pending;

    public DateTimeOffset? VerifiedAt { get; set; }

    [MaxLength(32)]
    public string RoomNumber { get; set; } = string.Empty;

    public bool DigitalKeyIssued { get; set; }

    [MaxLength(512)]
    public string VerificationSummary { get; set; } = string.Empty;
}
