namespace HospitalityAI.Domain.Configuration;

/// <summary>
/// A single entry in the hotel knowledge base.
/// Keywords are matched against the guest's message (case-insensitive).
/// Category groups entries for prompt injection (e.g. "Rooms", "Amenities", "Dining").
/// Facts is a list of plain-English statements injected into the LLM prompt when matched.
/// </summary>
public class HotelKnowledgeEntry
{
    /// <summary>Logical category shown to the LLM — e.g. "Rooms", "Spa", "Dining".</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Topic name — e.g. "Deluxe King Room", "Conference Room".</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>Keywords that trigger this entry when found in the guest message.</summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// Plain-English facts about this topic injected verbatim into the LLM system prompt.
    /// Each string is one fact sentence.
    /// </summary>
    public List<string> Facts { get; set; } = new();
}
