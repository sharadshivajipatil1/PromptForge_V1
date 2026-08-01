namespace HospitalityAI.Infrastructure.Configuration;

using System.Text.Json;
using HospitalityAI.Domain.Configuration;
using HospitalityAI.Domain.Interfaces.Configuration;

public class JsonReferenceDataLoader : IReferenceDataLoader
{
    private readonly string _basePath;

    public JsonReferenceDataLoader(string? basePath = null)
    {
        _basePath = basePath ?? AppContext.BaseDirectory;
    }

    public async Task<GreetingSettings> LoadGreetingsAsync(CancellationToken ct = default)
    {
        var path = ResolvePath("greetings.json");
        if (path is null)
        {
            return new GreetingSettings();
        }

        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<GreetingSettings>(json, JsonOptions) ?? new GreetingSettings();
    }

    public async Task<FaqSettings> LoadFaqAsync(CancellationToken ct = default)
    {
        var path = ResolvePath("faq.json");
        if (path is null)
        {
            return new FaqSettings();
        }

        var json = await File.ReadAllTextAsync(path, ct);
        var entries = JsonSerializer.Deserialize<List<FaqEntry>>(json, JsonOptions) ?? new List<FaqEntry>();
        return new FaqSettings { Entries = entries };
    }

    public async Task<ActivitySettings> LoadActivitiesAsync(CancellationToken ct = default)
    {
        var path = ResolvePath("activities.json");
        if (path is null)
        {
            return new ActivitySettings();
        }

        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<ActivitySettings>(json, JsonOptions) ?? new ActivitySettings();
    }

    public async Task<KeywordSettings> LoadKeywordsAsync(CancellationToken ct = default)
    {
        var path = ResolvePath("keywords.json");
        if (path is null)
        {
            return new KeywordSettings();
        }

        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<KeywordSettings>(json, JsonOptions) ?? new KeywordSettings();
    }

    public async Task<SeedDataSettings> LoadSeedDataAsync(CancellationToken ct = default)
    {
        var path = ResolvePath("seed-data.json");
        if (path is null)
        {
            return new SeedDataSettings();
        }

        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<SeedDataSettings>(json, JsonOptions) ?? new SeedDataSettings();
    }

    public async Task<HotelKnowledgeSettings> LoadHotelKnowledgeAsync(CancellationToken ct = default)
    {
        var path = ResolvePath("hotel-knowledge.json");
        if (path is null)
        {
            return new HotelKnowledgeSettings();
        }

        var json = await File.ReadAllTextAsync(path, ct);
        var entries = JsonSerializer.Deserialize<List<HotelKnowledgeEntry>>(json, JsonOptions)
                      ?? new List<HotelKnowledgeEntry>();
        return new HotelKnowledgeSettings { Entries = entries };
    }

    private string? ResolvePath(string fileName)
    {
        var directory = new DirectoryInfo(_basePath);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
