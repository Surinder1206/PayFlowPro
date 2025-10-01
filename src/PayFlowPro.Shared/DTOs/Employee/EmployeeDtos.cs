using System.ComponentModel.DataAnnotations;
using PayFlowPro.Models.Enums;

namespace PayFlowPro.Shared.DTOs.Employee;

public class CreateEmployeeDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Phone, MaxLength(15)]
    public string? PhoneNumber { get; set; }
    
    [Required]
    public DateTime DateOfBirth { get; set; }
    
    [Required]
    public DateTime DateOfJoining { get; set; } = DateTime.Today;
    
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
    
    [Required]
    public Gender Gender { get; set; }
    
    [Required]
    public MaritalStatus MaritalStatus { get; set; }
    
    [Required, MaxLength(100)]
    public string JobTitle { get; set; } = string.Empty;
    
    [Required, Range(0, double.MaxValue, ErrorMessage = "Basic salary must be a positive value")]
    public decimal BasicSalary { get; set; }
    
    [Required]
    public int CompanyId { get; set; }
    
    [Required]
    public int DepartmentId { get; set; }
    
    // User account creation
    public bool CreateUserAccount { get; set; } = true;
    
    [MaxLength(50)]
    public string? UserRole { get; set; } = "Employee";
    
    public string? TemporaryPassword { get; set; }
}

public class UpdateEmployeeDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Phone, MaxLength(15)]
    public string? PhoneNumber { get; set; }
    
    [Required]
    public DateTime DateOfBirth { get; set; }
    
    [Required]
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
    
    [Required]
    public Gender Gender { get; set; }
    
    [Required]
    public MaritalStatus MaritalStatus { get; set; }
    
    [Required]
    public EmploymentStatus Status { get; set; }
    
    [Required, MaxLength(100)]
    public string JobTitle { get; set; } = string.Empty;
    
    [Required, Range(0, double.MaxValue, ErrorMessage = "Basic salary must be a positive value")]
    public decimal BasicSalary { get; set; }
    
    [Required]
    public int CompanyId { get; set; }
    
    [Required]
    public int DepartmentId { get; set; }
}

public class EmployeeListDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public EmploymentStatus Status { get; set; }
    public decimal BasicSalary { get; set; }
    public DateTime DateOfJoining { get; set; }
    public DateTime? DateOfLeaving { get; set; }
}

public class EmployeeDetailDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime DateOfJoining { get; set; }
    public DateTime? DateOfLeaving { get; set; }
    public string? Address { get; set; }
    public string? NationalId { get; set; }
    public string? TaxId { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
    public Gender Gender { get; set; }
    public MaritalStatus MaritalStatus { get; set; }
    public EmploymentStatus Status { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public decimal BasicSalary { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public bool HasUserAccount { get; set; }
    public string? UserRole { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}