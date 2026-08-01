namespace HospitalityAI.Domain.Models;

using System.ComponentModel.DataAnnotations;

public class ChatMessage
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(128)]
    public string ConversationId { get; set; } = string.Empty;

    public string? GuestId { get; set; }

    [Required]
    [MaxLength(16)]
    public string Sender { get; set; } = "Guest";

    [MaxLength(16)]
    public string Language { get; set; } = "en";

    [Required]
    [MaxLength(2048)]
    public string OriginalText { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? TranslatedText { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
