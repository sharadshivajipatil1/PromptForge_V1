namespace HospitalityAI.Infrastructure.Services;

using HospitalityAI.Domain.Interfaces;

public class ReservationService : IReservationService
{
    private readonly IDataStore _dataStore;

    public ReservationService(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<string?> GetReservationCodeByGuestIdAsync(string guestId, CancellationToken ct = default)
    {
        var guest = await _dataStore.GetGuestByIdAsync(guestId, ct);
        return guest?.ReservationCode;
    }
}
