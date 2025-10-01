using System.ComponentModel.DataAnnotations;
using PayFlowPro.Models.Enums;

namespace PayFlowPro.Models.Entities;

public class Payslip
{
    public int Id { get; set; }
    
    [Required, MaxLength(50)]
    public string PayslipNumber { get; set; } = string.Empty;
    
    public int EmployeeId { get; set; }
    
    public DateTime PayPeriodStart { get; set; }
    
    public DateTime PayPeriodEnd { get; set; }
    
    public DateTime PayDate { get; set; }
    
    public decimal BasicSalary { get; set; }
    
    public decimal GrossSalary { get; set; }
    
    public decimal TotalAllowances { get; set; }
    
    public decimal TotalDeductions { get; set; }
    
    public decimal NetSalary { get; set; }
    
    public decimal TaxAmount { get; set; }
    
    public int WorkingDays { get; set; }
    
    public int ActualWorkingDays { get; set; }
    
    public PayslipStatus Status { get; set; } = PayslipStatus.Draft;
    
    public string? Notes { get; set; }
    
    public string? GeneratedBy { get; set; }
    
    public DateTime? ApprovedAt { get; set; }
    
    public string? ApprovedBy { get; set; }
    
    public DateTime? EmailSentAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public ICollection<PayslipAllowance> PayslipAllowances { get; set; } = new List<PayslipAllowance>();
    public ICollection<PayslipDeduction> PayslipDeductions { get; set; } = new List<PayslipDeduction>();
}