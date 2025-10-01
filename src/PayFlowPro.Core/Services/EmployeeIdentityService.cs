using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.Entities;

namespace PayFlowPro.Core.Services;

/// <summary>
/// Interface for employee identity resolution services
/// </summary>
public interface IEmployeeIdentityService
{
    /// <summary>
    /// Gets the employee ID for the current authenticated user
    /// </summary>
    /// <param name="userId">The authenticated user's ID</param>
    /// <returns>The employee ID, or null if not found</returns>
    Task<int?> GetEmployeeIdAsync(string userId);
    
    /// <summary>
    /// Gets the employee record for the current authenticated user
    /// </summary>
    /// <param name="userId">The authenticated user's ID</param>
    /// <returns>The employee record, or null if not found</returns>
    Task<Employee?> GetEmployeeAsync(string userId);
    
    /// <summary>
    /// Checks if the user is associated with an employee record
    /// </summary>
    /// <param name="userId">The authenticated user's ID</param>
    /// <returns>True if the user has an employee record</returns>
    Task<bool> IsEmployeeAsync(string userId);
}

/// <summary>
/// Service implementation for employee identity resolution
/// </summary>
public class EmployeeIdentityService : IEmployeeIdentityService
{
    private readonly ApplicationDbContext _context;
    
    public EmployeeIdentityService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<int?> GetEmployeeIdAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return null;
            
        var employee = await _context.Employees
            .Where(e => e.UserId == userId)
            .Select(e => new { e.Id })
            .FirstOrDefaultAsync();
            
        return employee?.Id;
    }
    
    public async Task<Employee?> GetEmployeeAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return null;
            
        return await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Company)
            .FirstOrDefaultAsync(e => e.UserId == userId);
    }
    
    public async Task<bool> IsEmployeeAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return false;
            
        return await _context.Employees
            .AnyAsync(e => e.UserId == userId);
    }
}