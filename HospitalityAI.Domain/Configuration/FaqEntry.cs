namespace HospitalityAI.Domain.Configuration;

public class FaqEntry
{
    public List<string> Keywords { get; set; } = new();
    public string Answer { get; set; } = string.Empty;
}
