namespace HospitalityAI.Infrastructure.Services;

using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;

public class HousekeepingService : IHousekeepingService
{
    private readonly IDataStore _dataStore;

    public HousekeepingService(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<string> CreateHousekeepingTaskAsync(string roomNumber, string description, CancellationToken ct = default)
    {
        var task = new StaffTask
        {
            RoomNumber = roomNumber,
            Description = description,
            Type = TaskType.Housekeeping,
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Pending,
            SlaMinutes = 20
        };

        await _dataStore.SaveTaskAsync(task, ct);
        return task.Id;
    }
}
