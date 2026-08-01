using System.IdentityModel.Tokens.Jwt;
using HospitalityAI.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HospitalityAI.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_IncludesRoleClaimForAuthorization()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "HospitalityAI",
                ["Jwt:Audience"] = "HospitalityAI",
                ["Jwt:Key"] = "dev-only-insecure-key-1234567890"
            })
            .Build();

        var service = new JwtTokenService(configuration);

        var token = service.CreateToken("guest-1", new Dictionary<string, string>
        {
            ["role"] = "Guest",
            ["name"] = "Priya"
        });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(jwt.Claims, claim => claim.Type == "role" && claim.Value == "Guest");
    }

    [Fact]
    public void CreateToken_DoesNotDuplicateTheSubjectClaim()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "HospitalityAI",
                ["Jwt:Audience"] = "HospitalityAI",
                ["Jwt:Key"] = "dev-only-insecure-key-1234567890"
            })
            .Build();

        var service = new JwtTokenService(configuration);

        var token = service.CreateToken("guest-1", new Dictionary<string, string>
        {
            ["sub"] = "guest-1",
            ["role"] = "Guest"
        });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var subjectClaims = jwt.Claims.Where(claim => claim.Type == JwtRegisteredClaimNames.Sub || claim.Type == "sub").ToList();

        Assert.Single(subjectClaims);
        Assert.Equal("guest-1", subjectClaims[0].Value);
    }
}
