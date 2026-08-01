namespace HospitalityAI.Domain.Auth;

public class GuestLoginRequest
{
    public string ReservationCode { get; set; } = string.Empty;
}

public class StaffLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public IReadOnlyList<string>? Roles { get; set; }
    public string? Name { get; set; }
    public string? FullName { get; set; }
}
