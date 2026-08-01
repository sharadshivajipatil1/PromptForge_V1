namespace HospitalityAI.Domain.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(string subject, IReadOnlyDictionary<string, string> claims);
}
