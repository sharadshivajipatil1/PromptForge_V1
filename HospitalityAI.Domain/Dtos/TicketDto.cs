namespace HospitalityAI.Domain.Dtos;

public class TicketDto
{
    public string Id { get; set; } = string.Empty;
    public string? GuestId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string? CreatedBy { get; set; }
    public string? Remark { get; set; }
    public string? PriorityReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
