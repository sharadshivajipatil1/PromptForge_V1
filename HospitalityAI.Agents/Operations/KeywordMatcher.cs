namespace HospitalityAI.Agents.Operations;

public sealed class KeywordMatcher
{
    private static readonly string[] HazardKeywords =
    {
        "fire", "smoke", "gas leak", "gas smell", "carbon monoxide", "co2 detector", "electrical", "spark", "shock", "exposed wire", "evacuat", "emergency", "hazard", "explosion", "injury", "bleeding", "medical", "choking", "seizure"
    };

    private static readonly string[] UrgentKeywords =
    {
        "blocked exit", "no power", "no electricity", "broken glass", "flood", "leak", "safety"
    };

    public KeywordMatchResult Match(string description)
    {
        var matchedHazard = HazardKeywords.FirstOrDefault(keyword => description.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        var matchedUrgent = UrgentKeywords.FirstOrDefault(keyword => description.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        return new KeywordMatchResult(matchedHazard, matchedUrgent);
    }
}

public sealed class KeywordMatchResult
{
    public KeywordMatchResult(string? matchedHazard, string? matchedUrgent)
    {
        MatchedHazard = matchedHazard;
        MatchedUrgent = matchedUrgent;
    }

    public string? MatchedHazard { get; }
    public string? MatchedUrgent { get; }
}
