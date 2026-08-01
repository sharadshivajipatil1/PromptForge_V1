namespace HospitalityAI.Infrastructure.Services;

using HospitalityAI.Domain.Dtos;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;
using HospitalityAI.Domain.ValueObjects;

public class GuestService : IGuestService
{
    private readonly IDataStore _dataStore;

    public GuestService(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<Guest?> GetGuestAsync(string guestId, CancellationToken ct = default)
    {
        return await _dataStore.GetGuestByIdAsync(guestId, ct);
    }

    public async Task<Guest?> AuthenticateGuestAsync(string reservationCode, CancellationToken ct = default)
    {
        return await _dataStore.GetGuestByReservationCodeAsync(reservationCode, ct);
    }

    public async Task<Guest> UpdateGuestAsync(Guest guest, CancellationToken ct = default)
    {
        return await _dataStore.SaveGuestAsync(guest, ct);
    }

    public async Task<GuestDto> GetGuestProfileAsync(string guestId, CancellationToken ct = default)
    {
        var guest = await _dataStore.GetGuestByIdAsync(guestId, ct);
        return guest is null ? new GuestDto() : Map(guest);
    }

    public async Task<BookingConfirmationDto> BookRecommendationAsync(string guestId, string recommendationId, CancellationToken ct = default)
    {
        var guest = await _dataStore.GetGuestByIdAsync(guestId, ct);
        if (guest is null)
        {
            return new BookingConfirmationDto { Success = false, Message = "Guest not found." };
        }

        return new BookingConfirmationDto
        {
            Success = true,
            Message = "Recommendation booked successfully.",
            ConfirmedSlotId = recommendationId,
            ConfirmedTime = DateTimeOffset.UtcNow
        };
    }

    public async Task<GuestDto> CheckInAsync(string guestId, string reservationCode, CancellationToken ct = default)
    {
        var guest = await _dataStore.GetGuestByIdAsync(guestId, ct);
        if (guest is null || !string.Equals(guest.ReservationCode, reservationCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Guest reservation could not be verified.");
        }

        guest.IsCheckedIn = true;
        guest = await _dataStore.SaveGuestAsync(guest, ct);
        return Map(guest);
    }

    public async Task<GuestDto> CheckOutAsync(string guestId, CancellationToken ct = default)
    {
        var guest = await _dataStore.GetGuestByIdAsync(guestId, ct);
        if (guest is null)
        {
            throw new InvalidOperationException("Guest not found.");
        }

        guest.IsCheckedIn = false;
        guest = await _dataStore.SaveGuestAsync(guest, ct);
        return Map(guest);
    }

    private static GuestDto Map(Guest guest)
    {
        return new GuestDto
        {
            Id = guest.Id,
            FullName = guest.FullName,
            ReservationCode = guest.ReservationCode,
            PreferredLanguage = guest.PreferredLanguage,
            RoomNumber = guest.RoomNumber,
            IsCheckedIn = guest.IsCheckedIn,
            History = guest.History.Select(entry => new GuestHistoryDto
            {
                Type = entry.Type,
                Description = entry.Description,
                Date = entry.Date,
                Rating = entry.Rating
            }).ToList()
        };
    }
}
