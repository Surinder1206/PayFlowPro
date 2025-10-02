using Microsoft.EntityFrameworkCore;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.Entities;

namespace PayFlowPro.Web.Services;

public class DataFixService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataFixService> _logger;

    public DataFixService(ApplicationDbContext context, ILogger<DataFixService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> FixAccruedDaysAsync()
    {
        _logger.LogInformation("Starting to fix AccruedDays data...");

        // Find leave balances where AccruedDays equals AllocatedDays (the bug condition)
        // and they are new employees with no usage (UsedDays = 0, PendingDays = 0, CarriedOverDays = 0)
        var problematicBalances = await _context.LeaveBalances
            .Where(lb => lb.AccruedDays == lb.AllocatedDays 
                        && lb.UsedDays == 0 
                        && lb.PendingDays == 0 
                        && lb.CarriedOverDays == 0)
            .Include(lb => lb.Employee)
            .Include(lb => lb.LeaveType)
            .ToListAsync();

        _logger.LogInformation($"Found {problematicBalances.Count} leave balances to fix");

        foreach (var balance in problematicBalances)
        {
            var oldAccruedDays = balance.AccruedDays;
            balance.AccruedDays = 0; // Reset to 0 as it should be for new employees
            
            _logger.LogInformation($"Fixed {balance.Employee.FirstName} {balance.Employee.LastName} - {balance.LeaveType.Name}: AccruedDays {oldAccruedDays} → 0");
        }

        var updatedCount = await _context.SaveChangesAsync();
        
        _logger.LogInformation($"Successfully updated {updatedCount} leave balance records");
        
        return updatedCount;
    }

    public async Task LogLeaveBalancesAsync()
    {
        _logger.LogInformation("Current Leave Balance Status:");
        
        var balances = await _context.LeaveBalances
            .Include(lb => lb.Employee)
            .Include(lb => lb.LeaveType)
            .OrderBy(lb => lb.Employee.FirstName)
            .ThenBy(lb => lb.LeaveType.Name)
            .ToListAsync();

        foreach (var balance in balances)
        {
            var availableDays = balance.AllocatedDays + balance.CarriedOverDays + balance.AccruedDays - balance.UsedDays - balance.PendingDays;
            
            _logger.LogInformation($"{balance.Employee.FirstName} {balance.Employee.LastName} - {balance.LeaveType.Name}: " +
                                 $"Allocated={balance.AllocatedDays}, Accrued={balance.AccruedDays}, " +
                                 $"Available={availableDays}");
        }
    }
}