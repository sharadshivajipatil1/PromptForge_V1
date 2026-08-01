namespace HospitalityAI.Domain.Dtos;

public class ChatMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string? GuestId { get; set; }
    public string Sender { get; set; } = "Guest";
    public string Language { get; set; } = "en";
    public string OriginalText { get; set; } = string.Empty;
    public string? TranslatedText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
