using System.Security.Claims;
using HospitalityAI.Domain.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HospitalityAI.Api.Hubs;

[Authorize(Roles = "Staff")]
public class DashboardHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "staff");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task BroadcastTaskUpdateAsync(IReadOnlyList<TaskDto> tasks)
    {
        await Clients.Group("staff").SendAsync("tasksUpdated", tasks);
    }

    private string? GetCurrentUserId()
    {
        return Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
    }
}
