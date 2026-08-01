namespace HospitalityAI.Domain.Models;

using System.ComponentModel.DataAnnotations;
using HospitalityAI.Domain.ValueObjects;

public class ForecastRecord
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string ForDate { get; set; } = string.Empty;

    [Range(0, 100)]
    public double PredictedOccupancyPercent { get; set; }

    [Range(0, int.MaxValue)]
    public int PredictedRoomServiceOrders { get; set; }

    [Range(0, int.MaxValue)]
    public int RecommendedHousekeepingStaff { get; set; }

    [Range(0, int.MaxValue)]
    public int RecommendedFrontDeskStaff { get; set; }

    public List<InventoryRecommendation> RecommendedInventory { get; set; } = new();

    [MaxLength(2048)]
    public string Notes { get; set; } = string.Empty;

    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}
