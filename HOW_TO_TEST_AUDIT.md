# 🎯 **How to Test the Audit System** - Quick Start Guide

## 📋 **TL;DR - Immediate Testing Steps**

### **1. Set Up Database Tables (Required First!)**
```bash
# Option A: Use the automated script
./setup_audit_tables.bat

# Option B: Manual setup via SQL Server Management Studio
# Execute: create_audit_tables.sql
```

### **2. Start the Application**
```bash
cd C:\sal\PayslipManagement
dotnet run --project src\PayslipManagement.Blazor
```

### **3. Access Audit Testing**
1. **Login as Admin** (admin@payslip.com)
2. **Navigate to**: `Audit > Audit Testing` in the menu
3. **Run Tests**: Click individual test buttons or "Run All Tests"

---

## 🧪 **Interactive Testing Dashboard**

The audit testing page (`/audit/test`) provides:

### **✅ Automated Test Suite**
- **Basic Audit Logging** - Tests core audit functionality
- **Security Event Logging** - Tests security event tracking  
- **User Session Tracking** - Tests session management
- **Data Change Logging** - Tests change tracking

### **📊 Real-Time Results**
- **Visual Status Cards** - Shows pass/fail status for each test
- **Live Test Log** - Real-time test execution feedback
- **Error Reporting** - Detailed error messages if tests fail

### **🔧 Utility Functions**
- **Database Check** - Verifies audit tables exist and are accessible
- **Generate Test Data** - Creates sample audit logs for testing
- **Quick Links** - Direct access to audit logs viewer

---

## 🔍 **Manual Testing Scenarios**

### **Scenario 1: Employee Management**
```csharp
// Test audit logging during employee operations
1. Create new employee → Check for audit log
2. Update employee details → Verify before/after values logged
3. Delete employee → Confirm deletion is tracked
```

### **Scenario 2: Security Events**
```csharp
// Test security event generation
1. Failed login attempts → Should log security events
2. Permission denied actions → Should track unauthorized access
3. Data export operations → Should log data access events
```

### **Scenario 3: User Sessions**
```csharp
// Test session tracking
1. User login → Session created
2. User activity → Session updated  
3. User logout → Session ended
```

---

## 📈 **Verification Methods**

### **Database Verification**
```sql
-- Check recent audit logs
SELECT TOP 10 * FROM AuditLogs 
ORDER BY CreatedAt DESC;

-- Verify security events
SELECT EventType, Severity, COUNT(*) 
FROM SecurityEvents 
GROUP BY EventType, Severity;

-- Check user sessions
SELECT * FROM UserSessions 
WHERE CreatedAt >= DATEADD(hour, -24, GETUTCDATE());
```

### **UI Verification**
1. **Audit Logs Dashboard** (`/audit/logs`)
   - Verify logs display correctly
   - Test filtering functionality
   - Check export capabilities

2. **Test Results Page** (`/audit/test`)
   - Run comprehensive test suite
   - Monitor real-time results
   - Review error messages

---

## 🚀 **Expected Test Results**

### **✅ Successful Test Run Should Show:**
- ✅ Basic Audit Test: **PASSED**
- ✅ Security Event Test: **PASSED** 
- ✅ User Session Test: **PASSED**
- ✅ Data Change Test: **PASSED**

### **📊 Database Should Contain:**
- **AuditLogs**: Activity records with before/after values
- **SecurityEvents**: Security-related events with severity levels
- **UserSessions**: User login/activity tracking
- **DataChangeLogs**: Detailed field-level change tracking

---

## ⚠️ **Troubleshooting Common Issues**

### **Issue**: "Invalid object name 'AuditLogs'"
**Solution**: Run `setup_audit_tables.bat` or execute `create_audit_tables.sql`

### **Issue**: Tests fail with permission errors
**Solution**: Ensure logged in as Admin user with proper roles

### **Issue**: No audit logs appearing
**Solution**: Check service registration in `Program.cs` and verify HttpContext availability

### **Issue**: Performance issues with large datasets
**Solution**: Database indexes are included in the setup script for optimization

---

## 📋 **Testing Checklist**

### **Pre-Testing Setup**
- [ ] Audit tables created in database
- [ ] Application starts without errors
- [ ] Admin user can access audit sections
- [ ] All audit services registered properly

### **Functional Testing**
- [ ] Basic audit logging works
- [ ] Security events are captured
- [ ] User sessions are tracked
- [ ] Data changes are logged
- [ ] Filtering and search work
- [ ] Export functionality works

### **Performance Testing**
- [ ] Large volume queries perform well
- [ ] Concurrent operations work correctly
- [ ] Database indexes are effective
- [ ] Memory usage is reasonable

### **Security Testing**
- [ ] Only authorized users can access audit data
- [ ] Sensitive information is properly masked
- [ ] SQL injection prevention works
- [ ] XSS prevention is effective

---

## 🎉 **Success Metrics**

### **Comprehensive Audit System Verified When:**
1. **All 4 test categories pass** in the testing dashboard
2. **Database contains audit data** with proper relationships
3. **UI displays audit logs** with filtering and export
4. **Real-time logging works** during normal application operations
5. **Performance is acceptable** under load
6. **Security measures** prevent unauthorized access

---

## 📞 **Next Steps After Testing**

### **If Tests Pass:**
- ✅ Audit system is fully operational
- ✅ Begin using in production environment
- ✅ Monitor audit logs for security and compliance
- ✅ Set up automated cleanup for old logs

### **If Tests Fail:**
- 🔍 Review error messages in test dashboard
- 🔍 Check database setup and permissions
- 🔍 Verify service registrations
- 🔍 Consult troubleshooting guide above

---

**🚀 Ready to test? Run the setup script and navigate to the audit testing page!**