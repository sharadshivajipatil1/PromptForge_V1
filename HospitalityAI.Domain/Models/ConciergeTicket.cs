namespace HospitalityAI.Domain.Models;

using System.ComponentModel.DataAnnotations;

public class ConciergeTicket
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? GuestId { get; set; }

    [Required]
    [MaxLength(128)]
    public string GuestName { get; set; } = string.Empty;

    [MaxLength(32)]
    public string RoomNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(2048)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string Status { get; set; } = "Open";

    [MaxLength(64)]
    public string? CreatedBy { get; set; }

    [MaxLength(1024)]
    public string? Remark { get; set; }

    [MaxLength(1024)]
    public string? PriorityReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
