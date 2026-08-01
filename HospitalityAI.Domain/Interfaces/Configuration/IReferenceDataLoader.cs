namespace HospitalityAI.Domain.Interfaces.Configuration;

using HospitalityAI.Domain.Configuration;

public interface IReferenceDataLoader
{
    Task<GreetingSettings> LoadGreetingsAsync(CancellationToken ct = default);
    Task<FaqSettings> LoadFaqAsync(CancellationToken ct = default);
    Task<ActivitySettings> LoadActivitiesAsync(CancellationToken ct = default);
    Task<KeywordSettings> LoadKeywordsAsync(CancellationToken ct = default);
    Task<SeedDataSettings> LoadSeedDataAsync(CancellationToken ct = default);
    Task<HotelKnowledgeSettings> LoadHotelKnowledgeAsync(CancellationToken ct = default);
}
