using HospitalityAI.Agents.Operations;
using HospitalityAI.Domain.Dtos;
using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;
using Moq;
using Xunit;
using TaskStatus = HospitalityAI.Domain.Enums.TaskStatus;

namespace HospitalityAI.Tests;

public class OperationsAgentTests
{
    [Fact]
    public async Task PrioritizeTasksAsync_UsesSafetyKeywords_AndElevatesPriority()
    {
        var dataStore = new Mock<IDataStore>();
        var llmClient = new Mock<ILlmClient>();
        var task = new StaffTask
        {
            Id = "task-1",
            Type = TaskType.Maintenance,
            Description = "Guest reported a medical injury in room 410",
            Status = TaskStatus.Pending,
            SlaMinutes = 30,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-40)
        };

        dataStore.Setup(store => store.GetOpenTasksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StaffTask> { task });
        dataStore.Setup(store => store.GetForecastsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ForecastRecord> { Items = new List<ForecastRecord>(), Page = 0, PageSize = 1, TotalCount = 10 });
        dataStore.Setup(store => store.GetGuestsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guest>());
        dataStore.Setup(store => store.SaveForecastAsync(It.IsAny<ForecastRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ForecastRecord forecast, CancellationToken _) => forecast);

        var agent = new OperationsAgent(dataStore.Object, llmClient.Object);
        var prioritized = await agent.PrioritizeTasksAsync();

        Assert.Single(prioritized);
        Assert.Equal(TaskPriority.Critical, prioritized[0].Priority);
        Assert.Contains("hazard", prioritized[0].PriorityReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateForecastAsync_ReturnsInventoryAndStaffRecommendations()
    {
        var dataStore = new Mock<IDataStore>();
        var llmClient = new Mock<ILlmClient>();
        var guest = new Guest { Id = "g-1", FullName = "Test Guest" };

        dataStore.Setup(store => store.GetGuestsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guest> { guest });
        dataStore.Setup(store => store.GetOpenTasksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StaffTask>());
        dataStore.Setup(store => store.GetForecastsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ForecastRecord> { Items = new List<ForecastRecord>(), Page = 0, PageSize = 1, TotalCount = 10 });
        dataStore.Setup(store => store.SaveForecastAsync(It.IsAny<ForecastRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ForecastRecord forecast, CancellationToken _) => forecast);

        var agent = new OperationsAgent(dataStore.Object, llmClient.Object);
        var forecast = await agent.GenerateForecastAsync();

        Assert.True(forecast.RecommendedHousekeepingStaff >= 1);
        Assert.True(forecast.RecommendedFrontDeskStaff >= 1);
        Assert.Contains(forecast.RecommendedInventory, item => item.Item == "Guest toiletry kits");
        Assert.Contains(forecast.RecommendedInventory, item => item.Item == "Bath towel & linen sets");
    }
}
