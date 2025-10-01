using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Models.Entities;

public class UserSession
{
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string SessionId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    [MaxLength(50)]
    public string? DeviceType { get; set; }

    public DateTime LoginTime { get; set; } = DateTime.UtcNow;

    public DateTime? LogoutTime { get; set; }

    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public bool IsBlocked { get; set; } = false;

    [MaxLength(200)]
    public string? BlockReason { get; set; }

    public int LoginAttempts { get; set; } = 0;

    public DateTime? LastLoginAttempt { get; set; }

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
}