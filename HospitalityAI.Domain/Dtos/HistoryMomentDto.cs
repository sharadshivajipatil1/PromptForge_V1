namespace HospitalityAI.Domain.Dtos;

public class HistoryMomentDto
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public int? Rating { get; set; }
}
