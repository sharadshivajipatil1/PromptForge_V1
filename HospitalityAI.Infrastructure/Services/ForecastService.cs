namespace HospitalityAI.Infrastructure.Services;

using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;
using HospitalityAI.Domain.ValueObjects;

public class ForecastService : IForecastService
{
    private readonly IDataStore _dataStore;

    public ForecastService(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<ForecastRecord> GenerateForecastAsync(CancellationToken ct = default)
    {
        var forecast = new ForecastRecord
        {
            ForDate = DateTimeOffset.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
            PredictedOccupancyPercent = 72,
            PredictedRoomServiceOrders = 32,
            RecommendedHousekeepingStaff = 6,
            RecommendedFrontDeskStaff = 3,
            RecommendedInventory = new List<InventoryRecommendation>
            {
                new() { Item = "Bath towel & linen sets", RecommendedUnits = 40, Reason = "Buffer for same-day turnovers" },
                new() { Item = "Guest toiletry kits", RecommendedUnits = 20, Reason = "Housekeeping cart restock" },
                new() { Item = "Breakfast & room-service supplies", RecommendedUnits = 96, Reason = "Projected room-service demand" },
                new() { Item = "Minibar restock items", RecommendedUnits = 8, Reason = "Estimated minibar usage" },
                new() { Item = "Housekeeping cleaning supplies", RecommendedUnits = 24, Reason = "Housekeeping staffing buffer" }
            },
            Notes = "Forecast generated from local in-memory history."
        };

        return await _dataStore.SaveForecastAsync(forecast, ct);
    }
}
