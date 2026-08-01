namespace HospitalityAI.Agents.Recommendation;

using HospitalityAI.Domain.Configuration;
using HospitalityAI.Domain.Dtos;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Interfaces.Configuration;
using HospitalityAI.Domain.Models;
using HospitalityAI.Domain.ValueObjects;

public class RecommendationBuilder
{
    private readonly IDataStore _dataStore;
    private readonly IReferenceDataLoader _referenceDataLoader;
    private readonly ILlmClient _llmClient;

    public RecommendationBuilder(IDataStore dataStore, IReferenceDataLoader referenceDataLoader, ILlmClient llmClient)
    {
        _dataStore = dataStore;
        _referenceDataLoader = referenceDataLoader;
        _llmClient = llmClient;
    }

    public async Task<PersonalizationResponse> BuildAsync(Guest guest, CancellationToken ct = default)
    {
        var recentMoments = guest.History
            .OrderByDescending(entry => entry.Date)
            .Take(4)
            .Select(entry => new HistoryMomentDto
            {
                Type = entry.Type,
                Description = entry.Description,
                Date = entry.Date,
                Rating = entry.Rating
            })
            .ToList();

        var spaHistoryCount = guest.History.Count(entry => string.Equals(entry.Type, "Spa", StringComparison.OrdinalIgnoreCase));
        var diningHistoryCount = guest.History.Count(entry => string.Equals(entry.Type, "Dining", StringComparison.OrdinalIgnoreCase));
        var season = SeasonResolver.Resolve(DateTimeOffset.UtcNow);
        var recommendations = new List<RecommendationDto>();
        var reasoningSteps = new List<string>();

        if (spaHistoryCount > 0)
        {
            var spaSlots = (await _dataStore.GetAvailableSpaSlotsAsync(ct)).OrderBy(slot => slot.StartTime).ToList();
            for (var index = 0; index < Math.Min(2, spaSlots.Count); index++)
            {
                var slot = spaSlots[index];
                var confidence = Math.Min(0.95, 0.6 + spaHistoryCount * 0.1) - index * 0.1;
                var recentSpa = guest.History.Where(entry => string.Equals(entry.Type, "Spa", StringComparison.OrdinalIgnoreCase)).OrderByDescending(entry => entry.Date).FirstOrDefault();
                recommendations.Add(new RecommendationDto
                {
                    Category = "Spa",
                    Title = slot.ServiceName,
                    Description = $"{slot.ServiceName} at {slot.StartTime:HH:mm}",
                    SuggestedTime = slot.StartTime,
                    Confidence = confidence,
                    Reason = recentSpa is null
                        ? "A spa slot fits your stay preferences."
                        : $"Your recent spa visit to {recentSpa.Description} suggests a relaxing return visit.",
                    BookingRefId = slot.Id
                });
            }

            reasoningSteps.Add("Added spa recommendations based on recent spa history.");
        }

        if (diningHistoryCount > 0)
        {
            var diningOptions = (await _dataStore.GetDiningOptionsAsync(ct))
                .OrderBy(option => GetDiningOrder(option))
                .ToList();
            for (var index = 0; index < Math.Min(2, diningOptions.Count); index++)
            {
                var option = diningOptions[index];
                var confidence = Math.Min(0.95, 0.6 + diningHistoryCount * 0.1) - index * 0.1;
                var recentDining = guest.History.Where(entry => string.Equals(entry.Type, "Dining", StringComparison.OrdinalIgnoreCase)).OrderByDescending(entry => entry.Date).FirstOrDefault();
                recommendations.Add(new RecommendationDto
                {
                    Category = "Dining",
                    Title = option.Name,
                    Description = option.Description,
                    SuggestedTime = DateTimeOffset.UtcNow.Date.AddHours(19),
                    Confidence = confidence,
                    Reason = recentDining is null
                        ? "A dining option fits your recent preferences."
                        : $"Your recent dining experience with {recentDining.Description} suggests another enjoyable meal.",
                    BookingRefId = option.Id
                });
            }

            reasoningSteps.Add("Added dining recommendations based on recent dining history.");
        }

        var activities = await _referenceDataLoader.LoadActivitiesAsync(ct);
        var seasonalActivity = activities.SeasonalActivities.FirstOrDefault(entry => entry.Season?.Equals(season, StringComparison.OrdinalIgnoreCase) == true)
            ?? activities.SeasonalActivities.FirstOrDefault();
        if (seasonalActivity is not null)
        {
            recommendations.Add(new RecommendationDto
            {
                Category = "Activity",
                Title = seasonalActivity.Title ?? "Seasonal activity",
                Description = seasonalActivity.Description ?? string.Empty,
                SuggestedTime = DateTimeOffset.UtcNow.AddHours(seasonalActivity.HoursFromNow),
                Confidence = seasonalActivity.Confidence,
                Reason = seasonalActivity.Reason ?? string.Empty
            });
            reasoningSteps.Add("Added a seasonal activity recommendation.");
        }

        var professionActivity = ProfessionMatcher.Match(guest.Profession, activities.ProfessionActivities);
        if (professionActivity is not null)
        {
            var reason = professionActivity.ReasonTemplate?.Replace("{profession}", guest.Profession ?? string.Empty).Replace("{tripPurpose}", (guest.TripPurpose ?? string.Empty).ToLowerInvariant()) ?? string.Empty;
            recommendations.Add(new RecommendationDto
            {
                Category = "Activity",
                Title = professionActivity.Title ?? "Profession activity",
                Description = professionActivity.Description ?? string.Empty,
                SuggestedTime = DateTimeOffset.UtcNow.AddHours(professionActivity.HoursFromNow),
                Confidence = professionActivity.Confidence,
                Reason = reason
            });
            reasoningSteps.Add("Added a profession-based recommendation.");
        }

        if (spaHistoryCount == 0 && diningHistoryCount == 0)
        {
            recommendations.Add(new RecommendationDto
            {
                Category = "Activity",
                Title = "Explore hotel amenities",
                Description = "Discover the hotel amenities and make the most of your stay.",
                SuggestedTime = DateTimeOffset.UtcNow.AddHours(2),
                Confidence = 0.4,
                Reason = "No recent spa or dining history was found, so a general amenity suggestion is a good fallback."
            });
            reasoningSteps.Add("Added a generic fallback recommendation because no spa or dining history exists.");
        }

        var llmPrompt = "You are the Concierge agent narrating this guest's journey so far. Write 2-3 warm, story-like sentences that connect the guest's profile and the chosen recommendations.";
        var llmNarrative = await _llmClient.CompleteAsync(
            llmPrompt,
            $"Guest: {guest.FullName}\nLoyalty: {guest.LoyaltyTier}\nProfession: {guest.Profession}\nSeason: {season}\nRecommendations: {string.Join(" | ", recommendations.Select(item => $"{item.Title}: {item.Reason}"))}",
            ct);

        reasoningSteps.Add("Generated an LLM narrative for the recommendations.");

        return new PersonalizationResponse
        {
            GuestId = guest.Id,
            GuestName = guest.FullName,
            LoyaltyTier = guest.LoyaltyTier,
            RecentMoments = recentMoments,
            Recommendations = recommendations,
            AgentNarrative = llmNarrative,
            ReasoningSteps = reasoningSteps
        };
    }

    private static DateTimeOffset GetDiningOrder(DiningOption option)
    {
        if (DateTimeOffset.TryParse(option.Category, out var parsed))
        {
            return parsed;
        }

        return DateTimeOffset.UtcNow.Date.AddHours(19);
    }
}
