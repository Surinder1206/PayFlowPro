using PayFlowPro.Shared.DTOs.Audit;

namespace PayFlowPro.Core.Interfaces;

public interface IDataChangeTracker
{
    Task<bool> TrackEntityChangeAsync<T>(T entity, string changeType, string? reason = null) where T : class;
    Task<bool> TrackEntityChangesAsync<T>(IEnumerable<T> entities, string changeType, string? reason = null) where T : class;
    Task<IEnumerable<DataChangeLogDto>> GetEntityHistoryAsync(string entityType, string entityId);
    Task<IEnumerable<DataChangeLogDto>> GetUserChangesAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    void EnableTracking();
    void DisableTracking();
    bool IsTrackingEnabled { get; }
}

public interface ISecurityMonitor
{
    Task MonitorLoginAttemptAsync(string userId, string ipAddress, bool isSuccessful, string? failureReason = null);
    Task MonitorSuspiciousActivityAsync(string? userId, string activity, string ipAddress, string? details = null);
    Task MonitorDataAccessAsync(string userId, string entityType, string entityId, string accessType);
    Task MonitorPermissionChangeAsync(string userId, string targetUserId, string permission, string action);
    Task<bool> IsIpAddressBlockedAsync(string ipAddress);
    Task BlockIpAddressAsync(string ipAddress, string reason, TimeSpan? duration = null);
    Task<int> GetFailedLoginAttemptsAsync(string userId, TimeSpan timeWindow);
    Task<bool> ShouldLockAccountAsync(string userId);
}

public interface IComplianceService
{
    Task<ComplianceReportDto> GenerateGDPRReportAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<ComplianceReportDto> GenerateSOXReportAsync(DateTime startDate, DateTime endDate);
    Task<ComplianceReportDto> GenerateCustomComplianceReportAsync(string reportType, DateTime startDate, DateTime endDate, Dictionary<string, object>? parameters = null);
    Task<bool> ValidateDataRetentionPolicyAsync();
    Task<int> ApplyDataRetentionPolicyAsync(DateTime cutoffDate);
    Task<byte[]> ExportUserDataAsync(string userId, string format = "JSON");
    Task<bool> DeleteUserDataAsync(string userId, string reason);
    Task<IEnumerable<string>> GetDataProcessingActivitiesAsync(string userId);
}