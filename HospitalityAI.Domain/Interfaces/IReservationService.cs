namespace HospitalityAI.Domain.Interfaces;

public interface IReservationService
{
    Task<string?> GetReservationCodeByGuestIdAsync(string guestId, CancellationToken ct = default);
}
