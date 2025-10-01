using System.ComponentModel.DataAnnotations;
using PayFlowPro.Models.Enums;

namespace PayFlowPro.Models.Entities;

public class AllowanceType
{
    public int Id { get; set; }
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? Code { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public Enums.AllowanceType Type { get; set; }
    
    public decimal? DefaultAmount { get; set; }
    
    public decimal? DefaultPercentage { get; set; }
    
    public bool IsTaxable { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<EmployeeAllowance> EmployeeAllowances { get; set; } = new List<EmployeeAllowance>();
    public ICollection<PayslipAllowance> PayslipAllowances { get; set; } = new List<PayslipAllowance>();
}

public class DeductionType
{
    public int Id { get; set; }
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? Code { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public Enums.DeductionType Type { get; set; }
    
    public decimal? DefaultAmount { get; set; }
    
    public decimal? DefaultPercentage { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<EmployeeDeduction> EmployeeDeductions { get; set; } = new List<EmployeeDeduction>();
    public ICollection<PayslipDeduction> PayslipDeductions { get; set; } = new List<PayslipDeduction>();
}