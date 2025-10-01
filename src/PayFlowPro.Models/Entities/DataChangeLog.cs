using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Models.Entities;

public class DataChangeLog
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string EntityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ChangeType { get; set; } = string.Empty; // Create, Update, Delete

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string UserEmail { get; set; } = string.Empty;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? ChangedProperties { get; set; }

    public DateTime ChangeTime { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    public int Version { get; set; } = 1;

    [MaxLength(50)]
    public string? ParentEntityType { get; set; }

    [MaxLength(50)]
    public string? ParentEntityId { get; set; }

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
}