namespace HospitalityAI.Agents.Recommendation;

using HospitalityAI.Domain.Configuration;

public static class ProfessionMatcher
{
    public static ActivityOption? Match(string? profession, IEnumerable<ActivityOption> professionActivities)
    {
        if (string.IsNullOrWhiteSpace(profession))
        {
            return null;
        }

        return professionActivities.FirstOrDefault(option =>
            option.MatchKeywords.Any(keyword => profession.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }
}
