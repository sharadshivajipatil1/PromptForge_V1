using HospitalityAI.Api.Hubs;
using HospitalityAI.Domain.Dtos;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;
using Microsoft.AspNetCore.SignalR;

namespace HospitalityAI.Api.Services;

public class TaskNotificationService : ITaskNotificationService
{
    private readonly IHubContext<DashboardHub> _dashboardHub;
    private readonly IDataStore _dataStore;

    public TaskNotificationService(IHubContext<DashboardHub> dashboardHub, IDataStore dataStore)
    {
        _dashboardHub = dashboardHub;
        _dataStore = dataStore;
    }

    public async Task NotifyTaskCreatedAsync(CancellationToken ct = default)
    {
        await NotifyTaskUpdatedAsync(ct);
    }

    public async Task NotifyTaskUpdatedAsync(CancellationToken ct = default)
    {
        var allTasks = await _dataStore.GetTasksAsync(null, null, 1, 100, ct);
        var taskDtos = allTasks.Select(MapTask).ToList();
        await _dashboardHub.Clients.Group("staff").SendAsync("tasksUpdated", taskDtos, ct);
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
}