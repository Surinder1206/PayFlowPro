using PayFlowPro.Models.Enums;

namespace PayFlowPro.Shared.DTOs.Audit;

public class UserSessionDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public string? DeviceType { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime? LogoutTime { get; set; }
    public DateTime LastActivity { get; set; }
    public bool IsActive { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public int LoginAttempts { get; set; }
    public DateTime? LastLoginAttempt { get; set; }
    public string UserEmail { get; set; } = string.Empty;
}

public class CreateUserSessionDto
{
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public string? DeviceType { get; set; }
}

public class UserSessionFilterDto
{
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsBlocked { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? DeviceType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "LoginTime";
    public bool SortDescending { get; set; } = true;
}

public class DataChangeLogDto
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedProperties { get; set; }
    public DateTime ChangeTime { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? CorrelationId { get; set; }
    public int Version { get; set; }
    public string? ParentEntityType { get; set; }
    public string? ParentEntityId { get; set; }
}

public class DataChangeLogFilterDto
{
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public ChangeType? ChangeType { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ParentEntityType { get; set; }
    public string? ParentEntityId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "ChangeTime";
    public bool SortDescending { get; set; } = true;
}

public class ComplianceReportDto
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalDataChanges { get; set; }
    public int TotalUserSessions { get; set; }
    public int TotalSecurityEvents { get; set; }
    public int UniqueUsers { get; set; }
    public List<ComplianceDataAccessDto> DataAccess { get; set; } = new();
    public List<ComplianceUserActivityDto> UserActivities { get; set; } = new();
    public List<ComplianceSecurityIncidentDto> SecurityIncidents { get; set; } = new();
}

public class ComplianceDataAccessDto
{
    public string EntityType { get; set; } = string.Empty;
    public int AccessCount { get; set; }
    public int ModificationCount { get; set; }
    public List<string> AccessingUsers { get; set; } = new();
}

public class ComplianceUserActivityDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int LoginCount { get; set; }
    public int DataModifications { get; set; }
    public DateTime? LastActivity { get; set; }
}

public class ComplianceSecurityIncidentDto
{
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime? LastOccurrence { get; set; }
    public bool IsResolved { get; set; }
}