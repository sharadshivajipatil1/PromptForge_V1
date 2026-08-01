namespace HospitalityAI.Agents.Recommendation;

public static class SeasonResolver
{
    public static string Resolve(DateTimeOffset now)
    {
        return now.Month switch
        {
            12 or 1 or 2 => "Winter",
            3 or 4 or 5 => "Spring",
            6 or 7 or 8 => "Summer",
            _ => "Autumn"
        };
    }
}
