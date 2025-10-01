@echo off
echo =======================================================
echo         PAYSLIP MANAGEMENT AUDIT SETUP
echo =======================================================
echo.

echo 🚀 Setting up Audit System Tables...
echo.

echo This script will:
echo  1. Create audit tables in your database
echo  2. Set up indexes for performance
echo  3. Configure foreign key constraints
echo.

echo Prerequisites:
echo  ✓ SQL Server running (LocalDB or full instance)
echo  ✓ PayslipManagementDb database exists
echo  ✓ User has db_ddladmin permissions
echo.

set /p continue="Continue with setup? (Y/N): "
if /i not "%continue%"=="Y" (
    echo Setup cancelled.
    pause
    exit /b
)

echo.
echo 🔧 Executing audit table creation script...
echo.

:: Try LocalDB first
echo Attempting connection to LocalDB...
sqlcmd -S "(localdb)\MSSQLLocalDB" -d PayslipManagementDb -i create_audit_tables.sql -o audit_setup.log 2>&1

if %ERRORLEVEL% EQU 0 (
    echo ✅ Audit tables created successfully!
    echo.
    echo 📊 Verifying table creation...
    sqlcmd -S "(localdb)\MSSQLLocalDB" -d PayslipManagementDb -Q "SELECT name FROM sys.tables WHERE name IN ('AuditLogs', 'SecurityEvents', 'UserSessions', 'DataChangeLogs')" -h -1
    echo.
    echo 🎉 Audit system setup complete!
    echo.
    echo Next steps:
    echo  1. Start your application: dotnet run --project src\PayslipManagement.Blazor
    echo  2. Login as admin user
    echo  3. Navigate to Audit ^> Audit Testing
    echo  4. Run the test suite to verify functionality
    echo.
) else (
    echo ❌ Failed to create audit tables with LocalDB
    echo.
    echo 🔄 Trying alternative connection...
    echo Please enter your SQL Server connection details:
    set /p server="Server name (default: localhost): "
    if "%server%"=="" set server=localhost
    
    echo.
    echo Attempting connection to %server%...
    sqlcmd -S "%server%" -d PayslipManagementDb -E -i create_audit_tables.sql -o audit_setup.log 2>&1
    
    if %ERRORLEVEL% EQU 0 (
        echo ✅ Audit tables created successfully!
        echo.
        echo 📊 Verifying table creation...
        sqlcmd -S "%server%" -d PayslipManagementDb -E -Q "SELECT name FROM sys.tables WHERE name IN ('AuditLogs', 'SecurityEvents', 'UserSessions', 'DataChangeLogs')" -h -1
        echo.
        echo 🎉 Audit system setup complete!
    ) else (
        echo ❌ Failed to create audit tables
        echo.
        echo 📋 Manual setup required:
        echo  1. Open SQL Server Management Studio
        echo  2. Connect to your database server
        echo  3. Open and execute: create_audit_tables.sql
        echo  4. Verify tables are created
        echo.
        echo Check audit_setup.log for detailed error information.
    )
)

echo.
echo 📝 Setup log saved to: audit_setup.log
echo.
pause