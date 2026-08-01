namespace HospitalityAI.Domain.Dtos;

using HospitalityAI.Domain.Enums;

public class StaffUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public StaffRole Role { get; set; }
}
