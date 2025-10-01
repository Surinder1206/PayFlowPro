using PayFlowPro.Shared.DTOs.Audit;
using PayFlowPro.Models.Enums;

namespace PayFlowPro.Core.Interfaces;

public interface IAuditService
{
    // Audit Log Operations
    Task LogActivityAsync(CreateAuditLogDto auditLog);
    Task LogActivityAsync(string action, string entityType, string? entityId = null, 
        object? oldValues = null, object? newValues = null, string? description = null,
        AuditSeverity severity = AuditSeverity.Info, AuditCategory category = AuditCategory.General);
    
    Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterDto filter);
    Task<AuditLogDto?> GetAuditLogAsync(int id);
    Task<AuditLogSummaryDto> GetAuditSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
    
    // Security Event Operations  
    Task LogSecurityEventAsync(CreateSecurityEventDto securityEvent);
    Task LogSecurityEventAsync(SecurityEventType eventType, SecuritySeverity severity, 
        string title, string description, string? resource = null);
    
    Task<IEnumerable<SecurityEventDto>> GetSecurityEventsAsync(SecurityEventFilterDto filter);
    Task<SecurityEventDto?> GetSecurityEventAsync(int id);
    Task<SecurityDashboardDto> GetSecurityDashboardAsync();
    Task ResolveSecurityEventAsync(int id, string resolvedBy, string? resolutionNotes = null);
    
    // User Session Operations
    Task<UserSessionDto> CreateSessionAsync(CreateUserSessionDto session);
    Task UpdateSessionActivityAsync(string sessionId, DateTime? logoutTime = null);
    Task<IEnumerable<UserSessionDto>> GetUserSessionsAsync(UserSessionFilterDto filter);
    Task<UserSessionDto?> GetSessionAsync(string sessionId);
    Task BlockSessionAsync(string sessionId, string reason);
    Task<int> GetActiveSessionCountAsync();
    
    // Data Change Tracking
    Task LogDataChangeAsync(string entityType, string entityId, ChangeType changeType,
        object? oldValues = null, object? newValues = null, string? reason = null);
    
    Task<IEnumerable<DataChangeLogDto>> GetDataChangesAsync(DataChangeLogFilterDto filter);
    Task<IEnumerable<DataChangeLogDto>> GetEntityHistoryAsync(string entityType, string entityId);
    
    // Compliance & Reporting
    Task<ComplianceReportDto> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate, 
        string reportType = "Standard");
    Task<byte[]> ExportAuditLogsAsync(AuditLogFilterDto filter, string format = "CSV");
    Task<byte[]> ExportSecurityEventsAsync(SecurityEventFilterDto filter, string format = "CSV");
    
    // System Health & Monitoring
    Task<bool> ValidateDataIntegrityAsync();
    Task CleanupOldLogsAsync(DateTime cutoffDate);
    Task<Dictionary<string, object>> GetSystemHealthMetricsAsync();
}