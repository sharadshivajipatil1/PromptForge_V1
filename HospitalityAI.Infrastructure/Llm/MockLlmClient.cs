namespace HospitalityAI.Infrastructure.Llm;

using HospitalityAI.Domain.Interfaces;

public class MockLlmClient : ILlmClient
{
    public Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var normalizedSystem = (systemPrompt ?? string.Empty).ToLowerInvariant();
        var normalizedUser = (userPrompt ?? string.Empty).ToLowerInvariant();

        if (normalizedSystem.Contains("chatbot"))
        {
            return Task.FromResult(HandleConciergeChat(normalizedUser));
        }

        if (normalizedSystem.Contains("triage"))
        {
            return Task.FromResult(HandleTriage(normalizedUser));
        }

        if (normalizedSystem.Contains("personaliz"))
        {
            return Task.FromResult("Recommendations are the best personalized match for this guest.");
        }

        if (normalizedSystem.Contains("prioritiz"))
        {
            return Task.FromResult("Tasks ranked by SLA risk, impact, and severity.");
        }

        if (normalizedSystem.Contains("forecast"))
        {
            return Task.FromResult("Staffing and inventory adjusted per occupancy trend.");
        }

        if (normalizedSystem.Contains("identity") || normalizedSystem.Contains("check-in") || normalizedSystem.Contains("checkout"))
        {
            return Task.FromResult(normalizedSystem.Contains("checkout")
                ? "Folio closed, key access revoked."
                : "ID/selfie verified successfully.");
        }

        return Task.FromResult("Request processed successfully.");
    }

    private static string HandleConciergeChat(string message)
    {
        if (message.Contains("goodbye") || message.Contains("bye") || message.Contains("thank you") || message.Contains("thanks"))
        {
            return "You’re very welcome. Please let me know if I can help with anything else.";
        }

        if (message.Contains("hello") || message.Contains("hi") || message.Contains("greeting") || message.Contains("good morning") || message.Contains("good evening"))
        {
            return "Hello! I’m here to help with your stay and any hotel needs.";
        }

        if (message.Contains("help"))
        {
            return "I’m happy to help with your stay, dining, spa, or other hotel requests.";
        }

        if (MatchesActionableRequest(message))
        {
            return "ESCALATE_TO_FRONTDESK";
        }

        return "ESCALATE_TO_FRONTDESK";
    }

    private static bool MatchesActionableRequest(string message)
    {
        var verbs = new[] { "send", "bring", "deliver", "arrange", "fix", "repair", "replace", "broken", "not working", "extra", "complain", "complaint", "issue with", "problem with", "someone to my room", "clean my room" };
        return verbs.Any(verb => message.Contains(verb));
    }

    private static string HandleTriage(string description)
    {
        var lower = description.ToLowerInvariant();
        var criticalTerms = new[] { "injury", "injured", "hurt", "bleeding", "unconscious", "chest pain", "can't breathe", "cannot breathe", "allergic reaction", "slipped", "fell", "fall", "collapsed", "seizure", "overdose", "choking" };
        var urgentTerms = new[] { "overflowing", "overflow", "infestation", "bed bug", "bedbug", "pest", "mold", "sewage", "foul smell", "no hot water", "locked out", "intruder", "theft", "stolen", "trespass" };

        if (criticalTerms.Any(term => ContainsWholeWord(lower, term)))
        {
            return "CRITICAL: Immediate safety concern detected.";
        }

        if (urgentTerms.Any(term => ContainsWholeWord(lower, term)))
        {
            return "URGENT: High-priority service issue detected.";
        }

        return "ROUTINE: Standard service request.";
    }

    private static bool ContainsWholeWord(string text, string term)
    {
        var normalizedTerm = term.Trim();
        if (string.IsNullOrWhiteSpace(normalizedTerm))
        {
            return false;
        }

        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(normalizedTerm);
    }
}
