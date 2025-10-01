using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayFlowPro.Models.Entities;

/// <summary>
/// Entity representing salary history entries for employees
/// </summary>
public class SalaryHistory
{
    public int Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PreviousSalary { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal NewSalary { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalaryIncrease { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal IncreasePercentage { get; set; }

    [Required]
    public DateTime EffectiveDate { get; set; }

    [Required]
    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MaxLength(100)]
    public string ApprovedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("EmployeeId")]
    public virtual Employee Employee { get; set; } = null!;
}