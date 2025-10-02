using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Models.Entities;

public class SystemSetting
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Value { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = "General";

    [Required]
    [StringLength(20)]
    public string DataType { get; set; } = "String"; // String, Number, Boolean, JSON

    public bool IsEncrypted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}