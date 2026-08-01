namespace HospitalityAI.Domain.Configuration;

public class ActivitySettings
{
    public List<ActivityOption> SeasonalActivities { get; set; } = new();
    public List<ActivityOption> ProfessionActivities { get; set; } = new();
}

public class ActivityOption
{
    public string? Season { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int HoursFromNow { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
    public string? ReasonTemplate { get; set; }
    public List<string> MatchKeywords { get; set; } = new();
}
