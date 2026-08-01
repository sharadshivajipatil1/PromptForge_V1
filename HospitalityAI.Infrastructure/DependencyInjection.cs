namespace HospitalityAI.Infrastructure;

using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Interfaces.Configuration;
using HospitalityAI.Infrastructure.Authentication;
using HospitalityAI.Infrastructure.Configuration;
using HospitalityAI.Infrastructure.Llm;
using HospitalityAI.Infrastructure.Services;
using HospitalityAI.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddHospitalityInfrastructure(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddSingleton<IReferenceDataLoader, JsonReferenceDataLoader>();
        services.AddSingleton<IDataStore, InMemoryDataStore>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<RuntimeModeService>(sp =>
        {
            var runtimeMode = new RuntimeModeService();
            // Seed from config so the app starts in Bedrock mode when Bedrock:UseBedrock = true.
            if (configuration != null && bool.TryParse(configuration["Bedrock:UseBedrock"], out var useBedrock))
            {
                runtimeMode.UseBedrock = useBedrock;
            }
            var modelId = configuration?["Bedrock:ModelId"];
            if (!string.IsNullOrWhiteSpace(modelId))
            {
                runtimeMode.BedrockModelId = modelId;
            }
            return runtimeMode;
        });
        services.AddSingleton<MockLlmClient>();
        // BedrockLlmClient needs IConfiguration — let the DI container resolve it.
        services.AddSingleton<BedrockLlmClient>();
        services.AddSingleton<ILlmClient>(sp => new SwitchingLlmClient(
            sp.GetRequiredService<RuntimeModeService>(),
            sp.GetRequiredService<MockLlmClient>(),
            sp.GetRequiredService<BedrockLlmClient>()));
        services.AddSingleton<IGuestService, GuestService>();
        services.AddSingleton<IReservationService, ReservationService>();
        services.AddSingleton<IHousekeepingService, HousekeepingService>();
        services.AddSingleton<IMaintenanceService, MaintenanceService>();
        services.AddSingleton<IForecastService, ForecastService>();
        services.AddSingleton<IOtpService, OtpService>();
        return services;
    }
}
