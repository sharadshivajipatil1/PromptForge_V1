using HospitalityAI.Agents.Operations;
using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;
using Moq;
using Xunit;

namespace HospitalityAI.Tests;

public class PriorityAlgorithmTests
{
    [Fact]
    public void KeywordMatcher_FindsHazardAndUrgentKeywords()
    {
        var matcher = new KeywordMatcher();
        var result = matcher.Match("There is a gas leak in the hallway and a blocked exit");

        Assert.Equal("gas leak", result.MatchedHazard);
        Assert.Equal("blocked exit", result.MatchedUrgent);
    }

    [Fact]
    public async Task SafetyTriageService_FlagsCriticalAndUrgentResponses()
    {
        var llm = new Mock<ILlmClient>();
        llm.Setup(client => client.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("CRITICAL: possible injury in room 402");

        var service = new SafetyTriageService(llm.Object);
        var result = await service.TriageAsync("Possible injury in room 402");

        Assert.True(result.AiFlaggedCritical);
        Assert.False(result.AiFlaggedUrgent);
    }

    [Fact]
    public void PriorityReasonGenerator_UsesSpecPhrasingPatterns()
    {
        var generator = new PriorityReasonGenerator();
        var task = new StaffTask
        {
            Type = TaskType.Maintenance,
            SlaMinutes = 20,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-18)
        };

        var reason = generator.Generate(task, "leak", null, false, false, 4.0, 0.1, TaskPriority.High);

        Assert.Contains("urgent", reason, StringComparison.OrdinalIgnoreCase);
    }
}
