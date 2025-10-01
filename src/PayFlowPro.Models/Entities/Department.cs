using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Models.Entities;

public class Department
{
    public int Id { get; set; }
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(20)]
    public string? Code { get; set; }
    
    public int CompanyId { get; set; }
    
    public int? ManagerEmployeeId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Company Company { get; set; } = null!;
    public Employee? Manager { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}