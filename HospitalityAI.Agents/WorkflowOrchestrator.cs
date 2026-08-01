namespace HospitalityAI.Agents;

using HospitalityAI.Agents.Operations;
using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Models;

public class WorkflowOrchestrator
{
    private readonly ConciergeAgent _conciergeAgent;
    private readonly OperationsAgent _operationsAgent;
    private readonly IDataStore _dataStore;
    private readonly ITaskNotificationService _taskNotificationService;
    private readonly WorkflowIntentClassifier _intentClassifier = new();

    public WorkflowOrchestrator(ConciergeAgent conciergeAgent, OperationsAgent operationsAgent, IDataStore dataStore, ITaskNotificationService taskNotificationService)
    {
        _conciergeAgent = conciergeAgent;
        _operationsAgent = operationsAgent;
        _dataStore = dataStore;
        _taskNotificationService = taskNotificationService;
    }

    public async Task<string> RouteAsync(string message, string guestId = "guest", CancellationToken ct = default)
    {
        var intent = _intentClassifier.Classify(message);

        if (intent == WorkflowIntent.Forecast)
        {
            var forecast = await _operationsAgent.GenerateForecastAsync(message, ct);
            return $"Forecast received: occupancy {forecast.PredictedOccupancyPercent:F0}% with {forecast.PredictedRoomServiceOrders} orders. {forecast.Notes}";
        }

        if (intent == WorkflowIntent.Operations)
        {
            // Use the Hotel Operations Priority Service to determine priority
            var priorityService = new HotelOperationsPriorityService();
            var assessment = priorityService.AssessPriority(message);
            
            var task = new StaffTask
            {
                Type = assessment.Department == "Maintenance" ? TaskType.Maintenance : TaskType.Housekeeping,
                Description = message,
                Priority = assessment.Priority,
                Status = TaskStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
                SlaMinutes = assessment.Priority switch
                {
                    TaskPriority.Critical => 0,  // Immediate
                    TaskPriority.High => 30,     // 30 minutes
                    TaskPriority.Medium => 240,  // 4 hours
                    TaskPriority.Low => 1440,    // 24 hours
                    _ => 240
                },
                Department = assessment.Department,
                AssignedTo = $"{assessment.Department} Team",
                PriorityReason = $"Score: {assessment.Score} - {assessment.Reason}"
            };

            var savedTask = await _dataStore.SaveTaskAsync(task, ct);
            
            // Notify staff dashboard of new task
            await _taskNotificationService.NotifyTaskCreatedAsync(ct);
            
            return assessment.Priority switch
            {
                TaskPriority.Critical => "URGENT: Your critical request has been escalated immediately to our team.",
                TaskPriority.High => $"Your urgent request has been logged with our {assessment.Department} team - they'll respond within 30 minutes.",
                TaskPriority.Medium => $"Your request has been logged with our {assessment.Department} team - they'll respond within 4 hours.",
                TaskPriority.Low => $"Your request has been logged with our {assessment.Department} team - they'll respond within 24 hours.",
                _ => "Your request has been logged with the operations team."
            };
        }

        return await _conciergeAgent.HandleChatAsync(guestId, message, null, ct);
    }
}
