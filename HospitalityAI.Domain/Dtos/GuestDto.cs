namespace HospitalityAI.Domain.Dtos;

public class GuestDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ReservationCode { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "en";
    public string? RoomNumber { get; set; }
    public bool IsCheckedIn { get; set; }
    public List<GuestHistoryDto> History { get; set; } = new();
}
