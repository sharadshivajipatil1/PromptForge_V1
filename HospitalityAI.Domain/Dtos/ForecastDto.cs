namespace HospitalityAI.Domain.Dtos;

public class ForecastDto
{
    public string Id { get; set; } = string.Empty;
    public string ForDate { get; set; } = string.Empty;
    public double PredictedOccupancyPercent { get; set; }
    public int PredictedRoomServiceOrders { get; set; }
    public int RecommendedHousekeepingStaff { get; set; }
    public int RecommendedFrontDeskStaff { get; set; }
    public List<InventoryRecommendationDto> RecommendedInventory { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public int RecommendedMaintenanceStaff { get; set; }
    public int RecommendedFoodBeverageStaff { get; set; }
    public string OperationalOutlook { get; set; } = string.Empty;
}

public class InventoryRecommendationDto
{
    public string Item { get; set; } = string.Empty;
    public int RecommendedUnits { get; set; }
    public string Reason { get; set; } = string.Empty;
}
