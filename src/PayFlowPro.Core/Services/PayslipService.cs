using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using PayFlowPro.Core.Interfaces;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.Entities;
using PayFlowPro.Models.Enums;

namespace PayFlowPro.Core.Services;

public class PayslipService : IPayslipService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IPayslipCalculationService _calculationService;

    public PayslipService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IPayslipCalculationService calculationService)
    {
        _contextFactory = contextFactory;
        _calculationService = calculationService;
    }

    public async Task<Payslip> GeneratePayslipAsync(int employeeId, DateTime payPeriodStart, DateTime payPeriodEnd)
    {
        using var context = _contextFactory.CreateDbContext();
        
        // Check if payslip already exists for this period
        var existingPayslip = await context.Payslips
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId 
                && p.PayPeriodStart == payPeriodStart 
                && p.PayPeriodEnd == payPeriodEnd);

        if (existingPayslip != null)
        {
            throw new InvalidOperationException($"Payslip already exists for employee {employeeId} for period {payPeriodStart:yyyy-MM-dd} to {payPeriodEnd:yyyy-MM-dd}");
        }

        // Get employee details
        var employee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            throw new ArgumentException($"Employee with ID {employeeId} not found.");
        }

        // Calculate payslip components
        var calculation = await _calculationService.CalculatePayslipAsync(employeeId, payPeriodStart, payPeriodEnd);

        // Create payslip
        var payslip = new Payslip
        {
            PayslipNumber = await GeneratePayslipNumberAsync(employeeId, payPeriodStart),
            EmployeeId = employeeId,
            PayPeriodStart = payPeriodStart,
            PayPeriodEnd = payPeriodEnd,
            PayDate = payPeriodEnd.AddDays(5), // Pay 5 days after period end
            BasicSalary = calculation.BasicSalary,
            GrossSalary = calculation.GrossSalary,
            TotalAllowances = calculation.TotalAllowances,
            TotalDeductions = calculation.TotalDeductions,
            TaxAmount = calculation.TaxAmount,
            NetSalary = calculation.NetSalary,
            WorkingDays = calculation.WorkingDays,
            ActualWorkingDays = calculation.ActualWorkingDays,
            Status = PayslipStatus.Generated,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add payslip to context first
        context.Payslips.Add(payslip);
        
        // Add allowances to the payslip's collection
        foreach (var allowance in calculation.Allowances)
        {
            payslip.PayslipAllowances.Add(allowance);
        }

        // Add deductions to the payslip's collection
        foreach (var deduction in calculation.Deductions)
        {
            payslip.PayslipDeductions.Add(deduction);
        }

        // Save everything in one transaction with retry logic for duplicate key errors
        var maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await context.SaveChangesAsync();
                break; // Success, exit retry loop
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2601)
            {
                if (attempt == maxRetries)
                    throw new InvalidOperationException($"Unable to generate unique payslip number after {maxRetries} attempts. Please try again.");
                
                // Regenerate payslip number and try again
                payslip.PayslipNumber = await GeneratePayslipNumberAsync(employeeId, payPeriodStart);
            }
        }

        // Load the complete payslip with navigation properties
        return await GetPayslipByIdAsync(payslip.Id) ?? payslip;
    }

    public async Task<Payslip?> GetPayslipByIdAsync(int payslipId)
    {
        using var context = _contextFactory.CreateDbContext();
        
        return await context.Payslips
            .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
            .Include(p => p.Employee)
                .ThenInclude(e => e.Company)
            .Include(p => p.PayslipAllowances)
                .ThenInclude(pa => pa.AllowanceType)
            .Include(p => p.PayslipDeductions)
                .ThenInclude(pd => pd.DeductionType)
            .FirstOrDefaultAsync(p => p.Id == payslipId);
    }

    public async Task<List<Payslip>> GetPayslipsByEmployeeAsync(int employeeId, int? year = null, int? month = null)
    {
        using var context = _contextFactory.CreateDbContext();
        
        var query = context.Payslips
            .Include(p => p.Employee)
            .Include(p => p.PayslipAllowances)
                .ThenInclude(pa => pa.AllowanceType)
            .Include(p => p.PayslipDeductions)
                .ThenInclude(pd => pd.DeductionType)
            .Where(p => p.EmployeeId == employeeId);

        if (year.HasValue)
        {
            query = query.Where(p => p.PayPeriodStart.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(p => p.PayPeriodStart.Month == month.Value);
        }

        return await query
            .OrderByDescending(p => p.PayPeriodStart)
            .ToListAsync();
    }

    public async Task<List<Payslip>> GetPayslipsForPeriodAsync(DateTime startDate, DateTime endDate)
    {
        using var context = _contextFactory.CreateDbContext();
        
        return await context.Payslips
            .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
            .Where(p => p.PayPeriodStart >= startDate && p.PayPeriodEnd <= endDate)
            .OrderBy(p => p.Employee.FirstName)
            .ThenBy(p => p.Employee.LastName)
            .ToListAsync();
    }

    public async Task<Payslip> ApprovePayslipAsync(int payslipId, string approvedBy)
    {
        using var context = _contextFactory.CreateDbContext();
        
        var payslip = await context.Payslips.FindAsync(payslipId);
        if (payslip == null)
        {
            throw new ArgumentException($"Payslip with ID {payslipId} not found.");
        }

        if (payslip.Status != PayslipStatus.Generated)
        {
            throw new InvalidOperationException($"Only generated payslips can be approved. Current status: {payslip.Status}");
        }

        payslip.Status = PayslipStatus.Approved;
        payslip.ApprovedBy = approvedBy;
        payslip.ApprovedAt = DateTime.UtcNow;
        payslip.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return payslip;
    }

    public async Task<Payslip> UpdatePayslipStatusAsync(int payslipId, PayslipStatus status)
    {
        using var context = _contextFactory.CreateDbContext();
        
        var payslip = await context.Payslips.FindAsync(payslipId);
        if (payslip == null)
        {
            throw new ArgumentException($"Payslip with ID {payslipId} not found.");
        }

        payslip.Status = status;
        payslip.UpdatedAt = DateTime.UtcNow;

        if (status == PayslipStatus.Sent)
        {
            payslip.EmailSentAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        return payslip;
    }

    public async Task<bool> DeletePayslipAsync(int payslipId)
    {
        using var context = _contextFactory.CreateDbContext();
        
        var payslip = await context.Payslips
            .Include(p => p.PayslipAllowances)
            .Include(p => p.PayslipDeductions)
            .FirstOrDefaultAsync(p => p.Id == payslipId);

        if (payslip == null)
        {
            return false;
        }

        if (payslip.Status == PayslipStatus.Approved || payslip.Status == PayslipStatus.Paid)
        {
            throw new InvalidOperationException($"Cannot delete payslip with status: {payslip.Status}");
        }

        // Remove related allowances and deductions
        context.PayslipAllowances.RemoveRange(payslip.PayslipAllowances);
        context.PayslipDeductions.RemoveRange(payslip.PayslipDeductions);
        context.Payslips.Remove(payslip);

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<string> GeneratePayslipNumberAsync(int employeeId, DateTime payPeriod)
    {
        using var context = _contextFactory.CreateDbContext();
        
        var employee = await context.Employees.FindAsync(employeeId);
        var employeeCode = employee?.EmployeeCode ?? employeeId.ToString("D6");
        
        var year = payPeriod.Year;
        var month = payPeriod.Month;
        
        // Use a more robust approach to avoid race conditions
        var baseNumber = $"PS-{employeeCode}-{year:D4}{month:D2}";
        
        // Find all existing payslip numbers for this employee and month
        var existingNumbers = await context.Payslips
            .Where(p => p.EmployeeId == employeeId 
                && p.PayPeriodStart.Year == year 
                && p.PayPeriodStart.Month == month)
            .Select(p => p.PayslipNumber)
            .ToListAsync();

        // Find the next available sequence number
        var sequence = 1;
        while (existingNumbers.Contains($"{baseNumber}-{sequence:D3}"))
        {
            sequence++;
        }

        return $"{baseNumber}-{sequence:D3}";
    }
}