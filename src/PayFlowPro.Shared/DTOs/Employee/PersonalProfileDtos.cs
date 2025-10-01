using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Shared.DTOs.Employee;

/// <summary>
/// DTO for employee personal profile information
/// </summary>
public class PersonalProfileDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? NationalId { get; set; }
    public string? TaxId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime DateOfJoining { get; set; }
    public string? ProfileImageUrl { get; set; }
    public decimal BasicSalary { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public List<EmergencyContactDto> EmergencyContacts { get; set; } = new();
}

/// <summary>
/// DTO for updating employee personal information
/// </summary>
public class UpdatePersonalProfileDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Phone]
    [MaxLength(15)]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(10)]
    public string? Gender { get; set; }

    [MaxLength(20)]
    public string? MaritalStatus { get; set; }

    [MaxLength(100)]
    public string? BankName { get; set; }

    [MaxLength(50)]
    public string? BankAccountNumber { get; set; }

    public List<EmergencyContactDto> EmergencyContacts { get; set; } = new();
}

/// <summary>
/// DTO for emergency contact information
/// </summary>
public class EmergencyContactDto
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Relationship { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public bool IsPrimary { get; set; }
}

/// <summary>
/// DTO for requesting changes to restricted fields
/// </summary>
public class ProfileChangeRequestDto
{
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
}

/// <summary>
/// DTO for profile update response
/// </summary>
public class ProfileUpdateResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> UpdatedFields { get; set; } = new();
    public List<string> PendingApprovalFields { get; set; } = new();
}