using System.Security.Claims;
using HospitalityAI.Api.Controllers;
using HospitalityAI.Api.Hubs;
using HospitalityAI.Api.Services;
using HospitalityAI.Api.Hubs;
using HospitalityAI.Domain.Dtos;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;
using HospitalityAI.Infrastructure.Llm;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace HospitalityAI.Tests;

public class GuestControllerTests
{
    [Fact]
    public async Task CheckIn_AllowsAuthenticatedGuest_WhenGuestIdIsMissing()
    {
        var guestService = new Mock<IGuestService>();
        guestService
            .Setup(x => x.CheckInAsync("guest-1", "RES-8842", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuestDto { RoomNumber = "812", IsCheckedIn = true });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bedrock:Region"] = "us-east-1",
                ["Bedrock:AgentArn"] = "arn:aws:bedrock:us-east-1:123456789012:agent/test-agent",
                ["Bedrock:AgentAliasId"] = "TSTALIASID"
            })
            .Build();

        var controller = new GuestController(
            null!,
            null!,
            guestService.Object,
            null!,
            null!,
            null!,
            Mock.Of<IOtpService>(),
            new BedrockAgentService(configuration),
            Mock.Of<IHubContext<AgentActivityHub>>(),
            Mock.Of<IHubContext<DashboardHub>>(),
            Mock.Of<IDataStore>(),
            Mock.Of<RuntimeModeService>());
            new BedrockAgentService(configuration),
            Mock.Of<IHubContext<AgentActivityHub>>(),
            new RuntimeModeService());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "guest-1")
                }, "Test"))
            }
        };

        var result = await controller.CheckIn(new CheckInRequestDto
        {
            GuestId = string.Empty,
            ReservationCode = "RES-8842"
        }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        guestService.Verify(x => x.CheckInAsync("guest-1", "RES-8842", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Book_ReturnsConfirmation_ForAuthenticatedGuest()
    {
        var guestService = new Mock<IGuestService>();
        guestService
            .Setup(x => x.BookRecommendationAsync("guest-1", "slot-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookingConfirmationDto { Success = true, Message = "Booking confirmed.", ConfirmedSlotId = "slot-1", ConfirmedTime = DateTimeOffset.UtcNow });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bedrock:Region"] = "us-east-1",
                ["Bedrock:AgentArn"] = "arn:aws:bedrock:us-east-1:123456789012:agent/test-agent",
                ["Bedrock:AgentAliasId"] = "TSTALIASID"
            })
            .Build();

        var controller = new GuestController(
            null!,
            null!,
            guestService.Object,
            null!,
            null!,
            null!,
            Mock.Of<IOtpService>(),
            new BedrockAgentService(configuration),
            Mock.Of<IHubContext<AgentActivityHub>>(),
            new RuntimeModeService());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "guest-1")
                }, "Test"))
            }
        };

        var result = await controller.Book(new GuestController.BookingRequest
        {
            Category = "Spa & Wellness",
            SlotId = "slot-1",
            Title = "Massage therapy",
            SuggestedTime = "2026-08-02T16:00:00Z"
        }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<BookingConfirmationDto>(okResult.Value);
        Assert.True(response.Success);
        guestService.Verify(x => x.BookRecommendationAsync("guest-1", "slot-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Book_SavesTicketPriorityReason_ForGuestDashboardTracking()
    {
        var guestService = new Mock<IGuestService>();
        guestService
            .Setup(x => x.BookRecommendationAsync("guest-1", "slot-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookingConfirmationDto { Success = true, Message = "Booking confirmed.", ConfirmedSlotId = "slot-1", ConfirmedTime = DateTimeOffset.UtcNow });
        guestService
            .Setup(x => x.GetGuestAsync("guest-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guest
            {
                Id = "guest-1",
                FullName = "Priya Sharma",
                RoomNumber = "812"
            });

        var dataStore = new Mock<IDataStore>();
        dataStore
            .Setup(x => x.SaveTaskAsync(It.IsAny<StaffTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffTask task, CancellationToken _) => task);
        dataStore
            .Setup(x => x.SaveTicketAsync(It.IsAny<ConciergeTicket>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConciergeTicket ticket, CancellationToken _) => ticket);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bedrock:Region"] = "us-east-1",
                ["Bedrock:AgentArn"] = "arn:aws:bedrock:us-east-1:123456789012:agent/test-agent",
                ["Bedrock:AgentAliasId"] = "TSTALIASID"
            })
            .Build();

        var controller = new GuestController(
            null!,
            null!,
            guestService.Object,
            null!,
            null!,
            null!,
            Mock.Of<IOtpService>(),
            new BedrockAgentService(configuration),
            Mock.Of<IHubContext<AgentActivityHub>>(),
            new RuntimeModeService(),
            dataStore.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "guest-1")
                }, "Test"))
            }
        };

        await controller.Book(new GuestController.BookingRequest
        {
            Category = "Spa & Wellness",
            SlotId = "slot-1",
            Title = "Massage therapy",
            SuggestedTime = "2026-08-02T16:00:00Z"
        }, CancellationToken.None);

        dataStore.Verify(x => x.SaveTicketAsync(
            It.Is<ConciergeTicket>(ticket =>
                ticket.CreatedBy == "Guest" &&
                ticket.Remark == "Guest created this request from the dashboard." &&
                ticket.PriorityReason == "Guest requested a service booking from the dashboard."),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
