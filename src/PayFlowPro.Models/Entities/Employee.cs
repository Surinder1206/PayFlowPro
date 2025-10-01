using System.ComponentModel.DataAnnotations;
using PayFlowPro.Models.Enums;

namespace PayFlowPro.Models.Entities;

public class Employee
{
    public int Id { get; set; }
    
    [Required, MaxLength(20)]
    public string EmployeeCode { get; set; } = string.Empty;
    
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    public string FullName => $"{FirstName} {LastName}".Trim();
    
    [MaxLength(100)]
    public string? Email { get; set; }
    
    [MaxLength(15)]
    public string? PhoneNumber { get; set; }
    
    public DateTime DateOfBirth { get; set; }
    
    public DateTime DateOfJoining { get; set; }
    
    public DateTime? DateOfLeaving { get; set; }
    
    [MaxLength(500)]
    public string? Address { get; set; }
    
    [MaxLength(50)]
    public string? NationalId { get; set; }
    
    [MaxLength(50)]
    public string? TaxId { get; set; }
    
    [MaxLength(50)]
    public string? BankAccountNumber { get; set; }
    
    [MaxLength(100)]
    public string? BankName { get; set; }
    
    public Gender Gender { get; set; }
    
    public MaritalStatus MaritalStatus { get; set; }
    
    public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;
    
    [MaxLength(100)]
    public string? JobTitle { get; set; }
    
    public decimal BasicSalary { get; set; }
    
    public string? ProfileImageUrl { get; set; }
    
    public int CompanyId { get; set; }
    
    public int DepartmentId { get; set; }
    
    public string? UserId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Company Company { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public ApplicationUser? User { get; set; }
    public ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();
    public ICollection<EmployeeAllowance> EmployeeAllowances { get; set; } = new List<EmployeeAllowance>();
    public ICollection<EmployeeDeduction> EmployeeDeductions { get; set; } = new List<EmployeeDeduction>();
    public ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
    public ICollection<ProfileChangeRequest> ProfileChangeRequests { get; set; } = new List<ProfileChangeRequest>();
}