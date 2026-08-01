namespace HospitalityAI.Domain.Interfaces;

public interface IMaintenanceService
{
    Task<string> CreateMaintenanceTaskAsync(string roomNumber, string description, CancellationToken ct = default);
}
