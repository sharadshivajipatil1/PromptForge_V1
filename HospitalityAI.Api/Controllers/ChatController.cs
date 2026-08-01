using HospitalityAI.Agents;
using Microsoft.AspNetCore.Mvc;

namespace HospitalityAI.Api.Controllers;

[ApiController]
[Route("api/chat-legacy")]
public class ChatController : ControllerBase
{
    private readonly WorkflowOrchestrator _orchestrator;

    public ChatController(WorkflowOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] LegacyChatRequest request, CancellationToken ct)
    {
        // Legacy endpoint — use GuestId from request body if provided, otherwise default to "guest".
        var guestId = !string.IsNullOrWhiteSpace(request.GuestId) ? request.GuestId : "guest";
        var reply = await _orchestrator.RouteAsync(request.Message, guestId, ct);
        return Ok(new { conversationId = request.ConversationId, replyOriginalLanguage = request.Language ?? "en", replyInGuestLanguage = reply, detectedLanguage = request.Language ?? "en" });
    }

    public class LegacyChatRequest
    {
        public string ConversationId { get; set; } = string.Empty;
        public string? GuestId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Language { get; set; }
    }
}
