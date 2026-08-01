namespace HospitalityAI.Domain.Models;

using System.ComponentModel.DataAnnotations;
using HospitalityAI.Domain.Enums;

public class StaffTask
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public TaskType Type { get; set; } = TaskType.Housekeeping;

    [MaxLength(32)]
    public string RoomNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string Description { get; set; } = string.Empty;

    public TaskPriority Priority { get; set; } = TaskPriority.Low;

    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Range(1, int.MaxValue)]
    public int SlaMinutes { get; set; } = 20;

    [MaxLength(128)]
    public string? AssignedTo { get; set; }

    [MaxLength(128)]
    public string? Department { get; set; }

    [MaxLength(256)]
    public string? PriorityReason { get; set; }
}
