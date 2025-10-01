using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Models.Entities;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string? FirstName { get; set; }
    
    [MaxLength(100)]
    public string? LastName { get; set; }
    
    public string FullName => $"{FirstName} {LastName}".Trim();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Employee? Employee { get; set; }
}