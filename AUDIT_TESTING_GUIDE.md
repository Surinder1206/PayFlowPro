# Audit System Testing Guide

## 🔍 **How to Test the Audit Functionality**

This comprehensive guide shows you how to test all aspects of the audit trail system we've built for the payslip management application.

---

## 📋 **Phase 1: Database Setup & Initial Testing**

### Step 1: Create Audit Tables
```sql
-- Run the SQL script to create audit tables
-- Execute: create_audit_tables.sql in your SQL Server Management Studio
-- OR use sqlcmd from command line:
sqlcmd -S (localdb)\MSSQLLocalDB -d PayFlowProDb -i create_audit_tables.sql
```

### Step 2: Verify Tables Creation
```sql
-- Check if tables were created successfully
SELECT name FROM sys.tables WHERE name IN ('AuditLogs', 'SecurityEvents', 'UserSessions', 'DataChangeLogs');

-- Verify table structures
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'AuditLogs'
ORDER BY ORDINAL_POSITION;
```

---

## 🧪 **Phase 2: Manual Audit Testing**

### Test 1: Basic Audit Logging
Create a simple test page to verify audit logging is working:

```csharp
// Add this to a test razor page or controller
@inject IAuditService AuditService

<button class="btn btn-primary" @onclick="TestBasicAudit">Test Basic Audit</button>

@code {
    private async Task TestBasicAudit()
    {
        await AuditService.LogActivityAsync(
            "TestAction", 
            "TestEntity", 
            "123",
            oldValues: new { Name = "Old Value", Amount = 100 },
            newValues: new { Name = "New Value", Amount = 200 },
            "Testing basic audit functionality"
        );
    }
}
```

### Test 2: Security Event Logging
```csharp
private async Task TestSecurityEvent()
{
    await AuditService.LogSecurityEventAsync(
        SecurityEventType.SuspiciousActivity,
        SecuritySeverity.High,
        "Test Security Event",
        "This is a test security event to verify logging functionality"
    );
}
```

### Test 3: User Session Tracking
```csharp
private async Task TestUserSession()
{
    var sessionDto = new CreateUserSessionDto
    {
        UserId = "test-user-id",
        SessionId = Guid.NewGuid().ToString(),
        IpAddress = "192.168.1.100",
        UserAgent = "Test User Agent",
        DeviceType = "Desktop"
    };
    
    var session = await AuditService.CreateSessionAsync(sessionDto);
    
    // Update session activity
    await AuditService.UpdateSessionActivityAsync(session.SessionId);
}
```

---

## 🔄 **Phase 3: Integration Testing Scenarios**

### Scenario 1: Employee Management Actions
Test audit logging when performing CRUD operations on employees:

1. **Create Employee** - Should log:
   - AuditLog: Action="Create", EntityType="Employee"
   - DataChangeLog: ChangeType="Create"

2. **Update Employee** - Should log:
   - AuditLog with old/new values comparison
   - DataChangeLog with changed properties

3. **Delete Employee** - Should log:
   - AuditLog: Action="Delete"
   - SecurityEvent if deletion requires special permissions

### Scenario 2: Payslip Processing Workflow
Test comprehensive audit trail during payslip generation:

1. **Generate Payslip**:
   ```csharp
   // Should automatically log via service
   await PayslipService.GeneratePayslipAsync(employeeId, payPeriod);
   ```

2. **Approve Payslip**:
   ```csharp
   // Should log approval action
   await PayslipService.ApprovePayslipAsync(payslipId, approverId);
   ```

3. **Send Email**:
   ```csharp
   // Should log email action
   await PayslipService.SendPayslipEmailAsync(payslipId);
   ```

### Scenario 3: Authentication & Authorization Testing
Test security event logging for auth scenarios:

1. **Successful Login**:
   - Should create UserSession
   - Should log SecurityEvent: LoginSuccess

2. **Failed Login**:
   - Should log SecurityEvent: LoginFailure
   - Should track failed attempts

3. **Permission Denied**:
   - Should log SecurityEvent: UnauthorizedAccess

---

## 📊 **Phase 4: Dashboard & UI Testing**

### Test the Audit Logs Dashboard
1. **Navigate to `/audit/logs`** (Admin users only)
2. **Verify Display**:
   - Summary cards show correct counts
   - Audit logs table displays recent entries
   - Filtering works correctly
   - Pagination functions properly

### Test Filtering Functionality
```csharp
// Test various filter combinations:
var filter = new AuditLogFilterDto
{
    StartDate = DateTime.Today.AddDays(-7),
    EndDate = DateTime.Today,
    Severity = AuditSeverity.Error,
    Category = AuditCategory.Security,
    UserEmail = "admin@payslip.com"
};

var results = await AuditService.GetAuditLogsAsync(filter);
```

