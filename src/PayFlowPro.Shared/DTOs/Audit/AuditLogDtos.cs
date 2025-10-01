using PayFlowPro.Models.Enums;

namespace PayFlowPro.Shared.DTOs.Audit;

public class AuditLogDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Description { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CorrelationId { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CreateAuditLogDto
{
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Description { get; set; }
    public AuditSeverity Severity { get; set; } = AuditSeverity.Info;
    public AuditCategory Category { get; set; } = AuditCategory.General;
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
}

public class AuditLogFilterDto
{
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public AuditSeverity? Severity { get; set; }
    public AuditCategory? Category { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? IpAddress { get; set; }
    public bool? IsSuccess { get; set; }
    public string? CorrelationId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class AuditLogSummaryDto
{
    public int TotalLogs { get; set; }
    public int TodayLogs { get; set; }
    public int ErrorLogs { get; set; }
    public int WarningLogs { get; set; }
    public int UniqueUsers { get; set; }
    public DateTime? LastActivity { get; set; }
    public List<AuditCategoryCountDto> CategoryCounts { get; set; } = new();
    public List<AuditActionCountDto> ActionCounts { get; set; } = new();
}

public class AuditCategoryCountDto
{
    public AuditCategory Category { get; set; }
    public int Count { get; set; }
}

public class AuditActionCountDto
{
    public string Action { get; set; } = string.Empty;
    public int Count { get; set; }
}