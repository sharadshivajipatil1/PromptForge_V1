using HospitalityAI.Agents;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;
using Moq;
using Xunit;

namespace HospitalityAI.Tests;

public class WorkflowOrchestratorTests
{
    [Fact]
    public async Task RouteAsync_ForecastKeyword_GeneratesForecastShortcut()
    {
        var concierge = new Mock<ConciergeAgent>(MockBehavior.Strict, new object?[] { null!, null!, null!, null! });
        var operations = new Mock<OperationsAgent>(MockBehavior.Strict, new object?[] { null!, null! });
        var dataStore = new Mock<IDataStore>();

        var forecast = new HospitalityAI.Domain.Dtos.ForecastDto
        {
            PredictedOccupancyPercent = 72,
            PredictedRoomServiceOrders = 14,
            Notes = "Forecast ready."
        };

        var operationsAgent = new Mock<OperationsAgent>(MockBehavior.Strict, new object?[] { null!, null! });
        operationsAgent.Setup(agent => agent.GenerateForecastAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(forecast);

        var orchestrator = new WorkflowOrchestrator(concierge.Object, operationsAgent.Object, dataStore.Object);
        var response = await orchestrator.RouteAsync("Please forecast occupancy for tomorrow.");

        Assert.Contains("Forecast received", response);
        Assert.Contains("72%", response);
    }

    [Fact]
    public async Task RouteAsync_OperationalKeyword_CreatesHousekeepingTask()
    {
        var concierge = new Mock<ConciergeAgent>(MockBehavior.Strict, new object?[] { null!, null!, null!, null! });
        var operations = new Mock<OperationsAgent>(MockBehavior.Strict, new object?[] { null!, null! });
        var dataStore = new Mock<IDataStore>();
        dataStore.Setup(store => store.SaveTaskAsync(It.IsAny<StaffTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffTask task, CancellationToken _) => task);

        var orchestrator = new WorkflowOrchestrator(concierge.Object, operations.Object, dataStore.Object);
        var response = await orchestrator.RouteAsync("Please clean my room and replace the towel.");

        Assert.Equal("Your request has been logged with the operations team.", response);
        dataStore.Verify(store => store.SaveTaskAsync(It.Is<StaffTask>(task => task.Description == "Please clean my room and replace the towel."), It.IsAny<CancellationToken>()), Times.Once);
    }
}
