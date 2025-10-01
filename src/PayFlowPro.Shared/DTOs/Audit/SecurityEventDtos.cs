using PayFlowPro.Models.Enums;

namespace PayFlowPro.Shared.DTOs.Audit;

public class SecurityEventDto
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? Resource { get; set; }
    public string? HttpMethod { get; set; }
    public string? RequestUrl { get; set; }
    public int? ResponseCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CorrelationId { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class CreateSecurityEventDto
{
    public SecurityEventType EventType { get; set; }
    public SecuritySeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? Resource { get; set; }
    public string? HttpMethod { get; set; }
    public string? RequestUrl { get; set; }
    public int? ResponseCode { get; set; }
    public string? CorrelationId { get; set; }
    public string? AdditionalData { get; set; }
}

public class SecurityEventFilterDto
{
    public SecurityEventType? EventType { get; set; }
    public SecuritySeverity? Severity { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsResolved { get; set; }
    public string? Resource { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class SecurityDashboardDto
{
    public int TotalEvents { get; set; }
    public int TodayEvents { get; set; }
    public int CriticalEvents { get; set; }
    public int UnresolvedEvents { get; set; }
    public int FailedLoginAttempts { get; set; }
    public int ActiveSessions { get; set; }
    public DateTime? LastSecurityEvent { get; set; }
    public List<SecurityEventTypeCountDto> EventTypeCounts { get; set; } = new();
    public List<SecurityTrendDto> DailyTrends { get; set; } = new();
    public List<TopThreatDto> TopThreats { get; set; } = new();
}

public class SecurityEventTypeCountDto
{
    public SecurityEventType EventType { get; set; }
    public int Count { get; set; }
}

public class SecurityTrendDto
{
    public DateTime Date { get; set; }
    public int EventCount { get; set; }
    public int CriticalCount { get; set; }
}

public class TopThreatDto
{
    public string IpAddress { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public SecuritySeverity MaxSeverity { get; set; }
    public DateTime LastSeen { get; set; }
}