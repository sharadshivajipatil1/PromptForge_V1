namespace HospitalityAI.Domain.Interfaces;

public interface ITaskNotificationService
{
    Task NotifyTaskCreatedAsync(CancellationToken ct = default);
    Task NotifyTaskUpdatedAsync(CancellationToken ct = default);
}