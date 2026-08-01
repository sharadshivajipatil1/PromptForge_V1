namespace HospitalityAI.Domain.Dtos;

public class AuthTokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTimeOffset ExpiresAt { get; set; }
}