### Test Export Functionality
```csharp
// Test CSV export
var exportData = await AuditService.ExportAuditLogsAsync(filter, "CSV");
// Verify the exported file contains correct data
```

---

## 🧪 **Phase 5: Automated Testing**

### Unit Tests for AuditService
Create comprehensive unit tests:

```csharp
[Test]
public async Task LogActivityAsync_ShouldCreateAuditLog()
{
    // Arrange
    var mockContext = new Mock<ApplicationDbContext>();
    var mockHttpContext = new Mock<IHttpContextAccessor>();
    var auditService = new AuditService(mockContext.Object, mockHttpContext.Object);
    
    // Act
    await auditService.LogActivityAsync("TestAction", "TestEntity", "123");
    
    // Assert
    mockContext.Verify(c => c.AuditLogs.Add(It.IsAny<AuditLog>()), Times.Once);
    mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
}

[Test]
public async Task GetAuditLogsAsync_WithFilters_ShouldReturnFilteredResults()
{
    // Test filtering logic
    var filter = new AuditLogFilterDto 
    { 
        Severity = AuditSeverity.Error,
        StartDate = DateTime.Today.AddDays(-1)
    };
    
    var results = await auditService.GetAuditLogsAsync(filter);
    
    Assert.That(results.All(r => r.Severity == "Error"));
}
```

### Integration Tests
```csharp
[Test]
public async Task EndToEndAuditTest()
{
    // 1. Perform an action that should create audit logs
    var employee = new Employee { Name = "Test Employee" };
    await employeeService.CreateAsync(employee);
    
    // 2. Verify audit logs were created
    var auditLogs = await auditService.GetAuditLogsAsync(new AuditLogFilterDto 
    { 
        EntityType = "Employee",
        Action = "Create"
    });
    
    Assert.That(auditLogs.Count(), Is.GreaterThan(0));
    
    // 3. Verify audit log content
    var auditLog = auditLogs.First();
    Assert.That(auditLog.Action, Is.EqualTo("Create"));
    Assert.That(auditLog.EntityType, Is.EqualTo("Employee"));
    Assert.That(auditLog.NewValues, Contains.Substring("Test Employee"));
}
```

---

## 🔍 **Phase 6: Performance Testing**

### Test Large Volume Audit Logs
```csharp
// Generate test data
for (int i = 0; i < 10000; i++)
{
    await auditService.LogActivityAsync($"TestAction{i}", "TestEntity", i.ToString());
}

// Test query performance
var stopwatch = Stopwatch.StartNew();
var results = await auditService.GetAuditLogsAsync(new AuditLogFilterDto 
{ 
    Page = 1, 
    PageSize = 50 
});
stopwatch.Stop();

Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000)); // Should complete in under 1 second
```

### Test Concurrent Audit Logging
```csharp
// Test thread safety
var tasks = new List<Task>();
for (int i = 0; i < 100; i++)
{
    int index = i;
    tasks.Add(Task.Run(async () => 
    {
        await auditService.LogActivityAsync($"ConcurrentAction{index}", "TestEntity");
    }));
}

await Task.WhenAll(tasks);

// Verify all logs were created
var count = await context.AuditLogs.CountAsync();
Assert.That(count, Is.GreaterThanOrEqualTo(100));
```

---

## 🛠️ **Phase 7: SQL-Based Verification**

### Verify Audit Data Integrity
```sql
-- Check for orphaned audit logs (users that don't exist)
SELECT al.* FROM AuditLogs al
LEFT JOIN AspNetUsers u ON al.UserId = u.Id
WHERE u.Id IS NULL;

-- Verify audit log completeness for critical operations
SELECT 
    COUNT(*) as TotalLogs,
    COUNT(CASE WHEN Action = 'Create' THEN 1 END) as CreateActions,
    COUNT(CASE WHEN Action = 'Update' THEN 1 END) as UpdateActions,
    COUNT(CASE WHEN Action = 'Delete' THEN 1 END) as DeleteActions
FROM AuditLogs
WHERE EntityType = 'Employee'
AND CreatedAt >= DATEADD(day, -7, GETUTCDATE());

-- Check security events by severity
SELECT 
    EventType,
    Severity,
    COUNT(*) as Count,
    MAX(CreatedAt) as LastOccurrence
FROM SecurityEvents
GROUP BY EventType, Severity
ORDER BY Count DESC;
```

