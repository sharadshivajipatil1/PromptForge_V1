namespace HospitalityAI.Domain.Models;

using System.ComponentModel.DataAnnotations;
using HospitalityAI.Domain.Enums;

public class StaffUser
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string PasswordSalt { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string FullName { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public StaffRole Role { get; set; } = StaffRole.FrontDesk;
}
