using Microsoft.EntityFrameworkCore;
using PayFlowPro.Core.Interfaces;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.Entities;
using PayFlowPro.Models.Enums;

namespace PayFlowPro.Core.Services;

public class PayslipCalculationService : IPayslipCalculationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IUKTaxCalculationService _ukTaxService;

    public PayslipCalculationService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IUKTaxCalculationService ukTaxService)
    {
        _contextFactory = contextFactory;
        _ukTaxService = ukTaxService;
    }

    public async Task<PayslipCalculationResult> CalculatePayslipAsync(int employeeId, DateTime payPeriodStart, DateTime payPeriodEnd)
    {
        using var context = _contextFactory.CreateDbContext();
        
        var employee = await context.Employees
            .Include(e => e.EmployeeAllowances)
                .ThenInclude(ea => ea.AllowanceType)
            .Include(e => e.EmployeeDeductions)
                .ThenInclude(ed => ed.DeductionType)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
            throw new ArgumentException($"Employee with ID {employeeId} not found.");

        var result = new PayslipCalculationResult();
        
        // Calculate working days
        result.WorkingDays = CalculateWorkingDays(payPeriodStart, payPeriodEnd);
        result.ActualWorkingDays = result.WorkingDays; // For now, assume full attendance
        
        // Basic salary (pro-rated if needed)
        result.BasicSalary = employee.BasicSalary;
        
        // Calculate allowances
        result.Allowances = await CalculateAllowancesAsync(employee, result.BasicSalary, payPeriodStart, payPeriodEnd);
        result.TotalAllowances = result.Allowances.Sum(a => a.Amount);
        
        // Calculate gross salary
        result.GrossSalary = await CalculateGrossSalaryAsync(result.BasicSalary, result.Allowances);
        
        // Calculate deductions
        result.Deductions = await CalculateDeductionsAsync(employee, result.GrossSalary, payPeriodStart, payPeriodEnd);
        result.TotalDeductions = result.Deductions.Sum(d => d.Amount);
        
        // Calculate UK taxes (Income Tax and National Insurance)
        var ukTaxResult = await CalculateUKTaxesAsync(result.GrossSalary, employeeId);
        result.IncomeTax = ukTaxResult.IncomeTax;
        result.NationalInsurance = ukTaxResult.NationalInsurance;
        
        // Calculate net salary
        result.NetSalary = await CalculateNetSalaryAsync(result.GrossSalary, result.TotalDeductions, result.IncomeTax + result.NationalInsurance);
        
        return result;
    }

    public async Task<(decimal IncomeTax, decimal NationalInsurance)> CalculateUKTaxesAsync(decimal grossSalary, int employeeId)
    {
        // Convert monthly gross salary to annual for UK tax calculations
        decimal annualGrossSalary = grossSalary * 12;
        
        // Calculate UK taxes using the dedicated tax service for monthly period
        var ukTaxResult = _ukTaxService.CalculateUKTaxDeductions(annualGrossSalary, "1257L", PayFrequency.Monthly);
        
        // Return monthly amounts (already calculated for the monthly period)
        return (ukTaxResult.IncomeTax, ukTaxResult.NationalInsurance);
    }

    [Obsolete("Replaced with CalculateUKTaxesAsync for UK compliance")]
    public async Task<decimal> CalculateTaxAsync(decimal grossSalary, int employeeId)
    {
        // Legacy method - kept for backward compatibility
        var ukTaxes = await CalculateUKTaxesAsync(grossSalary, employeeId);
        return ukTaxes.IncomeTax + ukTaxes.NationalInsurance;
    }

    public async Task<decimal> CalculateGrossSalaryAsync(decimal basicSalary, List<PayslipAllowance> allowances)
    {
        var taxableAllowances = allowances.Where(a => a.IsTaxable).Sum(a => a.Amount);
        return basicSalary + taxableAllowances;
    }

    public async Task<decimal> CalculateNetSalaryAsync(decimal grossSalary, decimal totalDeductions, decimal taxAmount)
    {
        return Math.Round(grossSalary - totalDeductions - taxAmount, 2);
    }

    private async Task<List<PayslipAllowance>> CalculateAllowancesAsync(Employee employee, decimal basicSalary, DateTime startDate, DateTime endDate)
    {
        var allowances = new List<PayslipAllowance>();
        
        foreach (var empAllowance in employee.EmployeeAllowances.Where(ea => ea.IsActive 
            && ea.EffectiveFrom <= endDate 
            && (ea.EffectiveTo == null || ea.EffectiveTo >= startDate)))
        {
            var allowanceAmount = empAllowance.Percentage.HasValue 
                ? Math.Round(basicSalary * (empAllowance.Percentage.Value / 100), 2)
                : empAllowance.Amount;

            allowances.Add(new PayslipAllowance
            {
                AllowanceTypeId = empAllowance.AllowanceTypeId,
                Amount = allowanceAmount,
                IsTaxable = empAllowance.AllowanceType.IsTaxable,
                AllowanceType = empAllowance.AllowanceType
            });
        }
        
        return allowances;
    }

    private async Task<List<PayslipDeduction>> CalculateDeductionsAsync(Employee employee, decimal grossSalary, DateTime startDate, DateTime endDate)
    {
        var deductions = new List<PayslipDeduction>();
        
        foreach (var empDeduction in employee.EmployeeDeductions.Where(ed => ed.IsActive 
            && ed.EffectiveFrom <= endDate 
            && (ed.EffectiveTo == null || ed.EffectiveTo >= startDate)))
        {
            var deductionAmount = empDeduction.Percentage.HasValue 
                ? Math.Round(grossSalary * (empDeduction.Percentage.Value / 100), 2)
                : empDeduction.Amount;

            deductions.Add(new PayslipDeduction
            {
                DeductionTypeId = empDeduction.DeductionTypeId,
                Amount = deductionAmount,
                DeductionType = empDeduction.DeductionType
            });
        }
        
        return deductions;
    }

    private int CalculateWorkingDays(DateTime startDate, DateTime endDate)
    {
        int workingDays = 0;
        var currentDate = startDate;
        
        while (currentDate <= endDate)
        {
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }
            currentDate = currentDate.AddDays(1);
        }
        
        return workingDays;
    }
}