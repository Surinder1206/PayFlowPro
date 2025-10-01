using PayFlowPro.Shared.DTOs.Dashboard;
using System.Threading.Tasks;

namespace PayFlowPro.Core.Interfaces;

/// <summary>
/// Interface for dashboard data services providing role-specific dashboard information
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets admin-specific dashboard data with system-wide metrics and administration tools
    /// </summary>
    /// <param name="userId">The admin user ID</param>
    /// <returns>Admin dashboard data</returns>
    Task<AdminDashboardDto> GetAdminDashboardAsync(string userId);

    /// <summary>
    /// Gets HR-specific dashboard data focusing on workforce management
    /// </summary>
    /// <param name="userId">The HR user ID</param>
    /// <returns>HR dashboard data</returns>
    Task<HRDashboardDto> GetHRDashboardAsync(string userId);

    /// <summary>
    /// Gets employee-specific dashboard data with personal information
    /// </summary>
    /// <param name="userId">The employee user ID</param>
    /// <returns>Employee dashboard data</returns>
    Task<EmployeeDashboardDto> GetEmployeeDashboardAsync(string userId);

    /// <summary>
    /// Gets base dashboard information for any authenticated user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Base dashboard data</returns>
    Task<BaseDashboardDto> GetBaseDashboardAsync(string userId);
}