using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Models.Entities;

public class Company
{
    public int Id { get; set; }
    
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Address { get; set; }
    
    [MaxLength(15)]
    public string? PhoneNumber { get; set; }
    
    [MaxLength(100)]
    public string? Email { get; set; }
    
    [MaxLength(50)]
    public string? TaxId { get; set; }
    
    [MaxLength(50)]
    public string? RegistrationNumber { get; set; }
    
    public string? LogoUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}