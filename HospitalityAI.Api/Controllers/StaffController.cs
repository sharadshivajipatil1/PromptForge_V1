using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HospitalityAI.Agents;
using HospitalityAI.Domain.Dtos;
using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalityAI.Api.Controllers;

[ApiController]
[Authorize(Roles = "Staff")]
[Route("api/dashboard")]
public class StaffController : ControllerBase
{
    private readonly IDataStore _dataStore;
    private readonly OperationsAgent _operationsAgent;
    private readonly IForecastService _forecastService;
    private readonly ITaskNotificationService _taskNotificationService;

    public StaffController(IDataStore dataStore, OperationsAgent operationsAgent, IForecastService forecastService, ITaskNotificationService taskNotificationService)
    {
        _dataStore = dataStore;
        _operationsAgent = operationsAgent;
        _forecastService = forecastService;
        _taskNotificationService = taskNotificationService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var username = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized(new { message = "Staff session is not available." });
        }

        var staff = await _dataStore.GetStaffByUsernameAsync(username, ct);
        if (staff is null)
        {
            return NotFound(new { message = "Staff profile not found." });
        }

        return Ok(MapStaffUser(staff));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateStaffProfileRequest request, CancellationToken ct)
    {
        var username = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized(new { message = "Staff session is not available." });
        }

        var staff = await _dataStore.GetStaffByUsernameAsync(username, ct);
        if (staff is null)
        {
            return NotFound(new { message = "Staff profile not found." });
        }

        staff.FullName = string.IsNullOrWhiteSpace(request.FullName) ? staff.FullName : request.FullName.Trim();
        staff.Department = string.IsNullOrWhiteSpace(request.Department) ? staff.Department : request.Department.Trim();

        if (Enum.TryParse<StaffRole>(request.Role, true, out var role))
        {
            staff.Role = role;
        }

        var saved = await _dataStore.SaveStaffUserAsync(staff, ct);
        return Ok(MapStaffUser(saved));
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks(CancellationToken ct)
    {
        var tasks = await _dataStore.GetTasksAsync(null, null, 1, 100, ct);
        return Ok(tasks.Select(MapTask).ToList());
    }

    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { message = "Description is required." });
        }

        var task = new StaffTask
        {
            Type = Enum.TryParse<TaskType>(request.Type, true, out var parsedType) ? parsedType : TaskType.Housekeeping,
            RoomNumber = request.RoomNumber ?? string.Empty,
            Description = request.Description,
            Status = HospitalityAI.Domain.Enums.TaskStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            SlaMinutes = request.SlaMinutes <= 0 ? 20 : request.SlaMinutes,
            Priority = TaskPriority.Medium,
            AssignedTo = string.IsNullOrWhiteSpace(request.AssignedTo) ? "Dana Reyes" : request.AssignedTo.Trim(),
            Department = string.IsNullOrWhiteSpace(request.Department) ? "Front Desk" : request.Department.Trim(),
            PriorityReason = "Created from the staff dashboard."
        };

        var saved = await _dataStore.SaveTaskAsync(task, ct);
        
        // Notify staff dashboard of new task
        await _taskNotificationService.NotifyTaskCreatedAsync(ct);
        
        return CreatedAtAction(nameof(GetTasks), new { taskId = saved.Id }, MapTask(saved));
    }

    [HttpPost("tasks/{taskId}/complete")]
    public async Task<IActionResult> CompleteTask(string taskId, CancellationToken ct)
    {
        var task = await _dataStore.GetTaskByIdAsync(taskId, ct);
        if (task is null)
        {
            return NotFound(new { message = "Task not found." });
        }

        task.Status = HospitalityAI.Domain.Enums.TaskStatus.Completed;
        var saved = await _dataStore.SaveTaskAsync(task, ct);
        
        // Notify staff dashboard of task update
        await _taskNotificationService.NotifyTaskUpdatedAsync(ct);
        
        return Ok(MapTask(saved));
    }

    [HttpPatch("tasks/{taskId}/status")]
    public async Task<IActionResult> UpdateTaskStatus(string taskId, [FromBody] UpdateTaskStatusRequest request, CancellationToken ct)
    {
        var task = await _dataStore.GetTaskByIdAsync(taskId, ct);
        if (task is null)
        {
            return NotFound(new { message = "Task not found." });
        }

        if (!TryParseStatus(request.Status, out var status))
        {
            return BadRequest(new { message = "Status must be Open, Pending, Completed or 0, 1, 2." });
        }

        task.Status = status;
        var saved = await _dataStore.SaveTaskAsync(task, ct);
        
        // Notify staff dashboard of task update
        await _taskNotificationService.NotifyTaskUpdatedAsync(ct);
        
        return Ok(MapTask(saved));
    }

    [HttpGet("forecast")]
    public async Task<IActionResult> Forecast(CancellationToken ct)
    {
        var forecast = await _operationsAgent.GenerateForecastAsync("staff dashboard", ct);
        return Ok(new
        {
            forDate = forecast.ForDate,
            predictedOccupancyPercent = forecast.PredictedOccupancyPercent,
            predictedRoomServiceOrders = forecast.PredictedRoomServiceOrders,
            recommendedHousekeepingStaff = forecast.RecommendedHousekeepingStaff,
            recommendedFrontDeskStaff = forecast.RecommendedFrontDeskStaff,
            inventoryRecommendations = forecast.RecommendedInventory,
            recentHistory = Array.Empty<object>(),
            reasoningSteps = Array.Empty<object>(),
            notes = forecast.Notes
        });
    }

    [HttpGet("staffing-forecast")]
    public async Task<IActionResult> StaffingForecast(CancellationToken ct)
    {
        var forecast = await _operationsAgent.GenerateForecastAsync("staff dashboard", ct);
        var totalRecommendedStaff = forecast.RecommendedHousekeepingStaff + forecast.RecommendedFrontDeskStaff;
        
        return Ok(new
        {
            recommendedStaff = totalRecommendedStaff,
            reasoning = $"Based on {forecast.PredictedOccupancyPercent}% occupancy prediction. {forecast.Notes}"
        });
    }

    [HttpGet("operations-forecast")]
    public async Task<IActionResult> OperationsForecast(CancellationToken ct)
    {
        var forecast = await _operationsAgent.GenerateForecastAsync("staff dashboard", ct);
        var expectedTasks = Math.Max(10, (int)(forecast.PredictedRoomServiceOrders * 0.8) + (int)(forecast.PredictedOccupancyPercent * 0.2));
        
        return Ok(new
        {
            expectedTasks = expectedTasks,
            period = "Next 4 hours prediction"
        });
    }

    [HttpPost("task-priority")]
    public async Task<IActionResult> GetTaskPriority([FromBody] TaskPriorityRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { message = "Description is required." });
        }

        try
        {
            var priority = await _operationsAgent.GetPriorityRecommendationAsync(request.Description, ct);
            return Ok(priority);
        }
        catch (Exception ex)
        {
            // Fallback logic for priority determination
            var description = request.Description.ToLower();
            var priority = "Medium";
            var reason = "Standard priority based on task description";

            var urgentKeywords = new[] { "urgent", "emergency", "broken", "not working", "leak", "safety", "fire", "flood" };
            var highKeywords = new[] { "repair", "fix", "maintenance", "clean", "guest complaint", "vip" };

            if (urgentKeywords.Any(keyword => description.Contains(keyword)))
            {
                priority = "High";
                reason = "Contains urgent keywords requiring immediate attention";
            }
            else if (highKeywords.Any(keyword => description.Contains(keyword)))
            {
                priority = "Medium";
                reason = "Maintenance or service-related task requiring timely completion";
            }
            else
            {
                priority = "Low";
                reason = "Routine task with standard priority";
            }

            return Ok(new { Priority = priority, Reason = reason });
        }
    }

    [HttpGet("guests")]
    public async Task<IActionResult> Guests(CancellationToken ct)
    {
        var guests = await _dataStore.GetGuestsAsync(1, 100, ct);
        return Ok(guests.Select(guest => new
        {
            id = guest.Id,
            fullName = guest.FullName,
            preferredLanguage = guest.PreferredLanguage,
            loyaltyTier = guest.LoyaltyTier,
            roomNumber = guest.RoomNumber,
            reservationCode = guest.ReservationCode
        }));
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> Tickets(CancellationToken ct)
    {
        var tickets = await _dataStore.GetTicketsAsync(null, 1, 100, ct);
        return Ok(tickets.Items.Select(ticket => new TicketDto
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
        }));
    }

    [HttpPost("tickets/{ticketId}/resolve")]
    public async Task<IActionResult> ResolveTicket(string ticketId, CancellationToken ct)
    {
        var ticket = await _dataStore.GetTicketByIdAsync(ticketId, ct);
        if (ticket is null)
        {
            return NotFound(new { message = "Ticket not found." });
        }

        ticket.Status = "Resolved";
        var saved = await _dataStore.SaveTicketAsync(ticket, ct);
        return Ok(new TicketDto
        {
            Id = saved.Id,
            GuestId = saved.GuestId,
            GuestName = saved.GuestName,
            RoomNumber = saved.RoomNumber,
            Message = saved.Message,
            Status = saved.Status,
            CreatedBy = saved.CreatedBy,
            Remark = saved.Remark,
            PriorityReason = saved.PriorityReason,
            CreatedAt = saved.CreatedAt
        });
    }

    private static TaskDto MapTask(StaffTask task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Type = task.Type,
            RoomNumber = task.RoomNumber,
            Description = task.Description,
            Priority = task.Priority,
            Status = task.Status,
            CreatedAt = task.CreatedAt,
            SlaMinutes = task.SlaMinutes,
            AssignedTo = task.AssignedTo,
            Department = task.Department,
            PriorityReason = task.PriorityReason
        };
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
    }

    private string? GetCurrentUsername()
    {
        return User.FindFirstValue("username") ?? User.FindFirstValue(ClaimTypes.Name) ?? GetCurrentUserId();
    }

    private static StaffUserDto MapStaffUser(StaffUser staffUser)
    {
        return new StaffUserDto
        {
            Id = staffUser.Id,
            Username = staffUser.Username,
            FullName = staffUser.FullName,
            Department = staffUser.Department,
            Role = staffUser.Role
        };
    }

    public class CreateTaskRequest
    {
        [Required]
        public string Type { get; set; } = string.Empty;
        public string? RoomNumber { get; set; }
        [Required]
        public string Description { get; set; } = string.Empty;
        public int SlaMinutes { get; set; }
        public string? AssignedTo { get; set; }
        public string? Department { get; set; }
    }

    public class UpdateStaffProfileRequest
    {
        public string? FullName { get; set; }
        public string? Department { get; set; }
        public string? Role { get; set; }
    }

    public class TaskPriorityRequest
    {
        [Required]
        public string Description { get; set; } = string.Empty;
    }

    public class UpdateTaskStatusRequest
    {
        public string? Status { get; set; }
    }

    private static bool TryParseStatus(string? value, out HospitalityAI.Domain.Enums.TaskStatus status)
    {
        status = HospitalityAI.Domain.Enums.TaskStatus.Pending;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        if (string.Equals(normalized, "Open", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "Pending", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "0", StringComparison.OrdinalIgnoreCase))
        {
            status = HospitalityAI.Domain.Enums.TaskStatus.Pending;
            return true;
        }

        if (string.Equals(normalized, "InProgress", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "1", StringComparison.OrdinalIgnoreCase))
        {
            status = HospitalityAI.Domain.Enums.TaskStatus.InProgress;
            return true;
        }

        if (string.Equals(normalized, "Completed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "2", StringComparison.OrdinalIgnoreCase))
        {
            status = HospitalityAI.Domain.Enums.TaskStatus.Completed;
            return true;
        }

        return false;
    }
}
