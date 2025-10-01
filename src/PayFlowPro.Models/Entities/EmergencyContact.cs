using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayFlowPro.Models.Entities;

/// <summary>
/// Emergency contact information for employees
/// </summary>
public class EmergencyContact
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Relationship { get; set; } = string.Empty;

    [Required]
    [MaxLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;
}

/// <summary>
/// Profile change requests for restricted fields
/// </summary>
public class ProfileChangeRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    public string CurrentValue { get; set; } = string.Empty;

    [Required]
    public string RequestedValue { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Reason { get; set; }

    [Required]
    [MaxLength(450)]
    public string RequestedBy { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; }

    [MaxLength(450)]
    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(500)]
    public string? ReviewComments { get; set; }

    public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public ApplicationUser RequestedByUser { get; set; } = null!;
    public ApplicationUser? ReviewedByUser { get; set; }
}

/// <summary>
/// Status of profile change requests
/// </summary>
public enum ChangeRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}