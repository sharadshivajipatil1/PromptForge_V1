using HospitalityAI.Agents.Operations;
using HospitalityAI.Domain.Models;
using Xunit;

namespace HospitalityAI.Tests;

public class ForecastAlgorithmTests
{
    [Fact]
    public void ForecastCalculator_UsesWeightedMovingAverageAndTrend()
    {
        var history = new List<OccupancyHistory>
        {
            new() { Date = "2026-07-01", OccupancyPercent = 40, RoomServiceOrders = 6 },
            new() { Date = "2026-07-02", OccupancyPercent = 45, RoomServiceOrders = 7 },
            new() { Date = "2026-07-03", OccupancyPercent = 50, RoomServiceOrders = 8 },
            new() { Date = "2026-07-04", OccupancyPercent = 55, RoomServiceOrders = 9 },
            new() { Date = "2026-07-05", OccupancyPercent = 60, RoomServiceOrders = 10 },
            new() { Date = "2026-07-06", OccupancyPercent = 65, RoomServiceOrders = 11 },
            new() { Date = "2026-07-07", OccupancyPercent = 70, RoomServiceOrders = 12 },
            new() { Date = "2026-07-08", OccupancyPercent = 75, RoomServiceOrders = 13 },
            new() { Date = "2026-07-09", OccupancyPercent = 80, RoomServiceOrders = 14 },
            new() { Date = "2026-07-10", OccupancyPercent = 85, RoomServiceOrders = 15 },
            new() { Date = "2026-07-11", OccupancyPercent = 90, RoomServiceOrders = 16 },
            new() { Date = "2026-07-12", OccupancyPercent = 95, RoomServiceOrders = 17 },
            new() { Date = "2026-07-13", OccupancyPercent = 100, RoomServiceOrders = 18 },
            new() { Date = "2026-07-14", OccupancyPercent = 105, RoomServiceOrders = 19 }
        };

        var calculator = new ForecastCalculator();
        var result = calculator.Calculate(history);

        Assert.True(result.PredictedOccupancy > 0);
        Assert.True(result.PredictedOrders > 0);
        Assert.True(result.RecommendedHousekeeping >= 3);
        Assert.True(result.RecommendedFrontDesk >= 2);
        Assert.Equal(5, result.Inventory.Count);
    }
}
