namespace HospitalityAI.Domain.Dtos;

public class OperationsSnapshotDto
{
    public DateTimeOffset GeneratedAt { get; set; }
    public int OpenTaskCount { get; set; }
    public int CriticalTaskCount { get; set; }
    public int HighPriorityTaskCount { get; set; }
    public List<OperationsTaskDto> PendingTasks { get; set; } = new();
    public List<TriageResultDto> TriageResults { get; set; } = new();
    public List<ForecastDto> Forecasts { get; set; } = new();
}

public class OperationsTaskDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? PriorityReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int SlaMinutes { get; set; }
}

public class TriageResultDto
{
    public string TaskId { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
