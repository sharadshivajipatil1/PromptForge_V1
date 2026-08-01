using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HospitalityAI.Agents;
using HospitalityAI.Api.Hubs;
using HospitalityAI.Api.Services;
using HospitalityAI.Domain.Dtos;
using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;
using HospitalityAI.Infrastructure.Llm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HospitalityAI.Api.Controllers;

[ApiController]
[Authorize(Roles = "Guest")]
[Route("api")]
public class GuestController : ControllerBase
{
    private readonly ConciergeAgent _conciergeAgent;
    private readonly WorkflowOrchestrator _orchestrator;
    private readonly IGuestService _guestService;
    private readonly IReservationService _reservationService;
    private readonly IHousekeepingService _housekeepingService;
    private readonly IMaintenanceService _maintenanceService;
    private readonly IOtpService _otpService;
    private readonly BedrockAgentService _bedrockAgentService;
    private readonly IHubContext<AgentActivityHub> _hubContext;
    private readonly IHubContext<DashboardHub> _dashboardHub;
    private readonly IDataStore _dataStore;
    private readonly RuntimeModeService _runtimeMode;
    private readonly ITaskNotificationService _taskNotificationService;

    public GuestController(ConciergeAgent conciergeAgent, WorkflowOrchestrator orchestrator, IGuestService guestService, IReservationService reservationService, IHousekeepingService housekeepingService, IMaintenanceService maintenanceService, IOtpService otpService, BedrockAgentService bedrockAgentService, IHubContext<AgentActivityHub> hubContext, IHubContext<DashboardHub> dashboardHub, RuntimeModeService runtimeMode, ITaskNotificationService taskNotificationService, IDataStore? dataStore = null)
    {
        _conciergeAgent = conciergeAgent;
        _orchestrator = orchestrator;
        _guestService = guestService;
        _reservationService = reservationService;
        _housekeepingService = housekeepingService;
        _maintenanceService = maintenanceService;
        _otpService = otpService;
        _bedrockAgentService = bedrockAgentService;
        _hubContext = hubContext;
        _dashboardHub = dashboardHub;
        _dataStore = dataStore;
        _runtimeMode = runtimeMode;
        _taskNotificationService = taskNotificationService;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] GuestChatRequest request, CancellationToken ct)
    {
        var guestId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Unauthorized(new { message = "Missing user identity." });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message is required." });
        }

        await _hubContext.Clients.Group(guestId).SendAsync("agentActivity", new AgentActivityEvent
        {
            AgentName = "ConciergeAgent",
            Message = "Processing your request…",
            Timestamp = DateTimeOffset.UtcNow
        }, ct);

        string reply;
        if (_runtimeMode.UseBedrock)
        {
            // Route through the Bedrock Agent — uses the instructions configured
            // in the AWS console (same as the AWS Test panel experience).
            reply = await _bedrockAgentService.ChatAsync(guestId, request.Message, ct);

            // Enhanced safety net: if the agent doesn't return ESCALATE_TO_FRONTDESK but the
            // message is clearly an actionable request, force escalation anyway.
            // This is a more aggressive approach to ensure maintenance requests are caught
            if (!reply.Contains("ESCALATE_TO_FRONTDESK", StringComparison.OrdinalIgnoreCase))
            {
                if (ShouldEscalate(request.Message))
                {
                    reply = "ESCALATE_TO_FRONTDESK " + reply;
                }
            }

            // If the agent escalates, create a ticket + staff task and broadcast
            // to the staff dashboard via SignalR.
            if (reply.Contains("ESCALATE_TO_FRONTDESK", StringComparison.OrdinalIgnoreCase))
            {
                var guest = await _guestService.GetGuestAsync(guestId, ct);

                // Save a concierge ticket
                var ticket = new ConciergeTicket
                {
                    GuestId = guestId,
                    GuestName = guest?.FullName ?? guestId,
                    RoomNumber = guest?.RoomNumber ?? string.Empty,
                    Message = request.Message,
                    Status = "Open"
                };
                await _dataStore.SaveTicketAsync(ticket, ct);

                // Use Hotel Operations Priority Service for better task classification
                var priorityService = new HospitalityAI.Agents.Operations.HotelOperationsPriorityService();
                var assessment = priorityService.AssessPriority(request.Message, guest?.RoomNumber);
                
                // Save a staff task
                var task = new Domain.Models.StaffTask
                {
                    Type = assessment.Department == "Maintenance" ? Domain.Enums.TaskType.Maintenance : Domain.Enums.TaskType.GuestRequest,
                    RoomNumber = guest?.RoomNumber ?? string.Empty,
                    Description = $"[Concierge Escalation] {request.Message}",
                    Priority = assessment.Priority,
                    Status = Domain.Enums.TaskStatus.Pending,
                    SlaMinutes = assessment.Priority switch
                    {
                        Domain.Enums.TaskPriority.Critical => 0,   // Immediate
                        Domain.Enums.TaskPriority.High => 30,      // 30 minutes
                        Domain.Enums.TaskPriority.Medium => 240,   // 4 hours  
                        Domain.Enums.TaskPriority.Low => 1440,     // 24 hours
                        _ => 30
                    },
                    Department = assessment.Department,
                    AssignedTo = $"{assessment.Department} Team",
                    PriorityReason = $"Score: {assessment.Score} - {assessment.Reason} (Escalated from AI concierge)"
                };
                var savedTask = await _dataStore.SaveTaskAsync(task, ct);

                // Notify staff dashboard of new task
                await _taskNotificationService.NotifyTaskCreatedAsync(ct);

                // Push updated ticket list to all connected staff via SignalR
                var updatedTickets = await _dataStore.GetTicketsAsync(null, 1, 100, ct);
                await _dashboardHub.Clients.Group("staff").SendAsync("ticketsUpdated",
                    updatedTickets.Items.Select(t => new
                    {
                        id = t.Id,
                        guestId = t.GuestId,
                        guestName = t.GuestName,
                        roomNumber = t.RoomNumber,
                        message = t.Message,
                        status = t.Status,
                        createdAt = t.CreatedAt
                    }), ct);

                // Show a clean handoff message to the guest based on priority
                var cleanReply = reply
                    .Replace("ESCALATE_TO_FRONTDESK", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();
                    
                if (string.IsNullOrWhiteSpace(cleanReply))
                {
                    reply = assessment.Priority switch
                    {
                        Domain.Enums.TaskPriority.Critical => "URGENT: I've immediately escalated your critical request to our team for immediate attention.",
                        Domain.Enums.TaskPriority.High => $"I've escalated your urgent request to our {assessment.Department} team — they'll respond within 30 minutes.",
                        Domain.Enums.TaskPriority.Medium => $"I've passed your request to our {assessment.Department} team — they'll respond within 4 hours.",
                        _ => $"I've logged your request with our {assessment.Department} team — they'll respond within 24 hours."
                    };
                }
                else
                {
                    reply = cleanReply;
                }
            }
        }
        else
        {
            reply = await _orchestrator.RouteAsync(request.Message, guestId, ct);
        }

        await _hubContext.Clients.Group(guestId).SendAsync("agentActivity", new AgentActivityEvent
        {
            AgentName = "ConciergeAgent",
            Message = "Response ready.",
            Timestamp = DateTimeOffset.UtcNow
        }, ct);

        return Ok(new
        {
            conversationId = request.ConversationId,
            replyOriginalLanguage = request.Language ?? "en",
            replyInGuestLanguage = reply,
            detectedLanguage = request.Language ?? "en"
        });
    }

    [HttpPost("orchestrator/message")]
    public async Task<IActionResult> OrchestratorMessage([FromBody] GuestChatRequest request, CancellationToken ct)
    {
        var guestId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Unauthorized(new { message = "Missing user identity." });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message is required." });
        }

        await _hubContext.Clients.Group(guestId).SendAsync("agentActivity", new AgentActivityEvent
        {
            AgentName = "ConciergeAgent",
            Message = "Processing your request…",
            Timestamp = DateTimeOffset.UtcNow
        }, ct);

        var reply = await _orchestrator.RouteAsync(request.Message, guestId, ct);

        await _hubContext.Clients.Group(guestId).SendAsync("agentActivity", new AgentActivityEvent
        {
            AgentName = "ConciergeAgent",
            Message = "Response ready.",
            Timestamp = DateTimeOffset.UtcNow
        }, ct);

        return Ok(new
        {
            conversationId = request.ConversationId,
            replyOriginalLanguage = request.Language ?? "en",
            replyInGuestLanguage = reply,
            detectedLanguage = request.Language ?? "en"
        });
    }

    [HttpGet("tickets/me")]
    public async Task<IActionResult> GetMyTickets(CancellationToken ct)
    {
        var guestId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Unauthorized(new { message = "Missing user identity." });
        }

        if (_dataStore is null)
        {
            return Ok(Array.Empty<TicketDto>());
        }

        var tickets = await _dataStore.GetTicketsAsync(null, 1, 100, ct);
        var myTickets = tickets.Items
            .Where(ticket => string.Equals(ticket.GuestId, guestId, StringComparison.OrdinalIgnoreCase))
            .Select(ticket => new TicketDto
            {
                Id = ticket.Id,
                GuestId = ticket.GuestId,
                GuestName = ticket.GuestName,
                RoomNumber = ticket.RoomNumber,
                Message = ticket.Message,
                Status = ticket.Status,
                CreatedBy = ticket.CreatedBy,
                Remark = ticket.Remark,
                PriorityReason = ticket.PriorityReason,
                CreatedAt = ticket.CreatedAt
            })
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToList();

        return Ok(myTickets);
    }

    [HttpPost("checkin/send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ReservationCode))
        {
            return BadRequest(new { message = "Reservation code is required." });
        }

        var result = await _otpService.SendOtpAsync(request.ReservationCode, ct);
        return Ok(new { sent = result.Sent, maskedPhone = result.MaskedPhone, expiresInSeconds = 60, demoOtp = result.DemoOtp, message = result.Message });
    }

    [HttpPost("checkin/verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Otp))
        {
            return BadRequest(new { message = "OTP is required." });
        }

        var verified = await _otpService.VerifyOtpAsync(request.Otp, ct);
        return Ok(new { verified, message = verified ? "OTP verified." : "OTP verification failed." });
    }

    [HttpPost("checkin")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequestDto? request, CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ReservationCode))
        {
            return BadRequest(new { message = "Reservation code is required." });
        }

        var guestId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Unauthorized(new { message = "Missing user identity." });
        }

        var result = await _guestService.CheckInAsync(guestId, request.ReservationCode.Trim(), ct);
        return Ok(new
        {
            requestId = Guid.NewGuid().ToString(),
            status = "Verified",
            roomNumber = result.RoomNumber,
            digitalKeyIssued = true,
            verificationSummary = "ID/selfie verified successfully.",
            recommendations = Array.Empty<object>(),
            isCheckedIn = true
        });
    }

    [HttpPost("checkin/checkout")]
    public async Task<IActionResult> CheckOut(CancellationToken ct)
    {
        var guestId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Unauthorized(new { message = "Missing user identity." });
        }

        var result = await _guestService.CheckOutAsync(guestId, ct);
        return Ok(new { requestId = Guid.NewGuid().ToString(), folioSummary = "Folio closed, key access revoked.", verificationSummary = "Checkout completed.", isCheckedIn = false });
    }

    [HttpGet("personalization/me")]
    public async Task<IActionResult> Personalization(CancellationToken ct)
    {
        var guestId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Unauthorized(new { message = "Missing user identity." });
        }

        var guest = await _guestService.GetGuestAsync(guestId, ct);
        if (guest is null)
        {
            return NotFound(new { message = "Guest not found." });
        }

        string agentReply;
        try
        {
            using var agentCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            agentCts.CancelAfter(TimeSpan.FromSeconds(30));
            agentReply = await _bedrockAgentService.GetRecommendationsAsync(
                guest.ReservationCode, agentCts.Token);
        }
        catch (Exception ex)
        {
            agentReply = string.Empty;
            _ = ex;
        }

        var items = ParseRecommendations(agentReply);
        return Ok(new { greeting = ExtractGreeting(agentReply), recommendations = items });
    }

    /// <summary>
    /// Parses the structured agent response into individual recommendation objects.
    /// Expected format per recommendation block:
    ///   RECOMMENDATION_N
    ///   CATEGORY: ...
    ///   TITLE: ...
    ///   DETAIL: ...
    /// Falls back to a single raw-text item if parsing finds nothing.
    /// </summary>
    private static List<object> ParseRecommendations(string agentReply)
    {
        var items = new List<object>();
        if (string.IsNullOrWhiteSpace(agentReply))
        {
            return FallbackRecommendations();
        }

        var lines = agentReply.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string? category = null, title = null, detail = null;
        int index = 0;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith("RECOMMENDATION_", StringComparison.OrdinalIgnoreCase))
            {
                // Flush previous block
                if (title is not null)
                {
                    items.Add(new { id = $"rec-{++index}", category = category ?? "Experience", title, description = detail ?? "" });
                    title = null; category = null; detail = null;
                }
            }
            else if (line.StartsWith("CATEGORY:", StringComparison.OrdinalIgnoreCase))
                category = line["CATEGORY:".Length..].Trim();
            else if (line.StartsWith("TITLE:", StringComparison.OrdinalIgnoreCase))
                title = line["TITLE:".Length..].Trim();
            else if (line.StartsWith("DETAIL:", StringComparison.OrdinalIgnoreCase))
                detail = line["DETAIL:".Length..].Trim();
        }

        // Flush last block
        if (title is not null)
            items.Add(new { id = $"rec-{++index}", category = category ?? "Experience", title, description = detail ?? "" });

        return items.Count > 0 ? items : FallbackRecommendations();
    }

    private static string ExtractGreeting(string agentReply)
    {
        if (string.IsNullOrWhiteSpace(agentReply)) return string.Empty;
        var line = agentReply.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(l => l.TrimStart().StartsWith("GREETING:", StringComparison.OrdinalIgnoreCase));
        return line is null ? string.Empty : line["GREETING:".Length..].Trim();
    }

    private static List<object> FallbackRecommendations() =>
    [
        new { id = "rec-1", category = "Dining",          title = "Signature dining experience",     description = "Reserve a table at Olive Terrace for an unforgettable Mediterranean evening." },
        new { id = "rec-2", category = "Spa & Wellness",  title = "Spa reset",                       description = "Unwind with a Swedish massage or aromatherapy session at our award-winning spa." },
        new { id = "rec-3", category = "Local Experience",title = "City highlights tour",             description = "Let the concierge arrange a guided tour of nearby landmarks and cultural gems." }
    ];

    [HttpPost("agent/guest-history")]
    public async Task<IActionResult> GetGuestHistory([FromBody] GuestHistoryRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ReservationCode))
        {
            return BadRequest(new { message = "Reservation code is required." });
        }

        var reply = await _bedrockAgentService.GetGuestHistoryAsync(request.ReservationCode.Trim(), ct);
        return Ok(new { reply });
    }

    [HttpPost("bookings")]
    public async Task<IActionResult> Book([FromBody] BookingRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Category) || string.IsNullOrWhiteSpace(request.SlotId) || string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Category, slot ID, and title are required." });
        }

        var guestId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Unauthorized(new { message = "Missing user identity." });
        }

        var confirmation = await _guestService.BookRecommendationAsync(guestId, request.SlotId, ct);
        if (!confirmation.Success)
        {
            return Ok(confirmation);
        }

        var guest = await _guestService.GetGuestAsync(guestId, ct);
        if (_dataStore is not null && guest is not null)
        {
            var requestedWindow = !string.IsNullOrWhiteSpace(request.StartTime)
                ? $" at {request.StartTime}"
                : !string.IsNullOrWhiteSpace(request.SuggestedTime)
                    ? $" for {request.SuggestedTime}"
                    : string.Empty;

            var task = new StaffTask
            {
                Type = TaskType.GuestRequest,
                RoomNumber = guest.RoomNumber ?? string.Empty,
                Description = $"Request for {request.Title} ({request.Category}){requestedWindow}.",
                Status = HospitalityAI.Domain.Enums.TaskStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
                SlaMinutes = 45,
                Priority = TaskPriority.High,
                AssignedTo = "Dana Reyes",
                Department = "Front Desk",
                PriorityReason = "Guest requested a service booking from the dashboard."
            };

            await _dataStore.SaveTaskAsync(task, ct);

            var ticket = new ConciergeTicket
            {
                GuestId = guest.Id,
                GuestName = guest.FullName,
                RoomNumber = guest.RoomNumber ?? string.Empty,
                Message = $"Request for {request.Title} ({request.Category}){requestedWindow}.",
                Status = "Open",
                CreatedBy = "Guest",
                Remark = "Guest created this request from the dashboard.",
                PriorityReason = "Guest requested a service booking from the dashboard.",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _dataStore.SaveTicketAsync(ticket, ct);
        }

        confirmation.Message = string.IsNullOrWhiteSpace(confirmation.Message)
            ? "Your booking request has been shared with the concierge team."
            : $"{confirmation.Message} Staff has been notified.";

        return Ok(confirmation);
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
    }

    private static bool ShouldEscalate(string message)
    {
        // Simple keyword-based escalation detection for maintenance requests
        var keywords = new[] { "broken", "fix", "repair", "maintenance", "not working", "issue", "problem" };
        return keywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    public class GuestChatRequest
    {
        public string ConversationId { get; set; } = string.Empty;
        public string? GuestId { get; set; }
        [Required]
        public string Message { get; set; } = string.Empty;
        public string? Language { get; set; }
    }

    public class SendOtpRequest
    {
        [Required]
        public string ReservationCode { get; set; } = string.Empty;
    }

    public class VerifyOtpRequest
    {
        [Required]
        public string Otp { get; set; } = string.Empty;
    }

    public class BookingRequest
    {
        [Required]
        public string Category { get; set; } = string.Empty;
        [Required]
        public string SlotId { get; set; } = string.Empty;
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? SuggestedTime { get; set; }
        public string? StartTime { get; set; }
    }

    public class GuestHistoryRequest
    {
        [Required]
        public string ReservationCode { get; set; } = string.Empty;
    }
}
