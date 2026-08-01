namespace HospitalityAI.Domain.Interfaces;

using HospitalityAI.Domain.Auth;

public interface IAuthService
{
    Task<AuthResponse?> AuthenticateGuestAsync(GuestLoginRequest request, CancellationToken ct = default);
    Task<AuthResponse?> AuthenticateStaffAsync(StaffLoginRequest request, CancellationToken ct = default);
}
