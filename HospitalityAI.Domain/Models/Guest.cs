namespace HospitalityAI.Domain.Models;

using System.ComponentModel.DataAnnotations;
using HospitalityAI.Domain.ValueObjects;

public class Guest
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(128)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(16)]
    public string PreferredLanguage { get; set; } = "en";

    [MaxLength(64)]
    public string LoyaltyTier { get; set; } = "Standard";

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [MaxLength(32)]
    public string? RoomNumber { get; set; }

    [Required]
    [MaxLength(32)]
    public string ReservationCode { get; set; } = string.Empty;

    public bool IsCheckedIn { get; set; }

    [MaxLength(64)]
    public string? Profession { get; set; }

    [MaxLength(64)]
    public string? TripPurpose { get; set; }

    public List<HistoryEntry> History { get; set; } = new();
}
