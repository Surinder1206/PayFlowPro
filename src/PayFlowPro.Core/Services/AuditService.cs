using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PayFlowPro.Core.Interfaces;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.Entities;
using PayFlowPro.Models.Enums;
using PayFlowPro.Shared.DTOs.Audit;
using System.Security.Claims;
using System.Text.Json;

namespace PayFlowPro.Core.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    #region Audit Log Operations

    public async Task LogActivityAsync(CreateAuditLogDto auditLog)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var currentUser = httpContext?.User;
        
        var entity = new AuditLog
        {
            UserId = currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System",
            UserEmail = currentUser?.FindFirst(ClaimTypes.Email)?.Value ?? "system@payslip.com",
            Action = auditLog.Action,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            OldValues = auditLog.OldValues,
            NewValues = auditLog.NewValues,
            Description = auditLog.Description,
            IpAddress = GetClientIpAddress(),
            UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
            Severity = auditLog.Severity.ToString(),
            Category = auditLog.Category.ToString(),
            CreatedAt = DateTime.UtcNow,
            CorrelationId = auditLog.CorrelationId ?? Guid.NewGuid().ToString(),
            IsSuccess = auditLog.IsSuccess,
            ErrorMessage = auditLog.ErrorMessage
        };

        _context.AuditLogs.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task LogActivityAsync(string action, string entityType, string? entityId = null,
        object? oldValues = null, object? newValues = null, string? description = null,
        AuditSeverity severity = AuditSeverity.Info, AuditCategory category = AuditCategory.General)
    {
        var auditLog = new CreateAuditLogDto
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            Description = description,
            Severity = severity,
            Category = category,
            IsSuccess = true
        };

        await LogActivityAsync(auditLog);
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterDto filter)
    {
        var query = _context.AuditLogs
            .Include(al => al.User)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.UserId))
            query = query.Where(al => al.UserId == filter.UserId);

        if (!string.IsNullOrEmpty(filter.UserEmail))
            query = query.Where(al => al.UserEmail.Contains(filter.UserEmail));

        if (!string.IsNullOrEmpty(filter.Action))
            query = query.Where(al => al.Action.Contains(filter.Action));

        if (!string.IsNullOrEmpty(filter.EntityType))
            query = query.Where(al => al.EntityType == filter.EntityType);

        if (!string.IsNullOrEmpty(filter.EntityId))
            query = query.Where(al => al.EntityId == filter.EntityId);

        if (filter.Severity.HasValue)
            query = query.Where(al => al.Severity == filter.Severity.ToString());

        if (filter.Category.HasValue)
            query = query.Where(al => al.Category == filter.Category.ToString());

        if (filter.StartDate.HasValue)
            query = query.Where(al => al.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(al => al.CreatedAt <= filter.EndDate.Value);

        if (!string.IsNullOrEmpty(filter.IpAddress))
            query = query.Where(al => al.IpAddress == filter.IpAddress);

        if (filter.IsSuccess.HasValue)
            query = query.Where(al => al.IsSuccess == filter.IsSuccess.Value);

        if (!string.IsNullOrEmpty(filter.CorrelationId))
            query = query.Where(al => al.CorrelationId == filter.CorrelationId);

        // Apply sorting
        query = filter.SortDescending
            ? query.OrderByDescending(al => EF.Property<object>(al, filter.SortBy))
            : query.OrderBy(al => EF.Property<object>(al, filter.SortBy));

        // Apply pagination
        var auditLogs = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return auditLogs.Select(MapToAuditLogDto);
    }

    public async Task<AuditLogDto?> GetAuditLogAsync(int id)
    {
        var auditLog = await _context.AuditLogs
            .Include(al => al.User)
            .FirstOrDefaultAsync(al => al.Id == id);

        return auditLog != null ? MapToAuditLogDto(auditLog) : null;
    }

    public async Task<AuditLogSummaryDto> GetAuditSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.Date.AddDays(-30);
        endDate ??= DateTime.UtcNow.Date.AddDays(1);

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var summary = new AuditLogSummaryDto
        {
            TotalLogs = await _context.AuditLogs
                .Where(al => al.CreatedAt >= startDate && al.CreatedAt <= endDate)
                .CountAsync(),
                
            TodayLogs = await _context.AuditLogs
                .Where(al => al.CreatedAt >= today && al.CreatedAt < tomorrow)
                .CountAsync(),
                
            ErrorLogs = await _context.AuditLogs
                .Where(al => al.CreatedAt >= startDate && al.CreatedAt <= endDate && 
                           (al.Severity == "Error" || al.Severity == "Critical"))
                .CountAsync(),
                
            WarningLogs = await _context.AuditLogs
                .Where(al => al.CreatedAt >= startDate && al.CreatedAt <= endDate && al.Severity == "Warning")
                .CountAsync(),
                
            UniqueUsers = await _context.AuditLogs
                .Where(al => al.CreatedAt >= startDate && al.CreatedAt <= endDate)
                .Select(al => al.UserId)
                .Distinct()
                .CountAsync(),
                
            LastActivity = await _context.AuditLogs
                .OrderByDescending(al => al.CreatedAt)
                .Select(al => al.CreatedAt)
                .FirstOrDefaultAsync()
        };

        // Get category counts
        var categoryCounts = await _context.AuditLogs
            .Where(al => al.CreatedAt >= startDate && al.CreatedAt <= endDate)
            .GroupBy(al => al.Category)
            .Select(g => new AuditCategoryCountDto
            {
                Category = Enum.Parse<AuditCategory>(g.Key),
                Count = g.Count()
            })
            .ToListAsync();

        summary.CategoryCounts = categoryCounts;

        // Get action counts
        var actionCounts = await _context.AuditLogs
            .Where(al => al.CreatedAt >= startDate && al.CreatedAt <= endDate)
            .GroupBy(al => al.Action)
            .Select(g => new AuditActionCountDto
            {
                Action = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(g => g.Count)
            .Take(10)
            .ToListAsync();

        summary.ActionCounts = actionCounts;

        return summary;
    }

    #endregion

    #region Security Event Operations

    public async Task LogSecurityEventAsync(CreateSecurityEventDto securityEvent)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var currentUser = httpContext?.User;

        var entity = new SecurityEvent
        {
            EventType = securityEvent.EventType.ToString(),
            Severity = securityEvent.Severity.ToString(),
            Title = securityEvent.Title,
            Description = securityEvent.Description,
            UserId = securityEvent.UserId ?? currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            UserEmail = currentUser?.FindFirst(ClaimTypes.Email)?.Value,
            IpAddress = GetClientIpAddress(),
            UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
            Resource = securityEvent.Resource,
            HttpMethod = securityEvent.HttpMethod,
            RequestUrl = securityEvent.RequestUrl,
            ResponseCode = securityEvent.ResponseCode,
            CreatedAt = DateTime.UtcNow,
            CorrelationId = securityEvent.CorrelationId ?? Guid.NewGuid().ToString(),
            AdditionalData = securityEvent.AdditionalData
        };

        _context.SecurityEvents.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task LogSecurityEventAsync(SecurityEventType eventType, SecuritySeverity severity,
        string title, string description, string? resource = null)
    {
        var securityEvent = new CreateSecurityEventDto
        {
            EventType = eventType,
            Severity = severity,
            Title = title,
            Description = description,
            Resource = resource
        };

        await LogSecurityEventAsync(securityEvent);
    }

    public async Task<IEnumerable<SecurityEventDto>> GetSecurityEventsAsync(SecurityEventFilterDto filter)
    {
        var query = _context.SecurityEvents
            .Include(se => se.User)
            .AsQueryable();

        // Apply filters
        if (filter.EventType.HasValue)
            query = query.Where(se => se.EventType == filter.EventType.ToString());

        if (filter.Severity.HasValue)
            query = query.Where(se => se.Severity == filter.Severity.ToString());

        if (!string.IsNullOrEmpty(filter.UserId))
            query = query.Where(se => se.UserId == filter.UserId);

        if (!string.IsNullOrEmpty(filter.UserEmail))
            query = query.Where(se => se.UserEmail != null && se.UserEmail.Contains(filter.UserEmail));

        if (!string.IsNullOrEmpty(filter.IpAddress))
            query = query.Where(se => se.IpAddress == filter.IpAddress);

        if (filter.StartDate.HasValue)
            query = query.Where(se => se.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(se => se.CreatedAt <= filter.EndDate.Value);

        if (filter.IsResolved.HasValue)
            query = query.Where(se => se.IsResolved == filter.IsResolved.Value);

        if (!string.IsNullOrEmpty(filter.Resource))
            query = query.Where(se => se.Resource != null && se.Resource.Contains(filter.Resource));

        // Apply sorting
        query = filter.SortDescending
            ? query.OrderByDescending(se => EF.Property<object>(se, filter.SortBy))
            : query.OrderBy(se => EF.Property<object>(se, filter.SortBy));

        // Apply pagination
        var events = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return events.Select(MapToSecurityEventDto);
    }

    public async Task<SecurityEventDto?> GetSecurityEventAsync(int id)
    {
        var securityEvent = await _context.SecurityEvents
            .Include(se => se.User)
            .FirstOrDefaultAsync(se => se.Id == id);

        return securityEvent != null ? MapToSecurityEventDto(securityEvent) : null;
    }

    public async Task<SecurityDashboardDto> GetSecurityDashboardAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var lastWeek = today.AddDays(-7);

        var dashboard = new SecurityDashboardDto
        {
            TotalEvents = await _context.SecurityEvents.CountAsync(),
            
            TodayEvents = await _context.SecurityEvents
                .Where(se => se.CreatedAt >= today && se.CreatedAt < tomorrow)
                .CountAsync(),
                
            CriticalEvents = await _context.SecurityEvents
                .Where(se => se.Severity == "Critical")
                .CountAsync(),
                
            UnresolvedEvents = await _context.SecurityEvents
                .Where(se => !se.IsResolved)
                .CountAsync(),
                
            FailedLoginAttempts = await _context.SecurityEvents
                .Where(se => se.EventType == "LoginFailure" && se.CreatedAt >= today && se.CreatedAt < tomorrow)
                .CountAsync(),
                
            ActiveSessions = await _context.UserSessions
                .Where(us => us.IsActive && !us.IsBlocked)
                .CountAsync(),
                
            LastSecurityEvent = await _context.SecurityEvents
                .OrderByDescending(se => se.CreatedAt)
                .Select(se => se.CreatedAt)
                .FirstOrDefaultAsync()
        };

        // Get event type counts
        var eventTypeCounts = await _context.SecurityEvents
            .Where(se => se.CreatedAt >= lastWeek)
            .GroupBy(se => se.EventType)
            .Select(g => new SecurityEventTypeCountDto
            {
                EventType = Enum.Parse<SecurityEventType>(g.Key),
                Count = g.Count()
            })
            .ToListAsync();

        dashboard.EventTypeCounts = eventTypeCounts;

        // Get daily trends
        var dailyTrends = await _context.SecurityEvents
            .Where(se => se.CreatedAt >= lastWeek)
            .GroupBy(se => se.CreatedAt.Date)
            .Select(g => new SecurityTrendDto
            {
                Date = g.Key,
                EventCount = g.Count(),
                CriticalCount = g.Count(se => se.Severity == "Critical")
            })
            .OrderBy(g => g.Date)
            .ToListAsync();

        dashboard.DailyTrends = dailyTrends;

        // Get top threats by IP address
        var topThreats = await _context.SecurityEvents
            .Where(se => se.CreatedAt >= lastWeek && se.Severity != "Info")
            .GroupBy(se => se.IpAddress)
            .Select(g => new TopThreatDto
            {
                IpAddress = g.Key,
                EventCount = g.Count(),
                MaxSeverity = g.Max(se => se.Severity == "Critical" ? SecuritySeverity.Critical :
                                        se.Severity == "High" ? SecuritySeverity.High :
                                        se.Severity == "Medium" ? SecuritySeverity.Medium : SecuritySeverity.Low),
                LastSeen = g.Max(se => se.CreatedAt)
            })
            .OrderByDescending(g => g.EventCount)
            .Take(10)
            .ToListAsync();

        dashboard.TopThreats = topThreats;

        return dashboard;
    }

    public async Task ResolveSecurityEventAsync(int id, string resolvedBy, string? resolutionNotes = null)
    {
        var securityEvent = await _context.SecurityEvents.FindAsync(id);
        if (securityEvent != null)
        {
            securityEvent.IsResolved = true;
            securityEvent.ResolvedAt = DateTime.UtcNow;
            securityEvent.ResolvedBy = resolvedBy;
            securityEvent.ResolutionNotes = resolutionNotes;

            await _context.SaveChangesAsync();

            // Log the resolution
            await LogActivityAsync("SecurityEventResolved", "SecurityEvent", id.ToString(),
                null, new { ResolvedBy = resolvedBy, ResolutionNotes = resolutionNotes },
                "Security event resolved", AuditSeverity.Info, AuditCategory.Security);
        }
    }

    #endregion

    #region User Session Operations

    public async Task<UserSessionDto> CreateSessionAsync(CreateUserSessionDto session)
    {
        var entity = new UserSession
        {
            UserId = session.UserId,
            SessionId = session.SessionId,
            IpAddress = session.IpAddress,
            UserAgent = session.UserAgent,
            Location = session.Location,
            DeviceType = session.DeviceType,
            LoginTime = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            IsActive = true
        };

        _context.UserSessions.Add(entity);
        await _context.SaveChangesAsync();

        // Log the session creation
        await LogActivityAsync("SessionCreated", "UserSession", entity.SessionId,
            null, entity, "User session created", AuditSeverity.Info, AuditCategory.Authentication);

        return MapToUserSessionDto(entity);
    }

    public async Task UpdateSessionActivityAsync(string sessionId, DateTime? logoutTime = null)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(us => us.SessionId == sessionId);

        if (session != null)
        {
            session.LastActivity = DateTime.UtcNow;
            
            if (logoutTime.HasValue)
            {
                session.LogoutTime = logoutTime.Value;
                session.IsActive = false;

                // Log the logout
                await LogActivityAsync("UserLogout", "UserSession", sessionId,
                    null, null, "User logged out", AuditSeverity.Info, AuditCategory.Authentication);
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<UserSessionDto>> GetUserSessionsAsync(UserSessionFilterDto filter)
    {
        var query = _context.UserSessions
            .Include(us => us.User)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.UserId))
            query = query.Where(us => us.UserId == filter.UserId);

        if (!string.IsNullOrEmpty(filter.UserEmail))
            query = query.Where(us => us.User.Email.Contains(filter.UserEmail));

        if (!string.IsNullOrEmpty(filter.IpAddress))
            query = query.Where(us => us.IpAddress == filter.IpAddress);

        if (filter.IsActive.HasValue)
            query = query.Where(us => us.IsActive == filter.IsActive.Value);

        if (filter.IsBlocked.HasValue)
            query = query.Where(us => us.IsBlocked == filter.IsBlocked.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(us => us.LoginTime >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(us => us.LoginTime <= filter.EndDate.Value);

        if (!string.IsNullOrEmpty(filter.DeviceType))
            query = query.Where(us => us.DeviceType != null && us.DeviceType.Contains(filter.DeviceType));

        // Apply sorting
        query = filter.SortDescending
            ? query.OrderByDescending(us => EF.Property<object>(us, filter.SortBy))
            : query.OrderBy(us => EF.Property<object>(us, filter.SortBy));

        // Apply pagination
        var sessions = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return sessions.Select(MapToUserSessionDto);
    }

    public async Task<UserSessionDto?> GetSessionAsync(string sessionId)
    {
        var session = await _context.UserSessions
            .Include(us => us.User)
            .FirstOrDefaultAsync(us => us.SessionId == sessionId);

        return session != null ? MapToUserSessionDto(session) : null;
    }

    public async Task BlockSessionAsync(string sessionId, string reason)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(us => us.SessionId == sessionId);

        if (session != null)
        {
            session.IsBlocked = true;
            session.BlockReason = reason;
            session.IsActive = false;

            await _context.SaveChangesAsync();

            // Log the session blocking
            await LogActivityAsync("SessionBlocked", "UserSession", sessionId,
                null, new { Reason = reason }, "User session blocked", AuditSeverity.Warning, AuditCategory.Security);
        }
    }

    public async Task<int> GetActiveSessionCountAsync()
    {
        return await _context.UserSessions
            .Where(us => us.IsActive && !us.IsBlocked)
            .CountAsync();
    }

    #endregion

    #region Data Change Tracking

    public async Task LogDataChangeAsync(string entityType, string entityId, ChangeType changeType,
        object? oldValues = null, object? newValues = null, string? reason = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var currentUser = httpContext?.User;

        var entity = new DataChangeLog
        {
            EntityType = entityType,
            EntityId = entityId,
            ChangeType = changeType.ToString(),
            UserId = currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System",
            UserEmail = currentUser?.FindFirst(ClaimTypes.Email)?.Value ?? "system@payslip.com",
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            ChangeTime = DateTime.UtcNow,
            IpAddress = GetClientIpAddress(),
            Reason = reason,
            CorrelationId = Guid.NewGuid().ToString(),
            Version = 1
        };

        // Calculate changed properties
        if (oldValues != null && newValues != null)
        {
            entity.ChangedProperties = CalculateChangedProperties(oldValues, newValues);
        }

        _context.DataChangeLogs.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<DataChangeLogDto>> GetDataChangesAsync(DataChangeLogFilterDto filter)
    {
        var query = _context.DataChangeLogs
            .Include(dcl => dcl.User)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.EntityType))
            query = query.Where(dcl => dcl.EntityType == filter.EntityType);

        if (!string.IsNullOrEmpty(filter.EntityId))
            query = query.Where(dcl => dcl.EntityId == filter.EntityId);

        if (filter.ChangeType.HasValue)
            query = query.Where(dcl => dcl.ChangeType == filter.ChangeType.ToString());

        if (!string.IsNullOrEmpty(filter.UserId))
            query = query.Where(dcl => dcl.UserId == filter.UserId);

        if (!string.IsNullOrEmpty(filter.UserEmail))
            query = query.Where(dcl => dcl.UserEmail.Contains(filter.UserEmail));

        if (filter.StartDate.HasValue)
            query = query.Where(dcl => dcl.ChangeTime >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(dcl => dcl.ChangeTime <= filter.EndDate.Value);

        if (!string.IsNullOrEmpty(filter.ParentEntityType))
            query = query.Where(dcl => dcl.ParentEntityType == filter.ParentEntityType);

        if (!string.IsNullOrEmpty(filter.ParentEntityId))
            query = query.Where(dcl => dcl.ParentEntityId == filter.ParentEntityId);

        // Apply sorting
        query = filter.SortDescending
            ? query.OrderByDescending(dcl => EF.Property<object>(dcl, filter.SortBy))
            : query.OrderBy(dcl => EF.Property<object>(dcl, filter.SortBy));

        // Apply pagination
        var changes = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return changes.Select(MapToDataChangeLogDto);
    }

    public async Task<IEnumerable<DataChangeLogDto>> GetEntityHistoryAsync(string entityType, string entityId)
    {
        var changes = await _context.DataChangeLogs
            .Include(dcl => dcl.User)
            .Where(dcl => dcl.EntityType == entityType && dcl.EntityId == entityId)
            .OrderByDescending(dcl => dcl.ChangeTime)
            .ToListAsync();

        return changes.Select(MapToDataChangeLogDto);
    }

    #endregion

    #region Compliance & Reporting

    public async Task<ComplianceReportDto> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate, 
        string reportType = "Standard")
    {
        var report = new ComplianceReportDto
        {
            ReportType = reportType,
            GeneratedAt = DateTime.UtcNow,
            PeriodStart = startDate,
            PeriodEnd = endDate
        };

        // Basic statistics
        report.TotalDataChanges = await _context.DataChangeLogs
            .Where(dcl => dcl.ChangeTime >= startDate && dcl.ChangeTime <= endDate)
            .CountAsync();

        report.TotalUserSessions = await _context.UserSessions
            .Where(us => us.LoginTime >= startDate && us.LoginTime <= endDate)
            .CountAsync();

        report.TotalSecurityEvents = await _context.SecurityEvents
            .Where(se => se.CreatedAt >= startDate && se.CreatedAt <= endDate)
            .CountAsync();

        report.UniqueUsers = await _context.AuditLogs
            .Where(al => al.CreatedAt >= startDate && al.CreatedAt <= endDate)
            .Select(al => al.UserId)
            .Distinct()
            .CountAsync();

        // Data access summary
        var dataAccess = await _context.DataChangeLogs
            .Where(dcl => dcl.ChangeTime >= startDate && dcl.ChangeTime <= endDate)
            .GroupBy(dcl => dcl.EntityType)
            .Select(g => new ComplianceDataAccessDto
            {
                EntityType = g.Key,
                AccessCount = g.Count(),
                ModificationCount = g.Count(dcl => dcl.ChangeType != "Read"),
                AccessingUsers = g.Select(dcl => dcl.UserId).Distinct().ToList()
            })
            .ToListAsync();

        report.DataAccess = dataAccess;

        // User activity summary
        var userActivities = await _context.AuditLogs
            .Where(al => al.CreatedAt >= startDate && al.CreatedAt <= endDate)
            .GroupBy(al => new { al.UserId, al.UserEmail })
            .Select(g => new ComplianceUserActivityDto
            {
                UserId = g.Key.UserId,
                UserEmail = g.Key.UserEmail,
                LoginCount = g.Count(al => al.Action == "Login"),
                DataModifications = g.Count(al => al.Action.Contains("Update") || al.Action.Contains("Create") || al.Action.Contains("Delete")),
                LastActivity = g.Max(al => al.CreatedAt)
            })
            .ToListAsync();

        report.UserActivities = userActivities;

        // Security incidents
        var securityIncidents = await _context.SecurityEvents
            .Where(se => se.CreatedAt >= startDate && se.CreatedAt <= endDate)
            .GroupBy(se => new { se.EventType, se.Severity })
            .Select(g => new ComplianceSecurityIncidentDto
            {
                EventType = g.Key.EventType,
                Severity = g.Key.Severity,
                Count = g.Count(),
                LastOccurrence = g.Max(se => se.CreatedAt),
                IsResolved = g.All(se => se.IsResolved)
            })
            .ToListAsync();

        report.SecurityIncidents = securityIncidents;

        return report;
    }

    public async Task<byte[]> ExportAuditLogsAsync(AuditLogFilterDto filter, string format = "CSV")
    {
        var auditLogs = await GetAuditLogsAsync(filter);
        
        if (format.ToUpper() == "CSV")
        {
            return ExportToCsv(auditLogs);
        }
        
        // Add other export formats as needed
        throw new NotImplementedException($"Export format '{format}' is not implemented yet.");
    }

    public async Task<byte[]> ExportSecurityEventsAsync(SecurityEventFilterDto filter, string format = "CSV")
    {
        var securityEvents = await GetSecurityEventsAsync(filter);
        
        if (format.ToUpper() == "CSV")
        {
            return ExportToCsv(securityEvents);
        }
        
        // Add other export formats as needed
        throw new NotImplementedException($"Export format '{format}' is not implemented yet.");
    }

    #endregion

    #region System Health & Monitoring

    public async Task<bool> ValidateDataIntegrityAsync()
    {
        try
        {
            // Check for orphaned audit logs
            var orphanedAuditLogs = await _context.AuditLogs
                .Where(al => !_context.Users.Any(u => u.Id == al.UserId))
                .CountAsync();

            // Check for duplicate session IDs
            var duplicateSessions = await _context.UserSessions
                .GroupBy(us => us.SessionId)
                .Where(g => g.Count() > 1)
                .CountAsync();

            // Log integrity check results
            await LogActivityAsync("DataIntegrityCheck", "System", null,
                null, new { OrphanedAuditLogs = orphanedAuditLogs, DuplicateSessions = duplicateSessions },
                $"Data integrity check completed. Orphaned logs: {orphanedAuditLogs}, Duplicate sessions: {duplicateSessions}",
                orphanedAuditLogs > 0 || duplicateSessions > 0 ? AuditSeverity.Warning : AuditSeverity.Info,
                AuditCategory.SystemOperation);

            return orphanedAuditLogs == 0 && duplicateSessions == 0;
        }
        catch (Exception ex)
        {
            await LogActivityAsync("DataIntegrityCheck", "System", null,
                null, null, $"Data integrity check failed: {ex.Message}",
                AuditSeverity.Error, AuditCategory.SystemOperation);
            
            return false;
        }
    }

    public async Task CleanupOldLogsAsync(DateTime cutoffDate)
    {
        var oldAuditLogs = await _context.AuditLogs
            .Where(al => al.CreatedAt < cutoffDate)
            .CountAsync();

        var oldSecurityEvents = await _context.SecurityEvents
            .Where(se => se.CreatedAt < cutoffDate && se.IsResolved)
            .CountAsync();

        var oldDataChangeLogs = await _context.DataChangeLogs
            .Where(dcl => dcl.ChangeTime < cutoffDate)
            .CountAsync();

        // Remove old records
        _context.AuditLogs.RemoveRange(
            _context.AuditLogs.Where(al => al.CreatedAt < cutoffDate));

        _context.SecurityEvents.RemoveRange(
            _context.SecurityEvents.Where(se => se.CreatedAt < cutoffDate && se.IsResolved));

        _context.DataChangeLogs.RemoveRange(
            _context.DataChangeLogs.Where(dcl => dcl.ChangeTime < cutoffDate));

        await _context.SaveChangesAsync();

        // Log the cleanup
        await LogActivityAsync("LogCleanup", "System", null,
            null, new { CutoffDate = cutoffDate, AuditLogs = oldAuditLogs, SecurityEvents = oldSecurityEvents, DataChangeLogs = oldDataChangeLogs },
            $"Cleaned up {oldAuditLogs + oldSecurityEvents + oldDataChangeLogs} old log entries",
            AuditSeverity.Info, AuditCategory.SystemOperation);
    }

    public async Task<Dictionary<string, object>> GetSystemHealthMetricsAsync()
    {
        var metrics = new Dictionary<string, object>();
        var now = DateTime.UtcNow;
        var today = now.Date;
        var thisMonth = new DateTime(now.Year, now.Month, 1);

        // Basic counts
        metrics["TotalAuditLogs"] = await _context.AuditLogs.CountAsync();
        metrics["TotalSecurityEvents"] = await _context.SecurityEvents.CountAsync();
        metrics["TotalUserSessions"] = await _context.UserSessions.CountAsync();
        metrics["TotalDataChangeLogs"] = await _context.DataChangeLogs.CountAsync();

        // Today's activity
        metrics["TodayAuditLogs"] = await _context.AuditLogs
            .Where(al => al.CreatedAt >= today)
            .CountAsync();

        metrics["TodaySecurityEvents"] = await _context.SecurityEvents
            .Where(se => se.CreatedAt >= today)
            .CountAsync();

        // Active sessions
        metrics["ActiveSessions"] = await _context.UserSessions
            .Where(us => us.IsActive && !us.IsBlocked)
            .CountAsync();

        // Error rates
        metrics["ErrorRate"] = await CalculateErrorRate(today, now);
        
        // System performance indicators
        metrics["AverageResponseTime"] = await CalculateAverageResponseTime(today, now);
        
        // Storage usage (approximate)
        metrics["EstimatedStorageUsageMB"] = await EstimateStorageUsage();

        return metrics;
    }

    #endregion

    #region Private Helper Methods

    private string GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return "Unknown";

        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string CalculateChangedProperties(object oldValues, object newValues)
    {
        try
        {
            var oldJson = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(oldValues));
            var newJson = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(newValues));
            
            var changedProps = new List<string>();
            
            if (oldJson != null && newJson != null)
            {
                foreach (var key in newJson.Keys.Union(oldJson.Keys))
                {
                    var oldVal = oldJson.ContainsKey(key) ? oldJson[key]?.ToString() : null;
                    var newVal = newJson.ContainsKey(key) ? newJson[key]?.ToString() : null;
                    
                    if (oldVal != newVal)
                    {
                        changedProps.Add(key);
                    }
                }
            }
            
            return string.Join(",", changedProps);
        }
        catch
        {
            return "Unknown";
        }
    }

    private async Task<double> CalculateErrorRate(DateTime startTime, DateTime endTime)
    {
        var totalLogs = await _context.AuditLogs
            .Where(al => al.CreatedAt >= startTime && al.CreatedAt <= endTime)
            .CountAsync();

        if (totalLogs == 0) return 0;

        var errorLogs = await _context.AuditLogs
            .Where(al => al.CreatedAt >= startTime && al.CreatedAt <= endTime && !al.IsSuccess)
            .CountAsync();

        return (double)errorLogs / totalLogs * 100;
    }

    private async Task<double> CalculateAverageResponseTime(DateTime startTime, DateTime endTime)
    {
        // This is a simplified calculation - in a real system you'd track actual response times
        var recentActivity = await _context.AuditLogs
            .Where(al => al.CreatedAt >= startTime && al.CreatedAt <= endTime)
            .CountAsync();

        // Return estimated average based on activity level
        return recentActivity > 1000 ? 250 : recentActivity > 100 ? 150 : 50;
    }

    private async Task<long> EstimateStorageUsage()
    {
        var auditLogCount = await _context.AuditLogs.CountAsync();
        var securityEventCount = await _context.SecurityEvents.CountAsync();
        var sessionCount = await _context.UserSessions.CountAsync();
        var dataChangeCount = await _context.DataChangeLogs.CountAsync();

        // Rough estimates in bytes, then convert to MB
        var estimatedBytes = (auditLogCount * 500) + 
                            (securityEventCount * 400) + 
                            (sessionCount * 300) + 
                            (dataChangeCount * 600);

        return estimatedBytes / (1024 * 1024); // Convert to MB
    }

    private byte[] ExportToCsv<T>(IEnumerable<T> data)
    {
        // Simple CSV export implementation
        var csv = new System.Text.StringBuilder();
        
        // Add headers (simplified)
        var properties = typeof(T).GetProperties();
        csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));
        
        // Add data rows
        foreach (var item in data)
        {
            var values = properties.Select(p => 
            {
                var value = p.GetValue(item)?.ToString() ?? "";
                return value.Contains(",") ? $"\"{value}\"" : value;
            });
            csv.AppendLine(string.Join(",", values));
        }
        
        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    private AuditLogDto MapToAuditLogDto(AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id,
            UserId = auditLog.UserId,
            UserEmail = auditLog.UserEmail,
            Action = auditLog.Action,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            OldValues = auditLog.OldValues,
            NewValues = auditLog.NewValues,
            Description = auditLog.Description,
            IpAddress = auditLog.IpAddress,
            UserAgent = auditLog.UserAgent,
            Severity = auditLog.Severity,
            Category = auditLog.Category,
            CreatedAt = auditLog.CreatedAt,
            CorrelationId = auditLog.CorrelationId,
            IsSuccess = auditLog.IsSuccess,
            ErrorMessage = auditLog.ErrorMessage
        };
    }

    private SecurityEventDto MapToSecurityEventDto(SecurityEvent securityEvent)
    {
        return new SecurityEventDto
        {
            Id = securityEvent.Id,
            EventType = securityEvent.EventType,
            Severity = securityEvent.Severity,
            Title = securityEvent.Title,
            Description = securityEvent.Description,
            UserId = securityEvent.UserId,
            UserEmail = securityEvent.UserEmail,
            IpAddress = securityEvent.IpAddress,
            UserAgent = securityEvent.UserAgent,
            Resource = securityEvent.Resource,
            HttpMethod = securityEvent.HttpMethod,
            RequestUrl = securityEvent.RequestUrl,
            ResponseCode = securityEvent.ResponseCode,
            CreatedAt = securityEvent.CreatedAt,
            CorrelationId = securityEvent.CorrelationId,
            IsResolved = securityEvent.IsResolved,
            ResolvedAt = securityEvent.ResolvedAt,
            ResolvedBy = securityEvent.ResolvedBy,
            ResolutionNotes = securityEvent.ResolutionNotes
        };
    }

    private UserSessionDto MapToUserSessionDto(UserSession userSession)
    {
        return new UserSessionDto
        {
            Id = userSession.Id,
            UserId = userSession.UserId,
            SessionId = userSession.SessionId,
            IpAddress = userSession.IpAddress,
            UserAgent = userSession.UserAgent,
            Location = userSession.Location,
            DeviceType = userSession.DeviceType,
            LoginTime = userSession.LoginTime,
            LogoutTime = userSession.LogoutTime,
            LastActivity = userSession.LastActivity,
            IsActive = userSession.IsActive,
            IsBlocked = userSession.IsBlocked,
            BlockReason = userSession.BlockReason,
            LoginAttempts = userSession.LoginAttempts,
            LastLoginAttempt = userSession.LastLoginAttempt,
            UserEmail = userSession.User?.Email ?? "Unknown"
        };
    }

    private DataChangeLogDto MapToDataChangeLogDto(DataChangeLog dataChangeLog)
    {
        return new DataChangeLogDto
        {
            Id = dataChangeLog.Id,
            EntityType = dataChangeLog.EntityType,
            EntityId = dataChangeLog.EntityId,
            ChangeType = dataChangeLog.ChangeType,
            UserId = dataChangeLog.UserId,
            UserEmail = dataChangeLog.UserEmail,
            OldValues = dataChangeLog.OldValues,
            NewValues = dataChangeLog.NewValues,
            ChangedProperties = dataChangeLog.ChangedProperties,
            ChangeTime = dataChangeLog.ChangeTime,
            IpAddress = dataChangeLog.IpAddress,
            Reason = dataChangeLog.Reason,
            CorrelationId = dataChangeLog.CorrelationId,
            Version = dataChangeLog.Version,
            ParentEntityType = dataChangeLog.ParentEntityType,
            ParentEntityId = dataChangeLog.ParentEntityId
        };
    }

    #endregion
}