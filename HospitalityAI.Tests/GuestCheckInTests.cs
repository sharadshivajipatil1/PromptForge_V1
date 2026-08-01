using HospitalityAI.Domain.Dtos;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;
using HospitalityAI.Infrastructure.Services;
using Moq;
using Xunit;

namespace HospitalityAI.Tests;

public class GuestCheckInTests
{
    [Fact]
    public async Task CheckInAsync_AcceptsGuestReservationFromSessionAndRequest()
    {
        var guest = new Guest
        {
            Id = "guest-1",
            FullName = "Test Guest",
            ReservationCode = "RES-8842",
            RoomNumber = "812"
        };

        var dataStore = new Mock<IDataStore>();
        dataStore.Setup(store => store.GetGuestByIdAsync("guest-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(guest);
        dataStore.Setup(store => store.SaveGuestAsync(It.IsAny<Guest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guest saved, CancellationToken _) => saved);

        var service = new GuestService(dataStore.Object);

        var result = await service.CheckInAsync("guest-1", "RES-8842", CancellationToken.None);

        Assert.True(result.IsCheckedIn);
        Assert.Equal("812", result.RoomNumber);
    }
}
