namespace HospitalityAI.Agents.Operations;

public sealed class WorkflowIntentClassifier
{
    private static readonly string[] OperationsKeywords = { 
        "towel", "housekeeping", "maintenance", "leak", "clean", "room service", 
        "broken", "fix", "repair", "not working", "doesn't work", "won't work", "isn't working",
        "tv", "television", "remote", "air conditioning", "ac", "heating", "lights", "lamp",
        "toilet", "shower", "bath", "sink", "faucet", "drain", "clogged",
        "service", "help", "problem", "issue", "trouble"
    };
    private static readonly string[] ForecastKeywords = { "forecast", "staffing", "occupancy" };

    public WorkflowIntent Classify(string message)
    {
        var lowered = message.ToLowerInvariant();
        if (ForecastKeywords.Any(keyword => lowered.Contains(keyword)))
        {
            return WorkflowIntent.Forecast;
        }

        return OperationsKeywords.Any(keyword => lowered.Contains(keyword)) ? WorkflowIntent.Operations : WorkflowIntent.Concierge;
    }
}
