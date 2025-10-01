using System;
using System.Collections.Generic;

namespace PayFlowPro.Shared.DTOs.Dashboard;

/// <summary>
/// Base dashboard DTO containing common information for all roles
/// </summary>
public class BaseDashboardDto
{
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime LastLoginAt { get; set; }
    public List<string> RecentActivities { get; set; } = new();
}

/// <summary>
/// Admin-specific dashboard data with system-wide metrics
/// </summary>
public class AdminDashboardDto : BaseDashboardDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalUsers { get; set; }
    public int PendingApprovals { get; set; }
    public int SystemAlerts { get; set; }
    public decimal TotalPayrollThisMonth { get; set; }
    public List<SystemActivityDto> SystemActivities { get; set; } = new();
    public List<UserActivityDto> UserActivities { get; set; } = new();
    public DatabaseStatsDto DatabaseStats { get; set; } = new();
}

/// <summary>
/// HR-specific dashboard data focusing on workforce management
/// </summary>
public class HRDashboardDto : BaseDashboardDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int NewHiresThisMonth { get; set; }
    public int EmployeesLeavingThisMonth { get; set; }
    public int PendingPayslipApprovals { get; set; }
    public int DepartmentsManaged { get; set; }
    public decimal TotalPayrollThisMonth { get; set; }
    public decimal AverageSalary { get; set; }
    public List<DepartmentSummaryDto> DepartmentSummaries { get; set; } = new();
    public List<PayslipStatusDto> PayslipStatuses { get; set; } = new();
    public List<UpcomingTaskDto> UpcomingTasks { get; set; } = new();
}

/// <summary>
/// Employee-specific dashboard data with personal information
/// </summary>
public class EmployeeDashboardDto : BaseDashboardDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public DateTime DateOfJoining { get; set; }
    public decimal CurrentSalary { get; set; }
    public decimal YearToDateEarnings { get; set; }
    public DateTime? NextPayDate { get; set; }
    public int TotalPayslips { get; set; }
    public List<PersonalPayslipDto> RecentPayslips { get; set; } = new();
    public List<PersonalMilestoneDto> CareerMilestones { get; set; } = new();
    public EmployeeStatsDto PersonalStats { get; set; } = new();
}

/// <summary>
/// System activity tracking for admin dashboard
/// </summary>
public class SystemActivityDto
{
    public string Activity { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// User activity tracking for admin dashboard
/// </summary>
public class UserActivityDto
{
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}

/// <summary>
/// Database statistics for admin dashboard
/// </summary>
public class DatabaseStatsDto
{
    public long TotalRecords { get; set; }
    public long DatabaseSizeMB { get; set; }
    public int ActiveConnections { get; set; }
    public double QueryPerformanceMs { get; set; }
}

/// <summary>
/// Department summary for HR dashboard
/// </summary>
public class DepartmentSummaryDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal TotalPayroll { get; set; }
    public string ManagerName { get; set; } = string.Empty;
}

/// <summary>
/// Payslip status summary for HR dashboard
/// </summary>
public class PayslipStatusDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public string StatusColor { get; set; } = string.Empty;
}

/// <summary>
/// Upcoming tasks for HR dashboard
/// </summary>
public class UpcomingTaskDto
{
    public string Task { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Personal payslip information for employee dashboard
/// </summary>
public class PersonalPayslipDto
{
    public int PayslipId { get; set; }
    public string PayslipNumber { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal GrossSalary { get; set; }
    public decimal NetSalary { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime PayDate { get; set; }
}

/// <summary>
/// Career milestones for employee dashboard
/// </summary>
public class PersonalMilestoneDto
{
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// Employee personal statistics
/// </summary>
public class EmployeeStatsDto
{
    public int YearsOfService { get; set; }
    public int MonthsOfService { get; set; }
    public decimal SalaryGrowthPercentage { get; set; }
    public int TotalPayslipsGenerated { get; set; }
    public DateTime LastSalaryUpdate { get; set; }
}