namespace HospitalityAI.Domain.Configuration;

public class SeedDataSettings
{
    public List<SeedGuest> Guests { get; set; } = new();
    public List<SeedStaffUser> StaffUsers { get; set; } = new();
}

public class SeedGuest
{
    public string FullName { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = string.Empty;
    public string LoyaltyTier { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string ReservationCode { get; set; } = string.Empty;
    public string Profession { get; set; } = string.Empty;
    public string TripPurpose { get; set; } = string.Empty;
    public List<SeedHistoryEntry> History { get; set; } = new();
}

public class SeedHistoryEntry
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public int? Rating { get; set; }
}

public class SeedStaffUser
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string DefaultPassword { get; set; } = string.Empty;
}
