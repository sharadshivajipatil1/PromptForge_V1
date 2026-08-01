namespace HospitalityAI.Domain.Models;

using System.ComponentModel.DataAnnotations;

public class SpaSlot
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(128)]
    public string ServiceName { get; set; } = string.Empty;

    public DateTimeOffset StartTime { get; set; }

    [Range(1, 24)]
    public int DurationHours { get; set; } = 1;

    public bool IsAvailable { get; set; } = true;
}
