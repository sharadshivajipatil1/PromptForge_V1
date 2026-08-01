namespace HospitalityAI.Infrastructure.Storage;

using System.Text.Json;
using HospitalityAI.Domain.Configuration;
using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Interfaces.Configuration;
using HospitalityAI.Domain.Models;
using HospitalityAI.Domain.ValueObjects;

public class InMemoryDataStore : IDataStore
{
    private const string TaskStoreFileName = "hospitality-task-store.json";
    private readonly object _lock = new();
    private readonly List<Guest> _guests = new();
    private readonly List<StaffUser> _staffUsers = new();
    private readonly List<StaffTask> _tasks = new();
    private readonly List<CheckInRequest> _checkIns = new();
    private readonly List<ChatMessage> _messages = new();
    private readonly List<ForecastRecord> _forecasts = new();
    private readonly List<ConciergeTicket> _tickets = new();
    private readonly List<SpaSlot> _spaSlots = new();
    private readonly List<DiningOption> _diningOptions = new();
    private readonly IReferenceDataLoader _referenceDataLoader;
    private readonly IPasswordHasher _passwordHasher;

    public InMemoryDataStore(IReferenceDataLoader referenceDataLoader, IPasswordHasher passwordHasher)
    {
        _referenceDataLoader = referenceDataLoader;
        _passwordHasher = passwordHasher;
        LoadTasksFromFile();
        SeedInitialData();
    }

