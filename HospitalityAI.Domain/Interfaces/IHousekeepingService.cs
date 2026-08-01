namespace HospitalityAI.Domain.Interfaces;

public interface IHousekeepingService
{
    Task<string> CreateHousekeepingTaskAsync(string roomNumber, string description, CancellationToken ct = default);
}
