namespace HospitalityAI.Domain.Dtos;

using HospitalityAI.Domain.Enums;

public class TaskDto
{
    public string Id { get; set; } = string.Empty;
    public TaskType Type { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int SlaMinutes { get; set; }
    public string? AssignedTo { get; set; }
    public string? Department { get; set; }
    public string? PriorityReason { get; set; }
}
