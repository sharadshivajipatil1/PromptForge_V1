namespace HospitalityAI.Domain.Models;

using System.ComponentModel.DataAnnotations;

public class OccupancyHistory
{
    [Required]
    public string Date { get; set; } = string.Empty;

    [Range(0, 100)]
    public double OccupancyPercent { get; set; }

    [Range(0, int.MaxValue)]
    public int RoomServiceOrders { get; set; }
}
