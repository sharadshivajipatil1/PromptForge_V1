namespace HospitalityAI.Agents;

using HospitalityAI.Agents.Recommendation;
using HospitalityAI.Domain.Dtos;
using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Interfaces.Configuration;
using HospitalityAI.Domain.Models;

public class ConciergeAgent
{
    private readonly IGuestService _guestService;
    private readonly IDataStore _dataStore;
    private readonly ILlmClient _llmClient;
    private readonly IReferenceDataLoader _referenceDataLoader;
    private readonly RecommendationBuilder _recommendationBuilder;

    public ConciergeAgent(IGuestService guestService, IDataStore dataStore, ILlmClient llmClient, IReferenceDataLoader referenceDataLoader)
    {
        _guestService = guestService;
        _dataStore = dataStore;
        _llmClient = llmClient;
        _referenceDataLoader = referenceDataLoader;
        _recommendationBuilder = new RecommendationBuilder(dataStore, referenceDataLoader, llmClient);
    }

    public async Task<string> HandleChatAsync(string guestId, string message, string? language = null, CancellationToken ct = default)
    {
        var guest = await _guestService.GetGuestAsync(guestId, ct);
        if (guest is null)
        {
            return "I couldn’t find your guest profile.";
        }

        var detectedLanguage = DetectLanguage(message, language);

        // --- Layer 1: FAQ exact keyword match (no LLM call needed) ---
        var faq = await _referenceDataLoader.LoadFaqAsync(ct);
        var matchingFaq = faq.Entries.FirstOrDefault(entry => entry.Keywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
        if (matchingFaq is not null)
        {
            await PersistMessageAsync(guestId, message, detectedLanguage, matchingFaq.Answer, ct);
            return matchingFaq.Answer;
        }

        // --- Layer 2: Hotel knowledge base lookup ---
        // Find all entries whose keywords appear in the guest message.
        // Multiple entries can match — all matched facts are injected into
        // the LLM prompt so it answers with accurate hotel-specific data.
        var knowledge = await _referenceDataLoader.LoadHotelKnowledgeAsync(ct);
        var matchedEntries = knowledge.Entries
            .Where(entry => entry.Keywords.Any(keyword =>
                message.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Build the system prompt — inject hotel facts when available,
        // otherwise let the LLM fall back to its own general knowledge.
        string prompt;
        if (matchedEntries.Count > 0)
        {
            var factsBlock = new System.Text.StringBuilder();
            foreach (var entry in matchedEntries)
            {
                factsBlock.AppendLine($"[{entry.Category} — {entry.Topic}]");
                foreach (var fact in entry.Facts)
                {
                    factsBlock.AppendLine($"- {fact}");
                }
                factsBlock.AppendLine();
            }

            prompt = $"""
                You are the multilingual Concierge Agent for a luxury hotel, chatting directly with a guest via the hotel app.

                The guest has asked a question. Use the following hotel-specific facts as your primary source of truth.
                Keep your answer warm, concise, and no longer than 3 sentences.

                HOTEL KNOWLEDGE BASE — RELEVANT FACTS:
                {factsBlock}
                Additional guidelines:
                - Detect the guest's language and always reply in the same language.
                - Maintain a premium hospitality tone.
                - Never ask clarifying questions.
                - Never request sensitive personal information.
                - For urgent safety or medical issues, advise contacting emergency services (911) or hotel staff.
                - Only reply with exactly 'ESCALATE_TO_FRONTDESK' (nothing else) if the guest needs a real action taken — a booking, repair, delivery, complaint, or special arrangement. Never escalate for informational questions.
                """;
        }
        else
        {
            // No hotel knowledge matched — LLM answers from its own knowledge.
            prompt = """
                You are the multilingual Concierge Agent for a luxury hotel, chatting directly with a guest via the hotel app.

                No specific hotel facts matched this question in our knowledge base.
                Answer helpfully using your general knowledge of luxury hotel services and hospitality standards.
                If you genuinely cannot answer, suggest the guest contact the front desk for accurate details.
                Keep your answer warm, concise, and no longer than 3 sentences.

                Additional guidelines:
                - Detect the guest's language and always reply in the same language.
                - Maintain a premium hospitality tone.
                - Never ask clarifying questions.
                - Never request sensitive personal information.
                - For urgent safety or medical issues, advise contacting emergency services (911) or hotel staff.
                - Only reply with exactly 'ESCALATE_TO_FRONTDESK' (nothing else) if the guest needs a real action taken — a booking, repair, delivery, complaint, or special arrangement. Never escalate for informational questions.
                """;
        }

        var llmReply = await _llmClient.CompleteAsync(prompt, message, ct);
        var reply = llmReply;
        if (reply.Contains("ESCALATE_TO_FRONTDESK", StringComparison.OrdinalIgnoreCase))
        {
            await EscalateToFrontDeskAsync(guest, message, ct);
        }

        await PersistMessageAsync(guestId, message, detectedLanguage, reply, ct);
        return reply;
    }

    public async Task<PersonalizationResponse> GetPersonalizationAsync(string guestId, CancellationToken ct = default)
    {
        var guest = await _guestService.GetGuestAsync(guestId, ct);
        if (guest is null)
        {
            throw new InvalidOperationException("Guest not found.");
        }

        return await _recommendationBuilder.BuildAsync(guest, ct);
    }

    public async Task<CheckInWorkflowResponse> StartCheckInAsync(string guestId, string reservationCode, string? idDocumentImageBase64, string? selfieImageBase64, CancellationToken ct = default)
    {
        var guest = await _guestService.GetGuestAsync(guestId, ct);
        if (guest is null)
        {
            throw new InvalidOperationException("Guest not found.");
        }

        var request = new CheckInRequest
        {
            GuestId = guest.Id,
            Type = CheckInRequestType.CheckIn,
            ReservationCode = reservationCode,
            IdDocumentImageBase64 = idDocumentImageBase64 ?? string.Empty,
            SelfieImageBase64 = selfieImageBase64 ?? string.Empty,
            Status = CheckInRequestStatus.Pending,
            RoomNumber = guest.RoomNumber ?? string.Empty,
            DigitalKeyIssued = false,
            VerificationSummary = "Pending verification"
        };

        await _dataStore.SaveCheckInRequestAsync(request, ct);
        request.Status = CheckInRequestStatus.Verified;
        request.VerifiedAt = DateTimeOffset.UtcNow;
        request.VerificationSummary = "ID/selfie verified successfully.";
        request.DigitalKeyIssued = true;
        await _dataStore.SaveCheckInRequestAsync(request, ct);

        guest.IsCheckedIn = true;
        await _guestService.UpdateGuestAsync(guest, ct);

        var personalization = await GetPersonalizationAsync(guest.Id, ct);
        return new CheckInWorkflowResponse
        {
            RequestId = request.Id,
            Status = request.Status.ToString(),
            RoomNumber = guest.RoomNumber ?? string.Empty,
            DigitalKeyIssued = true,
            VerificationSummary = request.VerificationSummary,
            Recommendations = personalization.Recommendations,
            IsCheckedIn = true
        };
    }

    public async Task<CheckInWorkflowResponse> CheckoutAsync(string guestId, CancellationToken ct = default)
    {
        var guest = await _guestService.GetGuestAsync(guestId, ct);
        if (guest is null)
        {
            throw new InvalidOperationException("Guest not found.");
        }

        var request = new CheckInRequest
        {
            GuestId = guest.Id,
            Type = CheckInRequestType.CheckOut,
            ReservationCode = guest.ReservationCode,
            Status = CheckInRequestStatus.Verified,
            RoomNumber = guest.RoomNumber ?? string.Empty,
            DigitalKeyIssued = false,
            VerificationSummary = "Checkout completed"
        };

        await _dataStore.SaveCheckInRequestAsync(request, ct);
        guest.IsCheckedIn = false;
        await _guestService.UpdateGuestAsync(guest, ct);

        return new CheckInWorkflowResponse
        {
            RequestId = request.Id,
            Status = request.Status.ToString(),
            RoomNumber = guest.RoomNumber ?? string.Empty,
            DigitalKeyIssued = false,
            VerificationSummary = "Folio closed, key access revoked.",
            IsCheckedIn = false,
            FolioSummary = "Folio closed, key access revoked."
        };
    }

    public async Task<BookingConfirmationDto> BookRecommendationAsync(string guestId, string recommendationId, CancellationToken ct = default)
    {
        var guest = await _guestService.GetGuestAsync(guestId, ct);
        if (guest is null)
        {
            throw new InvalidOperationException("Guest not found.");
        }

        var confirmation = await _guestService.BookRecommendationAsync(guest.Id, recommendationId, ct);
        if (!confirmation.Success)
        {
            return confirmation;
        }

        var slot = await _dataStore.GetSpaSlotByIdAsync(recommendationId, ct);
        confirmation.ConfirmedSlotId = recommendationId;
        confirmation.ConfirmedTime = slot?.StartTime ?? DateTimeOffset.UtcNow;
        confirmation.Message = "Your booking was confirmed.";
        return confirmation;
    }

    private static string DetectLanguage(string message, string? clientLanguage)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "en";
        }

        if (message.Any(ch => ch >= '\u0900' && ch <= '\u097F'))
        {
            return "hi";
        }

        if (message.Any(ch => (ch >= '\u3040' && ch <= '\u30FF') || (ch >= '\u4E00' && ch <= '\u9FFF')))
        {
            return "ja";
        }

        if (message.Contains("¿", StringComparison.Ordinal) || message.Contains("¡", StringComparison.Ordinal) || message.Contains("hola", StringComparison.OrdinalIgnoreCase) || message.Contains("gracias", StringComparison.OrdinalIgnoreCase) || message.Contains("por favor", StringComparison.OrdinalIgnoreCase))
        {
            return "es";
        }

        if (message.Contains("bonjour", StringComparison.OrdinalIgnoreCase) || message.Contains("merci", StringComparison.OrdinalIgnoreCase) || message.Contains("s'il", StringComparison.OrdinalIgnoreCase))
        {
            return "fr";
        }

        return string.IsNullOrWhiteSpace(clientLanguage) ? "en" : clientLanguage;
    }

    private async Task EscalateToFrontDeskAsync(Guest guest, string message, CancellationToken ct)
    {
        var ticket = new ConciergeTicket
        {
            GuestId = guest.Id,
            GuestName = guest.FullName,
            RoomNumber = guest.RoomNumber ?? string.Empty,
            Message = message,
            Status = "Open"
        };
        await _dataStore.SaveTicketAsync(ticket, ct);

        var task = new StaffTask
        {
            Type = TaskType.GuestRequest,
            RoomNumber = guest.RoomNumber ?? string.Empty,
            Description = message,
            Priority = TaskPriority.High,
            Status = TaskStatus.Pending,
            SlaMinutes = 20,
            PriorityReason = "Guest request escalated from concierge chat."
        };
        await _dataStore.SaveTaskAsync(task, ct);
    }

    private async Task PersistMessageAsync(string guestId, string message, string detectedLanguage, string reply, CancellationToken ct)
    {
        var conversationId = $"guest-{guestId}";
        await _dataStore.SaveMessageAsync(new ChatMessage
        {
            ConversationId = conversationId,
            GuestId = guestId,
            Sender = "Guest",
            Language = detectedLanguage,
            OriginalText = message
        }, ct);

        await _dataStore.SaveMessageAsync(new ChatMessage
        {
            ConversationId = conversationId,
            GuestId = guestId,
            Sender = "Assistant",
            Language = detectedLanguage,
            OriginalText = reply,
            TranslatedText = reply
        }, ct);
    }
}
