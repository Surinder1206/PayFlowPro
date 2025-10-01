using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Models.Entities;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string UserEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(50)]
    public string Severity { get; set; } = "Info";

    [MaxLength(100)]
    public string Category { get; set; } = "General";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    public bool IsSuccess { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public string? StackTrace { get; set; }

    // Navigation properties for related entities
    public virtual ApplicationUser User { get; set; } = null!;
}