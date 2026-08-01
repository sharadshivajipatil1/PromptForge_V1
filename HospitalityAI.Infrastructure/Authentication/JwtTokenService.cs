namespace HospitalityAI.Infrastructure.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HospitalityAI.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public class JwtTokenService : IJwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _signingKey;

    public JwtTokenService(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"] ?? "HospitalityAI";
        _audience = configuration["Jwt:Audience"] ?? "HospitalityAI";
        _signingKey = configuration["Jwt:Key"] ?? "dev-only-insecure-key-1234567890";
    }

    public string CreateToken(string subject, IReadOnlyDictionary<string, string> claims)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var jwtClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(ClaimTypes.NameIdentifier, subject)
        };

        foreach (var claim in claims)
        {
            if (string.IsNullOrWhiteSpace(claim.Key) || string.IsNullOrWhiteSpace(claim.Value))
            {
                continue;
            }

            if (claim.Key.Equals("sub", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (claim.Key.Equals("role", StringComparison.OrdinalIgnoreCase) || claim.Key.StartsWith("role:", StringComparison.OrdinalIgnoreCase))
            {
                jwtClaims.Add(new Claim("role", claim.Value));
                jwtClaims.Add(new Claim(ClaimTypes.Role, claim.Value));
            }
            else if (claim.Key.Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                jwtClaims.Add(new Claim(ClaimTypes.Name, claim.Value));
                jwtClaims.Add(new Claim(claim.Key, claim.Value));
            }
            else
            {
                jwtClaims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: jwtClaims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
