using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PayFlowPro.Core.Interfaces;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.Entities;
using PayFlowPro.Models.Enums;
using PayFlowPro.Shared.DTOs.Dashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PayFlowPro.Core.Services;

/// <summary>
/// Service implementation for role-specific dashboard data retrieval
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public DashboardService(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(string userId)
    {
        var baseData = await GetBaseDashboardAsync(userId);
        
        // Get system-wide statistics
        var totalEmployees = await _context.Employees.CountAsync();
        var activeEmployees = await _context.Employees.CountAsync(e => e.Status == EmploymentStatus.Active);
        var totalDepartments = await _context.Departments.CountAsync(d => d.IsActive);
        var totalUsers = await _context.Users.CountAsync();
        
        // Get current month payroll
        var currentMonth = DateTime.Now;
        var startOfMonth = new DateTime(currentMonth.Year, currentMonth.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        
        var totalPayrollThisMonth = await _context.Payslips
            .Where(p => p.PayPeriodStart >= startOfMonth && p.PayPeriodEnd <= endOfMonth)
            .SumAsync(p => p.NetSalary);

        // Get pending approvals
        var pendingApprovals = await _context.Payslips
            .CountAsync(p => p.Status == PayslipStatus.Generated);

        // Get recent system activities from audit logs
        var systemActivities = await _context.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new SystemActivityDto
            {
                Activity = a.Description,
                Timestamp = a.CreatedAt,
                Severity = a.Severity,
                Category = a.Category
            })
            .ToListAsync();

        // Get recent user activities
        var userActivities = await _context.AuditLogs
            .Where(a => a.Category == "Security")
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new UserActivityDto
            {
                UserName = a.UserEmail,
                Action = a.Action,
                Timestamp = a.CreatedAt,
                IpAddress = a.IpAddress
            })
            .ToListAsync();

        return new AdminDashboardDto
        {
            UserName = baseData.UserName,
            UserEmail = baseData.UserEmail,
            Role = baseData.Role,
            LastLoginAt = baseData.LastLoginAt,
            RecentActivities = baseData.RecentActivities,
            TotalEmployees = totalEmployees,
            ActiveEmployees = activeEmployees,
            TotalDepartments = totalDepartments,
            TotalUsers = totalUsers,
            PendingApprovals = pendingApprovals,
            SystemAlerts = 0, // Can be enhanced with actual alert logic
            TotalPayrollThisMonth = totalPayrollThisMonth,
            SystemActivities = systemActivities,
            UserActivities = userActivities,
            DatabaseStats = new DatabaseStatsDto
            {
                TotalRecords = await GetTotalRecordsAsync(),
                DatabaseSizeMB = 0, // Would need specific DB queries
                ActiveConnections = 1,
                QueryPerformanceMs = 45.2 // Mock data
            }
        };
    }

    public async Task<HRDashboardDto> GetHRDashboardAsync(string userId)
    {
        var baseData = await GetBaseDashboardAsync(userId);
        
        var totalEmployees = await _context.Employees.CountAsync();
        var activeEmployees = await _context.Employees.CountAsync(e => e.Status == EmploymentStatus.Active);
        
        // Get new hires and departures this month
        var currentMonth = DateTime.Now;
        var startOfMonth = new DateTime(currentMonth.Year, currentMonth.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        
        var newHiresThisMonth = await _context.Employees
            .CountAsync(e => e.DateOfJoining >= startOfMonth && e.DateOfJoining <= endOfMonth);
            
        var employeesLeavingThisMonth = await _context.Employees
            .CountAsync(e => e.DateOfLeaving.HasValue && 
                           e.DateOfLeaving.Value >= startOfMonth && 
                           e.DateOfLeaving.Value <= endOfMonth);

        // Get payslip approvals
        var pendingPayslipApprovals = await _context.Payslips
            .CountAsync(p => p.Status == PayslipStatus.Generated);

        var totalPayrollThisMonth = await _context.Payslips
            .Where(p => p.PayPeriodStart >= startOfMonth && p.PayPeriodEnd <= endOfMonth)
            .SumAsync(p => p.NetSalary);

        var averageSalary = await _context.Employees
            .Where(e => e.Status == EmploymentStatus.Active)
            .AverageAsync(e => e.BasicSalary);

        // Get department summaries
        var departmentSummaries = await _context.Departments
            .Where(d => d.IsActive)
            .Select(d => new DepartmentSummaryDto
            {
                DepartmentName = d.Name,
                EmployeeCount = d.Employees.Count(e => e.Status == EmploymentStatus.Active),
                TotalPayroll = d.Employees
                    .Where(e => e.Status == EmploymentStatus.Active)
                    .Sum(e => e.BasicSalary),
                ManagerName = d.Manager != null ? d.Manager.FirstName + " " + d.Manager.LastName : "No Manager"
            })
            .ToListAsync();

        // Get payslip status distribution
        var payslipStatuses = await _context.Payslips
            .GroupBy(p => p.Status)
            .Select(g => new PayslipStatusDto
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                StatusColor = GetStatusColor(g.Key)
            })
            .ToListAsync();

        // Generate upcoming HR tasks (mock data for now)
        var upcomingTasks = GenerateUpcomingHRTasks();

        return new HRDashboardDto
        {
            UserName = baseData.UserName,
            UserEmail = baseData.UserEmail,
            Role = baseData.Role,
            LastLoginAt = baseData.LastLoginAt,
            RecentActivities = baseData.RecentActivities,
            TotalEmployees = totalEmployees,
            ActiveEmployees = activeEmployees,
            NewHiresThisMonth = newHiresThisMonth,
            EmployeesLeavingThisMonth = employeesLeavingThisMonth,
            PendingPayslipApprovals = pendingPayslipApprovals,
            DepartmentsManaged = departmentSummaries.Count,
            TotalPayrollThisMonth = totalPayrollThisMonth,
            AverageSalary = averageSalary,
            DepartmentSummaries = departmentSummaries,
            PayslipStatuses = payslipStatuses,
            UpcomingTasks = upcomingTasks
        };
    }

    public async Task<EmployeeDashboardDto> GetEmployeeDashboardAsync(string userId)
    {
        var baseData = await GetBaseDashboardAsync(userId);
        
        // Get employee record
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Company)
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (employee == null)
        {
            throw new InvalidOperationException("Employee record not found for user");
        }

        // Get manager information
        var manager = employee.Department?.Manager;
        var managerName = manager != null ? $"{manager.FirstName} {manager.LastName}" : "No Manager Assigned";

        // Calculate year-to-date earnings
        var currentYear = DateTime.Now.Year;
        var yearToDateEarnings = await _context.Payslips
            .Where(p => p.EmployeeId == employee.Id && p.PayPeriodStart.Year == currentYear)
            .SumAsync(p => p.NetSalary);

        // Get next pay date (estimate - first of next month)
        var nextPayDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);

        // Get total payslips count
        var totalPayslips = await _context.Payslips
            .CountAsync(p => p.EmployeeId == employee.Id);

        // Get recent payslips
        var recentPayslips = await _context.Payslips
            .Where(p => p.EmployeeId == employee.Id)
            .OrderByDescending(p => p.PayPeriodStart)
            .Take(6)
            .Select(p => new PersonalPayslipDto
            {
                PayslipId = p.Id,
                PayslipNumber = p.PayslipNumber,
                Period = p.PayPeriodStart.ToString("MMM yyyy"),
                GrossSalary = p.GrossSalary,
                NetSalary = p.NetSalary,
                Status = p.Status.ToString(),
                PayDate = p.PayDate != default(DateTime) ? p.PayDate : p.PayPeriodEnd.AddDays(5) // Estimate if not set
            })
            .ToListAsync();

        // Generate career milestones
        var careerMilestones = GenerateCareerMilestones(employee);

        // Calculate personal statistics
        var yearsOfService = DateTime.Now.Year - employee.DateOfJoining.Year;
        var monthsOfService = ((DateTime.Now.Year - employee.DateOfJoining.Year) * 12) + 
                              DateTime.Now.Month - employee.DateOfJoining.Month;

        // Get salary growth (mock calculation)
        var firstPayslip = await _context.Payslips
            .Where(p => p.EmployeeId == employee.Id)
            .OrderBy(p => p.PayPeriodStart)
            .FirstOrDefaultAsync();

        var salaryGrowthPercentage = 0m;
        if (firstPayslip != null && firstPayslip.GrossSalary > 0)
        {
            salaryGrowthPercentage = ((employee.BasicSalary - firstPayslip.GrossSalary) / firstPayslip.GrossSalary) * 100;
        }

        return new EmployeeDashboardDto
        {
            UserName = baseData.UserName,
            UserEmail = baseData.UserEmail,
            Role = baseData.Role,
            LastLoginAt = baseData.LastLoginAt,
            RecentActivities = baseData.RecentActivities,
            EmployeeCode = employee.EmployeeCode,
            JobTitle = employee.JobTitle ?? "Not Specified",
            DepartmentName = employee.Department?.Name ?? "No Department",
            ManagerName = managerName,
            DateOfJoining = employee.DateOfJoining,
            CurrentSalary = employee.BasicSalary,
            YearToDateEarnings = yearToDateEarnings,
            NextPayDate = nextPayDate,
            TotalPayslips = totalPayslips,
            RecentPayslips = recentPayslips,
            CareerMilestones = careerMilestones,
            PersonalStats = new EmployeeStatsDto
            {
                YearsOfService = yearsOfService,
                MonthsOfService = monthsOfService,
                SalaryGrowthPercentage = salaryGrowthPercentage,
                TotalPayslipsGenerated = totalPayslips,
                LastSalaryUpdate = employee.UpdatedAt
            }
        };
    }

    public async Task<BaseDashboardDto> GetBaseDashboardAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new ArgumentException("User not found", nameof(userId));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Employee";

        // Get recent activities from audit logs
        var recentActivities = await _context.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .Select(a => a.Description)
            .ToListAsync();

        return new BaseDashboardDto
        {
            UserName = user.FullName,
            UserEmail = user.Email ?? "",
            Role = primaryRole,
            LastLoginAt = user.LastLoginAt ?? DateTime.Now,
            RecentActivities = recentActivities
        };
    }

    #region Private Helper Methods

    private async Task<long> GetTotalRecordsAsync()
    {
        var employeeCount = await _context.Employees.CountAsync();
        var payslipCount = await _context.Payslips.CountAsync();
        var userCount = await _context.Users.CountAsync();
        var auditCount = await _context.AuditLogs.CountAsync();
        
        return employeeCount + payslipCount + userCount + auditCount;
    }

    private static string GetStatusColor(PayslipStatus status)
    {
        return status switch
        {
            PayslipStatus.Draft => "secondary",
            PayslipStatus.Generated => "warning",
            PayslipStatus.Approved => "info",
            PayslipStatus.Sent => "primary",
            PayslipStatus.Paid => "success",
            _ => "secondary"
        };
    }

    private static List<UpcomingTaskDto> GenerateUpcomingHRTasks()
    {
        return new List<UpcomingTaskDto>
        {
            new() { Task = "Monthly Payroll Processing", DueDate = DateTime.Now.AddDays(3), Priority = "High", Category = "Payroll" },
            new() { Task = "Performance Reviews Due", DueDate = DateTime.Now.AddDays(7), Priority = "Medium", Category = "Performance" },
            new() { Task = "New Employee Onboarding", DueDate = DateTime.Now.AddDays(2), Priority = "High", Category = "Recruitment" },
            new() { Task = "Benefits Enrollment Review", DueDate = DateTime.Now.AddDays(14), Priority = "Low", Category = "Benefits" },
            new() { Task = "Department Budget Review", DueDate = DateTime.Now.AddDays(10), Priority = "Medium", Category = "Finance" }
        };
    }

    private static List<PersonalMilestoneDto> GenerateCareerMilestones(Employee employee)
    {
        var milestones = new List<PersonalMilestoneDto>
        {
            new() 
            { 
                Title = "Joined Company", 
                Date = employee.DateOfJoining, 
                Description = $"Started as {employee.JobTitle}", 
                Icon = "fas fa-door-open" 
            }
        };

        // Add years of service milestones
        var yearsOfService = DateTime.Now.Year - employee.DateOfJoining.Year;
        for (int i = 1; i <= yearsOfService; i++)
        {
            if (i == 1 || i % 5 == 0 || i == yearsOfService)
            {
                milestones.Add(new PersonalMilestoneDto
                {
                    Title = $"{i} Year{(i > 1 ? "s" : "")} of Service",
                    Date = employee.DateOfJoining.AddYears(i),
                    Description = $"Completed {i} year{(i > 1 ? "s" : "")} with the company",
                    Icon = i >= 5 ? "fas fa-trophy" : "fas fa-star"
                });
            }
        }

        return milestones.OrderByDescending(m => m.Date).Take(5).ToList();
    }

    #endregion
}