### Performance Analysis Queries
```sql
-- Analyze audit log performance
SELECT 
    DATEPART(hour, CreatedAt) as Hour,
    COUNT(*) as LogCount,
    AVG(DATALENGTH(NewValues)) as AvgDataSize
FROM AuditLogs
WHERE CreatedAt >= DATEADD(day, -1, GETUTCDATE())
GROUP BY DATEPART(hour, CreatedAt)
ORDER BY Hour;

-- Check index usage
SELECT 
    i.name as IndexName,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates
FROM sys.dm_db_index_usage_stats s
JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
JOIN sys.objects o ON i.object_id = o.object_id
WHERE o.name IN ('AuditLogs', 'SecurityEvents', 'UserSessions', 'DataChangeLogs');
```

---

## 📈 **Phase 8: Business Logic Testing**

### Test Compliance Reporting
```csharp
// Generate compliance report
var report = await auditService.GenerateComplianceReportAsync(
    DateTime.Today.AddMonths(-1),
    DateTime.Today,
    "GDPR"
);

// Verify report contents
Assert.That(report.TotalDataChanges, Is.GreaterThan(0));
Assert.That(report.UniqueUsers, Is.GreaterThan(0));
Assert.That(report.UserActivities.Count, Is.GreaterThan(0));
```

### Test Data Retention Policies
```csharp
// Test cleanup functionality
var cutoffDate = DateTime.UtcNow.AddDays(-90);
await auditService.CleanupOldLogsAsync(cutoffDate);

// Verify old logs are removed
var oldLogs = await context.AuditLogs
    .Where(al => al.CreatedAt < cutoffDate)
    .CountAsync();
    
Assert.That(oldLogs, Is.EqualTo(0));
```

---

## 🎯 **Testing Checklist**

### ✅ **Functional Testing**
- [ ] Basic audit logging works
- [ ] Security event logging works  
- [ ] User session tracking works
- [ ] Data change logging works
- [ ] Filtering and search works
- [ ] Export functionality works
- [ ] Dashboard displays correctly
- [ ] Pagination works properly

### ✅ **Security Testing**
- [ ] Only authorized users can access audit logs
- [ ] Sensitive data is properly masked
- [ ] SQL injection prevention works
- [ ] XSS prevention works
- [ ] Data integrity is maintained

### ✅ **Performance Testing**
- [ ] Large volume queries perform well
- [ ] Concurrent logging works correctly
- [ ] Database indexes are effective
- [ ] Memory usage is reasonable
- [ ] Response times are acceptable

### ✅ **Integration Testing**
- [ ] Audit logs are created for all CRUD operations
- [ ] Authentication events are logged
- [ ] Authorization failures are logged
- [ ] System events are captured
- [ ] Error scenarios are handled

### ✅ **Compliance Testing**
- [ ] GDPR compliance features work
- [ ] Data retention policies function
- [ ] Audit trail completeness verified
- [ ] Data export capabilities tested
- [ ] User data deletion works

---

## 🚨 **Common Issues & Troubleshooting**

### Issue 1: Audit Tables Don't Exist
**Error**: `Invalid object name 'AuditLogs'`
**Solution**: Run the `create_audit_tables.sql` script

### Issue 2: Foreign Key Constraints
**Error**: Foreign key constraint failures
**Solution**: Ensure user IDs exist in AspNetUsers table before logging

### Issue 3: Performance Issues
**Error**: Slow audit log queries
**Solution**: Check database indexes and query optimization

### Issue 4: Missing Audit Logs
**Error**: Expected audit logs not created
**Solution**: Verify service registration and dependency injection

---

## 📝 **Testing Reports**

### Generate Test Report
```csharp
public class AuditTestReport
{
    public DateTime TestDate { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public List<string> Issues { get; set; } = new();
    public TimeSpan TotalTestTime { get; set; }
    
    public void GenerateReport()
    {
        Console.WriteLine($"=== Audit System Test Report ===");
        Console.WriteLine($"Test Date: {TestDate}");
        Console.WriteLine($"Total Tests: {TotalTests}");
        Console.WriteLine($"Passed: {PassedTests}");
        Console.WriteLine($"Failed: {FailedTests}");
        Console.WriteLine($"Success Rate: {(double)PassedTests / TotalTests * 100:F1}%");
        Console.WriteLine($"Test Duration: {TotalTestTime}");
        
        if (Issues.Any())
        {
            Console.WriteLine("\nIssues Found:");
            Issues.ForEach(issue => Console.WriteLine($"- {issue}"));
        }
    }
}
```

This comprehensive testing guide provides everything you need to thoroughly test the audit functionality across all scenarios - from basic logging to complex compliance requirements! 🎉