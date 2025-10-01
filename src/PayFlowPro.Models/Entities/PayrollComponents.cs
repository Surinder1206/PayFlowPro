namespace PayFlowPro.Models.Entities;

public class EmployeeAllowance
{
    public int Id { get; set; }
    
    public int EmployeeId { get; set; }
    
    public int AllowanceTypeId { get; set; }
    
    public decimal Amount { get; set; }
    
    public decimal? Percentage { get; set; }
    
    public DateTime EffectiveFrom { get; set; }
    
    public DateTime? EffectiveTo { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public AllowanceType AllowanceType { get; set; } = null!;
}

public class EmployeeDeduction
{
    public int Id { get; set; }
    
    public int EmployeeId { get; set; }
    
    public int DeductionTypeId { get; set; }
    
    public decimal Amount { get; set; }
    
    public decimal? Percentage { get; set; }
    
    public DateTime EffectiveFrom { get; set; }
    
    public DateTime? EffectiveTo { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public DeductionType DeductionType { get; set; } = null!;
}

public class PayslipAllowance
{
    public int Id { get; set; }
    
    public int PayslipId { get; set; }
    
    public int AllowanceTypeId { get; set; }
    
    public decimal Amount { get; set; }
    
    public bool IsTaxable { get; set; }
    
    // Navigation properties
    public Payslip Payslip { get; set; } = null!;
    public AllowanceType AllowanceType { get; set; } = null!;
}

public class PayslipDeduction
{
    public int Id { get; set; }
    
    public int PayslipId { get; set; }
    
    public int DeductionTypeId { get; set; }
    
    public decimal Amount { get; set; }
    
    // Navigation properties
    public Payslip Payslip { get; set; } = null!;
    public DeductionType DeductionType { get; set; } = null!;
}