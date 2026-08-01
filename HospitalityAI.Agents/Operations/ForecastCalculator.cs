namespace HospitalityAI.Agents.Operations;

using HospitalityAI.Domain.Dtos;
using HospitalityAI.Domain.Models;

public sealed class ForecastCalculator
{
    public ForecastComputationResult Calculate(IReadOnlyList<OccupancyHistory> history)
    {
        if (history.Count == 0)
        {
            var fallbackInventory = new List<InventoryRecommendationDto>
            {
                new() { Item = "Bath towel & linen sets", RecommendedUnits = 20, Reason = "Buffer for day-of-arrival turnover demand." },
                new() { Item = "Guest toiletry kits", RecommendedUnits = 15, Reason = "Housekeeping cart restock buffer." }
            };

            return new ForecastComputationResult(0, 0, 3, 2, 0, fallbackInventory);
        }

        var orderedHistory = history.OrderBy(item => item.Date).TakeLast(14).ToList();
        var weights = Enumerable.Range(1, orderedHistory.Count).ToList();

        var weightedOccupancy = orderedHistory.Select((item, index) => item.OccupancyPercent * weights[index]).Sum() / weights.Sum();
        var weightedOrders = orderedHistory.Select((item, index) => item.RoomServiceOrders * weights[index]).Sum() / weights.Sum();
        var trend = orderedHistory.Last().OccupancyPercent - orderedHistory.First().OccupancyPercent;

        var predictedOccupancy = Math.Clamp(weightedOccupancy + trend * 0.15, 0, 100);
        var predictedOrders = (int)Math.Round(weightedOrders + (trend > 0 ? weightedOrders * 0.05 : 0));
        var recommendedHousekeeping = Math.Max(3, (int)Math.Ceiling(predictedOccupancy / 12));
        var recommendedFrontDesk = Math.Max(2, (int)Math.Ceiling(predictedOccupancy / 25));

        var occupiedRooms = (int)Math.Round(predictedOccupancy);
        var inventory = new List<InventoryRecommendationDto>
        {
            new() { Item = "Bath towel & linen sets", RecommendedUnits = occupiedRooms * 2 + 10, Reason = "2 sets per occupied room (~N rooms) plus a 10-set buffer for same-day turnovers." },
            new() { Item = "Guest toiletry kits", RecommendedUnits = occupiedRooms + 15, Reason = "1 kit per occupied room (~N rooms) plus 15 spares for housekeeping carts." },
            new() { Item = "Breakfast & room-service supplies", RecommendedUnits = predictedOrders * 3, Reason = "~3 supply units per predicted room-service order (N orders)." },
            new() { Item = "Minibar restock items", RecommendedUnits = (int)Math.Ceiling(occupiedRooms * 0.6), Reason = "~60% of occupied rooms (~N rooms) typically need a minibar restock between stays." },
            new() { Item = "Housekeeping cleaning supplies", RecommendedUnits = occupiedRooms + recommendedHousekeeping * 3, Reason = "1 unit per occupied room plus 3 per housekeeping staff member (N staff) for cart restocking." }
        };

        return new ForecastComputationResult(
            predictedOccupancy,
            predictedOrders,
            recommendedHousekeeping,
            recommendedFrontDesk,
            occupiedRooms,
            inventory);
    }
}

public sealed class ForecastComputationResult
{
    public ForecastComputationResult(double predictedOccupancy, int predictedOrders, int recommendedHousekeeping, int recommendedFrontDesk, int occupiedRooms, List<InventoryRecommendationDto> inventory)
    {
        PredictedOccupancy = predictedOccupancy;
        PredictedOrders = predictedOrders;
        RecommendedHousekeeping = recommendedHousekeeping;
        RecommendedFrontDesk = recommendedFrontDesk;
        OccupiedRooms = occupiedRooms;
        Inventory = inventory;
    }

    public double PredictedOccupancy { get; }
    public int PredictedOrders { get; }
    public int RecommendedHousekeeping { get; }
    public int RecommendedFrontDesk { get; }
    public int OccupiedRooms { get; }
    public List<InventoryRecommendationDto> Inventory { get; }
}
