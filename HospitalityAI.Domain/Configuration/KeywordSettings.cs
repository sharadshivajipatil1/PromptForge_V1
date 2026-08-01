namespace HospitalityAI.Domain.Configuration;

public class KeywordSettings
{
    public List<string> Critical { get; set; } = new();
    public List<string> Urgent { get; set; } = new();
}
