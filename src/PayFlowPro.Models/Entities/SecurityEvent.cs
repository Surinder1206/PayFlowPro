using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Models.Entities;

public class SecurityEvent
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Severity { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [MaxLength(450)]
    public string? UserId { get; set; }

    [MaxLength(200)]
    public string? UserEmail { get; set; }

    [MaxLength(100)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(100)]
    public string? Resource { get; set; }

    [MaxLength(50)]
    public string? HttpMethod { get; set; }

    [MaxLength(500)]
    public string? RequestUrl { get; set; }

    public int? ResponseCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    public bool IsResolved { get; set; } = false;

    public DateTime? ResolvedAt { get; set; }

    [MaxLength(100)]
    public string? ResolvedBy { get; set; }

    public string? ResolutionNotes { get; set; }

    public string? AdditionalData { get; set; }

    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
}