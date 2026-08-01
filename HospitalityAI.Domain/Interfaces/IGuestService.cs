namespace HospitalityAI.Domain.Interfaces;

using HospitalityAI.Domain.Dtos;
using HospitalityAI.Domain.Models;

public interface IGuestService
{
    Task<Guest?> GetGuestAsync(string guestId, CancellationToken ct = default);
    Task<Guest?> AuthenticateGuestAsync(string reservationCode, CancellationToken ct = default);
    Task<Guest> UpdateGuestAsync(Guest guest, CancellationToken ct = default);
    Task<GuestDto> GetGuestProfileAsync(string guestId, CancellationToken ct = default);
    Task<BookingConfirmationDto> BookRecommendationAsync(string guestId, string recommendationId, CancellationToken ct = default);
    Task<GuestDto> CheckInAsync(string guestId, string reservationCode, CancellationToken ct = default);
    Task<GuestDto> CheckOutAsync(string guestId, CancellationToken ct = default);
}
