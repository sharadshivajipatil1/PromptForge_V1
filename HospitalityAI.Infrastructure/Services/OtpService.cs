namespace HospitalityAI.Infrastructure.Services;

using HospitalityAI.Domain.Interfaces;

public class OtpService : IOtpService
{
    private readonly IDataStore _dataStore;

    public OtpService(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Task<(bool Sent, string MaskedPhone, string DemoOtp, string Message)> SendOtpAsync(string reservationCode, CancellationToken ct = default)
    {
        var guest = _dataStore.GetGuestByReservationCodeAsync(reservationCode, ct).GetAwaiter().GetResult();
        if (guest is null)
        {
            return Task.FromResult((false, string.Empty, string.Empty, "Reservation not found."));
        }

        var demoOtp = new Random().Next(100000, 999999).ToString();
        return Task.FromResult((true, MaskPhone(guest.Phone), demoOtp, "Demo OTP generated successfully."));
    }

    public Task<bool> VerifyOtpAsync(string otp, CancellationToken ct = default)
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(otp));
    }

    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return "***";
        }

        return phone.Length <= 4 ? new string('*', phone.Length) : $"***{phone[^4..]}";
    }
}
