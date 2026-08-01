namespace HospitalityAI.Infrastructure.Authentication;

using HospitalityAI.Domain.Auth;
using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;

public class AuthService : IAuthService
{
    private readonly IDataStore _dataStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IDataStore dataStore, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _dataStore = dataStore;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse?> AuthenticateGuestAsync(GuestLoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ReservationCode))
        {
            return null;
        }

        var guest = await _dataStore.GetGuestByReservationCodeAsync(request.ReservationCode.Trim(), ct);
        if (guest is null)
        {
            return null;
        }

        var claims = new Dictionary<string, string>
        {
            ["sub"] = guest.Id,
            ["role"] = "Guest",
            ["roomNumber"] = guest.RoomNumber,
            ["checkedIn"] = guest.IsCheckedIn.ToString().ToLowerInvariant(),
            ["name"] = guest.FullName
        };

        return new AuthResponse
        {
            Token = _jwtTokenService.CreateToken(guest.Id, claims),
            UserId = guest.Id,
            Role = "Guest",
            Roles = new[] { "Guest" },
            Name = guest.FullName,
            FullName = guest.FullName
        };
    }

    public async Task<AuthResponse?> AuthenticateStaffAsync(StaffLoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var staff = await _dataStore.GetStaffByUsernameAsync(request.Username.Trim(), ct);
        if (staff is null)
        {
            return null;
        }

        if (!_passwordHasher.VerifyPassword(request.Password, staff.PasswordHash, staff.PasswordSalt))
        {
            return null;
        }

        var roleName = staff.Role.ToString();
        var claims = new Dictionary<string, string>
        {
            ["sub"] = staff.Id,
            ["role"] = "Staff",
            ["role:staff"] = roleName,
            ["name"] = staff.FullName,
            ["username"] = staff.Username
        };

        return new AuthResponse
        {
            Token = _jwtTokenService.CreateToken(staff.Id, claims),
            UserId = staff.Id,
            Role = roleName,
            Roles = new[] { "Staff", roleName },
            Name = staff.FullName,
            FullName = staff.FullName
        };
    }
}
