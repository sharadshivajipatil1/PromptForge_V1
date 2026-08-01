namespace HospitalityAI.Domain.Dtos;

public class OccupancyDto
{
    public string Date { get; set; } = string.Empty;
    public double OccupancyPercent { get; set; }
    public int RoomServiceOrders { get; set; }
}
