using System.Security.Claims;
using HospitalityAI.Domain.Auth;
using HospitalityAI.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalityAI.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("guest-login")]
    public async Task<IActionResult> GuestLogin([FromBody] GuestLoginRequest request, CancellationToken ct)
    {
        var response = await _authService.AuthenticateGuestAsync(request, ct);
        if (response is null)
        {
            return Unauthorized(new { message = "Invalid reservation code." });
        }

        return Ok(response);
    }

    [HttpPost("staff-login")]
    public async Task<IActionResult> StaffLogin([FromBody] StaffLoginRequest request, CancellationToken ct)
    {
        var response = await _authService.AuthenticateStaffAsync(request, ct);
        if (response is null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var roles = User.Claims.Where(claim => claim.Type == ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        return Ok(new { userId, roles });
    }
}
