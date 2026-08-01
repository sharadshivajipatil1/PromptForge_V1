namespace HospitalityAI.Domain.Models;

using System.ComponentModel.DataAnnotations;

public class DiningOption
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [MaxLength(64)]
    public string Category { get; set; } = string.Empty;
}
