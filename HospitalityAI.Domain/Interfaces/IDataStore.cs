namespace HospitalityAI.Domain.Interfaces;

using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Models;

public interface IDataStore
{
    Task<Guest?> GetGuestByIdAsync(string guestId, CancellationToken ct = default);
    Task<Guest?> GetGuestByReservationCodeAsync(string reservationCode, CancellationToken ct = default);
    Task<IReadOnlyList<Guest>> GetGuestsAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<PagedResult<Guest>> SearchGuestsAsync(string? searchTerm, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<Guest> SaveGuestAsync(Guest guest, CancellationToken ct = default);
    Task<bool> DeleteGuestAsync(string guestId, CancellationToken ct = default);

    Task<StaffUser?> GetStaffByUsernameAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<StaffUser>> GetStaffUsersAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<StaffUser> SaveStaffUserAsync(StaffUser staffUser, CancellationToken ct = default);
    Task<bool> DeleteStaffUserAsync(string staffUserId, CancellationToken ct = default);

    Task<IReadOnlyList<StaffTask>> GetOpenTasksAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StaffTask>> GetTasksAsync(TaskStatus? status = null, TaskPriority? priority = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<PagedResult<StaffTask>> SearchTasksAsync(string? searchTerm, TaskStatus? status = null, TaskPriority? priority = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<StaffTask?> GetTaskByIdAsync(string taskId, CancellationToken ct = default);
    Task<StaffTask> SaveTaskAsync(StaffTask task, CancellationToken ct = default);
    Task<bool> DeleteTaskAsync(string taskId, CancellationToken ct = default);

    Task<CheckInRequest> SaveCheckInRequestAsync(CheckInRequest request, CancellationToken ct = default);
    Task<CheckInRequest?> GetLatestCheckInRequestAsync(string guestId, CancellationToken ct = default);
    Task<IReadOnlyList<CheckInRequest>> GetCheckInRequestsAsync(string? guestId = null, int page = 1, int pageSize = 50, CancellationToken ct = default);

    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string conversationId, CancellationToken ct = default);
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string conversationId, int page, int pageSize, CancellationToken ct = default);
    Task<ChatMessage> SaveMessageAsync(ChatMessage message, CancellationToken ct = default);
    Task<bool> DeleteMessagesAsync(string conversationId, CancellationToken ct = default);

    Task<ForecastRecord> SaveForecastAsync(ForecastRecord forecast, CancellationToken ct = default);
    Task<ForecastRecord?> GetForecastByIdAsync(string forecastId, CancellationToken ct = default);
    Task<PagedResult<ForecastRecord>> GetForecastsAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);

    Task<ConciergeTicket> SaveTicketAsync(ConciergeTicket ticket, CancellationToken ct = default);
    Task<ConciergeTicket?> GetTicketByIdAsync(string ticketId, CancellationToken ct = default);
    Task<PagedResult<ConciergeTicket>> GetTicketsAsync(string? searchTerm = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<bool> DeleteTicketAsync(string ticketId, CancellationToken ct = default);

    Task<SpaSlot> SaveSpaSlotAsync(SpaSlot slot, CancellationToken ct = default);
    Task<SpaSlot?> GetSpaSlotByIdAsync(string slotId, CancellationToken ct = default);
    Task<IReadOnlyList<SpaSlot>> GetAvailableSpaSlotsAsync(CancellationToken ct = default);
    Task<bool> DeleteSpaSlotAsync(string slotId, CancellationToken ct = default);

    Task<DiningOption> SaveDiningOptionAsync(DiningOption option, CancellationToken ct = default);
    Task<DiningOption?> GetDiningOptionByIdAsync(string optionId, CancellationToken ct = default);
    Task<IReadOnlyList<DiningOption>> GetDiningOptionsAsync(CancellationToken ct = default);
    Task<bool> DeleteDiningOptionAsync(string optionId, CancellationToken ct = default);
}
