namespace HospitalityAI.Domain.Dtos;

public class SpaSlotDto
{
    public string Id { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public int DurationHours { get; set; }
    public bool IsAvailable { get; set; }
}
