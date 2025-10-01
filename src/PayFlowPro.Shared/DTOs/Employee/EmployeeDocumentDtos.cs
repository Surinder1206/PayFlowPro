using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Shared.DTOs.Employee;

/// <summary>
/// DTO for employee document information
/// </summary>
public class EmployeeDocumentDto
{
    public int Id { get; set; }
    
    public int EmployeeId { get; set; }
    
    public string DocumentName { get; set; } = string.Empty;
    
    public string Category { get; set; } = string.Empty;
    
    public string FileName { get; set; } = string.Empty;
    
    public string FilePath { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public string ContentType { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty; // Pending, Approved, Rejected
    
    public string? Description { get; set; }
    
    public DateTime UploadedAt { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
    
    public DateTime? ReviewedAt { get; set; }
    
    public string? ReviewedBy { get; set; }
    
    public string? ReviewComments { get; set; }
    
    public bool IsRequired { get; set; }
    
    public bool IsExpiring { get; set; }
    
    // Metadata
    public string UploadedByName { get; set; } = string.Empty;
    public string? ReviewedByName { get; set; }
}

/// <summary>
/// DTO for uploading a new employee document
/// </summary>
public class UploadEmployeeDocumentDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    [MaxLength(200)]
    public string DocumentName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    public long FileSize { get; set; }

    [Required]
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// DTO for updating employee document information
/// </summary>
public class UpdateEmployeeDocumentDto
{
    [Required]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string DocumentName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// DTO for document review/approval
/// </summary>
public class ReviewDocumentDto
{
    [Required]
    public int DocumentId { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty; // Approved, Rejected

    [MaxLength(500)]
    public string? Comments { get; set; }

    [Required]
    public string ReviewedBy { get; set; } = string.Empty;
}

/// <summary>
/// DTO for document categories with count
/// </summary>
public class DocumentCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public int PendingCount { get; set; }
    public int ExpiringCount { get; set; }
}

/// <summary>
/// DTO for document statistics
/// </summary>
public class DocumentStatisticsDto
{
    public int TotalDocuments { get; set; }
    public int PendingReview { get; set; }
    public int ApprovedDocuments { get; set; }
    public int RejectedDocuments { get; set; }
    public int ExpiringDocuments { get; set; }
    public long TotalFileSize { get; set; }
    public DateTime LastUploadDate { get; set; }
    public List<DocumentCategoryDto> Categories { get; set; } = new();
}