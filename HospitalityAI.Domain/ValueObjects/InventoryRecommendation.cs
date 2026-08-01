namespace HospitalityAI.Domain.ValueObjects;

using System.ComponentModel.DataAnnotations;

public class InventoryRecommendation
{
    [Required]
    [MaxLength(128)]
    public string Item { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int RecommendedUnits { get; set; }

    [Required]
    [MaxLength(256)]
    public string Reason { get; set; } = string.Empty;
}
