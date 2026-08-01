namespace HospitalityAI.Agents.Operations;

public static class TaskPrioritizer
{
    public static IReadOnlyList<HospitalityAI.Domain.Models.StaffTask> Prioritize(IEnumerable<HospitalityAI.Domain.Models.StaffTask> tasks, DateTimeOffset now)
    {
        return tasks.ToList();
    }
}
