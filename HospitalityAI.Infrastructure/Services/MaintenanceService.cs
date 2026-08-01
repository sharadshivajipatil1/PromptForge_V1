namespace HospitalityAI.Infrastructure.Services;

using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;

public class MaintenanceService : IMaintenanceService
{
    private readonly IDataStore _dataStore;

    public MaintenanceService(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<string> CreateMaintenanceTaskAsync(string roomNumber, string description, CancellationToken ct = default)
    {
        var task = new StaffTask
        {
            RoomNumber = roomNumber,
            Description = description,
            Type = TaskType.Maintenance,
            Priority = TaskPriority.High,
            Status = TaskStatus.Pending,
            SlaMinutes = 20
        };

        await _dataStore.SaveTaskAsync(task, ct);
        return task.Id;
    }
}