    private void LoadTasksFromFile()
    {
        lock (_lock)
        {
            try
            {
                var path = ResolveTaskStorePath();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    return;
                }

                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                var snapshot = JsonSerializer.Deserialize<TaskStoreSnapshot>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (snapshot is not null && snapshot.Tasks.Count > 0)
                {
                    _tasks.Clear();
                    foreach (var task in snapshot.Tasks)
                    {
                        _tasks.Add(task);
                    }
                }
            }
            catch
            {
                // Ignore file corruption so the app can continue operating with in-memory state.
            }
        }
    }

    private void PersistTasksToFile()
    {
        try
        {
            var path = ResolveTaskStorePath();
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var snapshot = new TaskStoreSnapshot
            {
                Tasks = _tasks.OrderByDescending(task => task.CreatedAt).ToList()
            };

            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // Ignore persistence failures so the app remains resilient if the file system is unavailable.
        }
    }

    private string? ResolveTaskStorePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionFile = Path.Combine(directory.FullName, "HospitalityAI.sln");
            var apiProjectFile = Path.Combine(directory.FullName, "HospitalityAI.Api", "HospitalityAI.Api.csproj");
            if (File.Exists(solutionFile) || File.Exists(apiProjectFile))
            {
                return Path.Combine(directory.FullName, TaskStoreFileName);
            }

            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, TaskStoreFileName);
    }

    private void SeedInitialData()
    {
        try
        {
            var seedData = _referenceDataLoader.LoadSeedDataAsync(CancellationToken.None).GetAwaiter().GetResult();
            lock (_lock)
            {
                foreach (var seedGuest in seedData.Guests)
                {
                    _guests.Add(new Guest
                    {
                        Id = Guid.NewGuid().ToString(),
                        FullName = seedGuest.FullName,
                        PreferredLanguage = seedGuest.PreferredLanguage,
                        LoyaltyTier = seedGuest.LoyaltyTier,
                        RoomNumber = seedGuest.RoomNumber,
                        ReservationCode = seedGuest.ReservationCode,
                        Profession = seedGuest.Profession,
                        TripPurpose = seedGuest.TripPurpose,
                        History = seedGuest.History.Select(entry => new HistoryEntry
                        {
                            Type = entry.Type,
                            Description = entry.Description,
                            Date = entry.Date,
                            Rating = entry.Rating
                        }).ToList()
                    });
                }

                foreach (var seedStaff in seedData.StaffUsers)
                {
                    Enum.TryParse<StaffRole>(seedStaff.Role, ignoreCase: true, out var role);
                    var (passwordHash, passwordSalt) = _passwordHasher.HashPassword(string.IsNullOrWhiteSpace(seedStaff.DefaultPassword) ? "Staff@123" : seedStaff.DefaultPassword);
                    _staffUsers.Add(new StaffUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        Username = seedStaff.Username,
                        PasswordHash = passwordHash,
                        PasswordSalt = passwordSalt,
                        FullName = seedStaff.FullName,
                        Department = seedStaff.Department,
                        Role = role
                    });
                }
            }
        }
        catch
        {
            // Fall back to built-in defaults if the JSON files cannot be loaded.
        }

        lock (_lock)
        {
            if (_guests.Count == 0)
            {
                _guests.Add(new Guest { Id = "guest-1", FullName = "Priya Sharma", ReservationCode = "RES-8842", RoomNumber = "812", Profession = "Software Engineer", TripPurpose = "Leisure" });
                _guests.Add(new Guest { Id = "guest-2", FullName = "James Carter", ReservationCode = "RES-1190", RoomNumber = "204", Profession = "Sales Executive", TripPurpose = "Business" });
            }

            if (_staffUsers.Count == 0)
            {
                var (managerHash, managerSalt) = _passwordHasher.HashPassword("Staff@123");
                var (frontDeskHash, frontDeskSalt) = _passwordHasher.HashPassword("Staff@123");
                _staffUsers.Add(new StaffUser { Id = "staff-1", Username = "manager", FullName = "Morgan Ellis", Department = "Operations", Role = StaffRole.Manager, PasswordHash = managerHash, PasswordSalt = managerSalt });
                _staffUsers.Add(new StaffUser { Id = "staff-2", Username = "frontdesk", FullName = "Dana Reyes", Department = "Front Desk", Role = StaffRole.FrontDesk, PasswordHash = frontDeskHash, PasswordSalt = frontDeskSalt });
            }

            if (_spaSlots.Count == 0)
            {
                _spaSlots.Add(new SpaSlot { Id = "spa-1", ServiceName = "Swedish Massage", StartTime = DateTimeOffset.UtcNow.AddHours(2), DurationHours = 1, IsAvailable = true });
            }

            if (_diningOptions.Count == 0)
            {
                _diningOptions.Add(new DiningOption { Id = "dining-1", Name = "Garden Cafe Breakfast", Description = "Fresh breakfast buffet", Price = 24m, Category = "Breakfast" });
            }
        }
    }

    public Task<Guest?> GetGuestByIdAsync(string guestId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_guests.FirstOrDefault(g => g.Id == guestId));
        }
    }

    public Task<Guest?> GetGuestByReservationCodeAsync(string reservationCode, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_guests.FirstOrDefault(g => g.ReservationCode == reservationCode));
        }
    }

    public Task<IReadOnlyList<Guest>> GetGuestsAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Max(pageSize, 1);
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Guest>>(_guests.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToList());
        }
    }

    public Task<PagedResult<Guest>> SearchGuestsAsync(string? searchTerm, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Max(pageSize, 1);
        lock (_lock)
        {
            var query = _guests.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(g => g.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) || g.ReservationCode.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var totalCount = query.Count();
            var items = query.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToList();
            return Task.FromResult(new PagedResult<Guest> { Items = items, Page = safePage, PageSize = safePageSize, TotalCount = totalCount });
        }
    }

    public Task<Guest> SaveGuestAsync(Guest guest, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var existing = _guests.FirstOrDefault(g => g.Id == guest.Id);
            if (existing is null)
            {
                _guests.Add(guest);
            }
            else
            {
                var index = _guests.IndexOf(existing);
                _guests[index] = guest;
            }

            return Task.FromResult(guest);
        }
    }

    public Task<bool> DeleteGuestAsync(string guestId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var removed = _guests.RemoveAll(g => g.Id == guestId) > 0;
            return Task.FromResult(removed);
        }
    }

    public Task<StaffUser?> GetStaffByUsernameAsync(string username, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_staffUsers.FirstOrDefault(s => s.Username == username));
        }
    }

    public Task<IReadOnlyList<StaffUser>> GetStaffUsersAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Max(pageSize, 1);
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<StaffUser>>(_staffUsers.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToList());
        }
    }

    public Task<StaffUser> SaveStaffUserAsync(StaffUser staffUser, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var existing = _staffUsers.FirstOrDefault(s => s.Id == staffUser.Id);
            if (existing is null)
            {
                _staffUsers.Add(staffUser);
            }
            else
            {
                var index = _staffUsers.IndexOf(existing);
                _staffUsers[index] = staffUser;
            }

            PersistStaffUsersToSeedFile();
            return Task.FromResult(staffUser);
        }
    }

    public Task<bool> DeleteStaffUserAsync(string staffUserId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var removed = _staffUsers.RemoveAll(s => s.Id == staffUserId) > 0;
            return Task.FromResult(removed);
        }
    }

    public Task<IReadOnlyList<StaffTask>> GetOpenTasksAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<StaffTask>>(_tasks.Where(t => t.Status != TaskStatus.Completed).ToList());
        }
    }

    public Task<IReadOnlyList<StaffTask>> GetTasksAsync(TaskStatus? status = null, TaskPriority? priority = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Max(pageSize, 1);
        lock (_lock)
        {
            var query = _tasks.AsEnumerable();
            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (priority.HasValue)
            {
                query = query.Where(t => t.Priority == priority.Value);
            }

            var items = query.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToList();
            return Task.FromResult<IReadOnlyList<StaffTask>>(items);
        }
    }

    public Task<PagedResult<StaffTask>> SearchTasksAsync(string? searchTerm, TaskStatus? status = null, TaskPriority? priority = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Max(pageSize, 1);
        lock (_lock)
        {
            var query = _tasks.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(t => t.Description.Contains(term, StringComparison.OrdinalIgnoreCase) || t.RoomNumber.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (priority.HasValue)
            {
                query = query.Where(t => t.Priority == priority.Value);
            }

            var totalCount = query.Count();
            var items = query.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToList();
            return Task.FromResult(new PagedResult<StaffTask> { Items = items, Page = safePage, PageSize = safePageSize, TotalCount = totalCount });
        }
    }

    public Task<StaffTask?> GetTaskByIdAsync(string taskId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_tasks.FirstOrDefault(t => t.Id == taskId));
        }
    }

    public Task<StaffTask> SaveTaskAsync(StaffTask task, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existing is null)
            {
                _tasks.Add(task);
            }
            else
            {
                var index = _tasks.IndexOf(existing);
                _tasks[index] = task;
            }

            PersistTasksToFile();
            return Task.FromResult(task);
        }
    }

    public Task<bool> DeleteTaskAsync(string taskId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var removed = _tasks.RemoveAll(t => t.Id == taskId) > 0;
            if (removed)
            {
                PersistTasksToFile();
            }

            return Task.FromResult(removed);
        }
    }

    private void PersistStaffUsersToSeedFile()
    {
        try
        {
            var path = ResolveSeedDataPath();
            if (path is null)
            {
                return;
            }

            var json = File.ReadAllText(path);
            var seedData = JsonSerializer.Deserialize<SeedDataSettings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new SeedDataSettings();

            foreach (var staffUser in _staffUsers)
            {
                var existing = seedData.StaffUsers.FirstOrDefault(staff => string.Equals(staff.Username, staffUser.Username, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                {
                    seedData.StaffUsers.Add(new SeedStaffUser
                    {
                        Username = staffUser.Username,
                        FullName = staffUser.FullName,
                        Department = staffUser.Department,
                        Role = staffUser.Role.ToString(),
                        DefaultPassword = string.Empty
                    });
                }
                else
                {
                    existing.FullName = staffUser.FullName;
                    existing.Department = staffUser.Department;
                    existing.Role = staffUser.Role.ToString();
                }
            }

            File.WriteAllText(path, JsonSerializer.Serialize(seedData, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Ignore persistence failures so runtime behavior remains resilient.
        }
    }

    private string? ResolveSeedDataPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "seed-data.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private sealed class TaskStoreSnapshot
    {
        public List<StaffTask> Tasks { get; set; } = new();
    }

    public Task<CheckInRequest> SaveCheckInRequestAsync(CheckInRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            _checkIns.Add(request);
            return Task.FromResult(request);
        }
    }

    public Task<CheckInRequest?> GetLatestCheckInRequestAsync(string guestId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_checkIns.LastOrDefault(c => c.GuestId == guestId));
        }
    }

    public Task<IReadOnlyList<CheckInRequest>> GetCheckInRequestsAsync(string? guestId = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Max(pageSize, 1);
        lock (_lock)
        {
            var query = _checkIns.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(guestId))
            {
                query = query.Where(c => c.GuestId == guestId);
            }

            var items = query.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToList();
            return Task.FromResult<IReadOnlyList<CheckInRequest>>(items);
        }
    }

    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string conversationId, CancellationToken ct = default) => GetMessagesAsync(conversationId, 1, 50, ct);

    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string conversationId, int page, int pageSize, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Max(pageSize, 1);
        lock (_lock)
        {
            var items = _messages.Where(m => m.ConversationId == conversationId).Skip((safePage - 1) * safePageSize).Take(safePageSize).ToList();
            return Task.FromResult<IReadOnlyList<ChatMessage>>(items);
        }
    }

    public Task<ChatMessage> SaveMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            _messages.Add(message);
            return Task.FromResult(message);
        }
    }

    public Task<bool> DeleteMessagesAsync(string conversationId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var removed = _messages.RemoveAll(m => m.ConversationId == conversationId) > 0;
            return Task.FromResult(removed);
        }
    }

    public Task<ForecastRecord> SaveForecastAsync(ForecastRecord forecast, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            _forecasts.Add(forecast);
            return Task.FromResult(forecast);
        }
    }

    public Task<ForecastRecord?> GetForecastByIdAsync(string forecastId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_forecasts.FirstOrDefault(f => f.Id == forecastId));
        }
    }

    public Task<PagedResult<ForecastRecord>> GetForecastsAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Max(pageSize, 1);
        lock (_lock)
        {
            var totalCount = _forecasts.Count;
            var items = _forecasts.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToList();
            return Task.FromResult(new PagedResult<ForecastRecord> { Items = items, Page = safePage, PageSize = safePageSize, TotalCount = totalCount });
        }
    }

    public Task<ConciergeTicket> SaveTicketAsync(ConciergeTicket ticket, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var existing = _tickets.FirstOrDefault(t => t.Id == ticket.Id);
            if (existing is null)
            {
                _tickets.Add(ticket);
            }
            else
            {
                var index = _tickets.IndexOf(existing);
                _tickets[index] = ticket;
            }

            return Task.FromResult(ticket);
        }
    }

    public Task<ConciergeTicket?> GetTicketByIdAsync(string ticketId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_tickets.FirstOrDefault(t => t.Id == ticketId));
        }
    }

    public Task<PagedResult<ConciergeTicket>> GetTicketsAsync(string? searchTerm = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Max(pageSize, 1);
        lock (_lock)
        {
            var query = _tickets.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(t => t.GuestName.Contains(term, StringComparison.OrdinalIgnoreCase) || t.Message.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var totalCount = query.Count();
            var items = query.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToList();
            return Task.FromResult(new PagedResult<ConciergeTicket> { Items = items, Page = safePage, PageSize = safePageSize, TotalCount = totalCount });
        }
    }

    public Task<bool> DeleteTicketAsync(string ticketId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var removed = _tickets.RemoveAll(t => t.Id == ticketId) > 0;
            return Task.FromResult(removed);
        }
    }

    public Task<SpaSlot> SaveSpaSlotAsync(SpaSlot slot, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var existing = _spaSlots.FirstOrDefault(s => s.Id == slot.Id);
            if (existing is null)
            {
                _spaSlots.Add(slot);
            }
            else
            {
                var index = _spaSlots.IndexOf(existing);
                _spaSlots[index] = slot;
            }

            return Task.FromResult(slot);
        }
    }

    public Task<SpaSlot?> GetSpaSlotByIdAsync(string slotId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_spaSlots.FirstOrDefault(s => s.Id == slotId));
        }
    }

    public Task<IReadOnlyList<SpaSlot>> GetAvailableSpaSlotsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<SpaSlot>>(_spaSlots.Where(s => s.IsAvailable).ToList());
        }
    }

    public Task<bool> DeleteSpaSlotAsync(string slotId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var removed = _spaSlots.RemoveAll(s => s.Id == slotId) > 0;
            return Task.FromResult(removed);
        }
    }

    public Task<DiningOption> SaveDiningOptionAsync(DiningOption option, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var existing = _diningOptions.FirstOrDefault(d => d.Id == option.Id);
            if (existing is null)
            {
                _diningOptions.Add(option);
            }
            else
            {
                var index = _diningOptions.IndexOf(existing);
                _diningOptions[index] = option;
            }

            return Task.FromResult(option);
        }
    }

    public Task<DiningOption?> GetDiningOptionByIdAsync(string optionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_diningOptions.FirstOrDefault(d => d.Id == optionId));
        }
    }

    public Task<IReadOnlyList<DiningOption>> GetDiningOptionsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<DiningOption>>(_diningOptions.ToList());
        }
    }

    public Task<bool> DeleteDiningOptionAsync(string optionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var removed = _diningOptions.RemoveAll(d => d.Id == optionId) > 0;
            return Task.FromResult(removed);
        }
    }
}
