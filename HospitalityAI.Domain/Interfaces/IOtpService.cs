namespace HospitalityAI.Domain.Interfaces;

public interface IOtpService
{
    Task<(bool Sent, string MaskedPhone, string DemoOtp, string Message)> SendOtpAsync(string reservationCode, CancellationToken ct = default);
    Task<bool> VerifyOtpAsync(string otp, CancellationToken ct = default);
}
