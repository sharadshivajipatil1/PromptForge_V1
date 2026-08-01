namespace HospitalityAI.Domain.ValueObjects;

using System.ComponentModel.DataAnnotations;

public class HistoryEntry
{
    [Required]
    [MaxLength(64)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Date { get; set; } = string.Empty;

    [Range(1, 5)]
    public int? Rating { get; set; }
}
