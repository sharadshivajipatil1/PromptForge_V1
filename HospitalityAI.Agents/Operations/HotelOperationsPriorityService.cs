namespace HospitalityAI.Agents.Operations;

using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Models;

public class HotelOperationsPriorityService
{
    public PriorityAssessment AssessPriority(string taskDescription, string? roomNumber = null, bool isVipGuest = false)
    {
        var description = taskDescription.ToLowerInvariant();
        
        // Calculate individual impact scores (0-100 scale)
        var guestImpact = CalculateGuestImpact(description);
        var revenueImpact = CalculateRevenueImpact(description, isVipGuest);
        var safetyRisk = CalculateSafetyRisk(description);
        var operationalImpact = CalculateOperationalImpact(description);
        var slaBreachRisk = CalculateSlaBreachRisk(description);
        
        // Apply the priority scoring formula
        var priorityScore = (guestImpact * 0.35) + (revenueImpact * 0.20) + (safetyRisk * 0.25) + 
                           (operationalImpact * 0.10) + (slaBreachRisk * 0.10);
        
        // Add VIP guest bonus
        if (isVipGuest)
        {
            priorityScore += 15;
        }
        
        // Ensure score doesn't exceed 100
        priorityScore = Math.Min(100, priorityScore);
        
        // Determine priority level and response time
        var (priority, responseTime, department) = DeterminePriority(priorityScore, description);
        
        var reason = GenerateReason(priority, guestImpact, safetyRisk, operationalImpact, isVipGuest);
        
        return new PriorityAssessment
        {
            Priority = priority,
            Score = (int)Math.Round(priorityScore),
            Reason = reason,
            ResponseTime = responseTime,
            Department = department
        };
    }
    
    private static int CalculateGuestImpact(string description)
    {
        // Critical guest impact keywords
        if (ContainsAny(description, new[] { "stuck in room", "locked out", "no entry", "can't get in", "trapped" }))
            return 100;
        
        if (ContainsAny(description, new[] { "medical", "emergency", "injured", "sick", "pain", "bleeding" }))
            return 95;
        
        if (ContainsAny(description, new[] { "no hot water", "no water", "toilet not working", "shower not working" }))
            return 80;
        
        if (ContainsAny(description, new[] { "ac not working", "air conditioning", "too hot", "too cold", "no heating" }))
            return 75;
        
        if (ContainsAny(description, new[] { "tv not working", "television", "no internet", "wifi down" }))
            return 60;
        
        if (ContainsAny(description, new[] { "noise", "loud", "complaint", "unclean", "dirty" }))
            return 70;
        
        if (ContainsAny(description, new[] { "room service", "food", "restaurant", "dining" }))
            return 50;
        
        return 30; // Default moderate impact
    }
    
    private static int CalculateRevenueImpact(string description, bool isVipGuest)
    {
        if (isVipGuest) return 80; // VIP guests have high revenue impact
        
        if (ContainsAny(description, new[] { "check-in", "checkout", "overbooking", "reservation" }))
            return 70;
        
        if (ContainsAny(description, new[] { "room service", "minibar", "restaurant", "spa" }))
            return 50;
        
        if (ContainsAny(description, new[] { "housekeeping", "maintenance", "cleaning" }))
            return 30;
        
        return 20; // Default low revenue impact
    }
    
    private static int CalculateSafetyRisk(string description)
    {
        // Critical safety issues
        if (ContainsAny(description, new[] { "fire", "smoke", "gas leak", "electrical", "power outage" }))
            return 100;
        
        if (ContainsAny(description, new[] { "water leak", "flood", "ceiling", "structural" }))
            return 90;
        
        if (ContainsAny(description, new[] { "elevator", "stuck", "emergency", "medical" }))
            return 85;
        
        if (ContainsAny(description, new[] { "security", "theft", "unauthorized", "intruder" }))
            return 80;
        
        if (ContainsAny(description, new[] { "slip", "fall", "injury", "broken glass" }))
            return 75;
        
        return 10; // Default low safety risk
    }
    
    private static int CalculateOperationalImpact(string description)
    {
        if (ContainsAny(description, new[] { "internet outage", "system down", "pos down", "network" }))
            return 90;
        
        if (ContainsAny(description, new[] { "elevator failure", "hvac down", "kitchen equipment" }))
            return 80;
        
        if (ContainsAny(description, new[] { "check-in delay", "overbooking", "front desk" }))
            return 70;
        
        if (ContainsAny(description, new[] { "housekeeping", "room not ready", "cleaning" }))
            return 50;
        
        return 30; // Default moderate operational impact
    }
    
    private static int CalculateSlaBreachRisk(string description)
    {
        if (ContainsAny(description, new[] { "urgent", "immediately", "asap", "emergency" }))
            return 80;
        
        if (ContainsAny(description, new[] { "guest waiting", "check-in delay", "complaint" }))
            return 60;
        
        return 30; // Default moderate SLA risk
    }
    
    private static (TaskPriority priority, string responseTime, string department) DeterminePriority(double score, string description)
    {
        var department = DetermineDepartment(description);
        
        if (score >= 90)
            return (TaskPriority.Critical, "Immediate", department);
        
        if (score >= 70)
            return (TaskPriority.High, "Under 30 minutes", department);
        
        if (score >= 40)
            return (TaskPriority.Medium, "1-4 hours", department);
        
        return (TaskPriority.Low, "Within 24 hours", department);
    }
    
    private static string DetermineDepartment(string description)
    {
        if (ContainsAny(description, new[] { "security", "theft", "unauthorized", "intruder", "emergency" }))
            return "Security";
        
        if (ContainsAny(description, new[] { "broken", "repair", "fix", "maintenance", "electrical", "plumbing", "hvac", "elevator" }))
            return "Maintenance";
        
        if (ContainsAny(description, new[] { "food", "restaurant", "dining", "kitchen", "room service", "minibar" }))
            return "F&B";
        
        if (ContainsAny(description, new[] { "housekeeping", "cleaning", "towels", "linens", "unclean" }))
            return "Housekeeping";
        
        if (ContainsAny(description, new[] { "check-in", "checkout", "reservation", "front desk", "billing" }))
            return "Front Desk";
        
        return "Management";
    }
    
    private static string GenerateReason(TaskPriority priority, int guestImpact, int safetyRisk, int operationalImpact, bool isVipGuest)
    {
        if (priority == TaskPriority.Critical)
        {
            if (safetyRisk >= 80) return "Critical safety risk requiring immediate response.";
            if (guestImpact >= 90) return "Severe guest impact requiring immediate attention.";
            return "Critical operational issue requiring immediate intervention.";
        }
        
        if (priority == TaskPriority.High)
        {
            if (isVipGuest) return "High priority request from VIP guest.";
            if (guestImpact >= 70) return "High guest impact requiring prompt response.";
            if (safetyRisk >= 50) return "Safety concern requiring urgent attention.";
            return "High operational priority requiring timely response.";
        }
        
        if (priority == TaskPriority.Medium)
        {
            return "Standard operational request with moderate priority.";
        }
        
        return "Routine request with standard priority handling.";
    }
    
    private static bool ContainsAny(string text, string[] keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}

public class PriorityAssessment
{
    public TaskPriority Priority { get; set; }
    public int Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ResponseTime { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}