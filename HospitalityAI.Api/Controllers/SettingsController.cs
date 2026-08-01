using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Infrastructure.Llm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalityAI.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly RuntimeModeService _runtimeModeService;

    public SettingsController(RuntimeModeService runtimeModeService)
    {
        _runtimeModeService = runtimeModeService;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { useBedrock = _runtimeModeService.UseBedrock, bedrockModelId = _runtimeModeService.BedrockModelId });
    }

    [AllowAnonymous]
    [HttpPost]
    public IActionResult Post([FromBody] UpdateSettingsRequest request)
    {
        if (request.UseBedrock is not null)
        {
            _runtimeModeService.UseBedrock = request.UseBedrock.Value;
        }

        return Ok(new { useBedrock = _runtimeModeService.UseBedrock, bedrockModelId = _runtimeModeService.BedrockModelId });
    }

    public class UpdateSettingsRequest
    {
        public bool? UseBedrock { get; set; }
    }
}
