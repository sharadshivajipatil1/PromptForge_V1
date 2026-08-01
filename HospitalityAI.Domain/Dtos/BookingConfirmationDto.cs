namespace HospitalityAI.Domain.Dtos;

public class BookingConfirmationDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ConfirmedSlotId { get; set; }
    public DateTimeOffset? ConfirmedTime { get; set; }
}
