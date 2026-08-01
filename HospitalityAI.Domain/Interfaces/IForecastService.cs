namespace HospitalityAI.Domain.Interfaces;

using HospitalityAI.Domain.Models;

public interface IForecastService
{
    Task<ForecastRecord> GenerateForecastAsync(CancellationToken ct = default);
}
