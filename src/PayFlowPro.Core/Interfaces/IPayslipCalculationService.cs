using PayFlowPro.Models.Entities;

namespace PayFlowPro.Core.Interfaces;

public interface IPayslipCalculationService
{
    Task<PayslipCalculationResult> CalculatePayslipAsync(int employeeId, DateTime payPeriodStart, DateTime payPeriodEnd);
    Task<decimal> CalculateTaxAsync(decimal grossSalary, int employeeId);
    Task<decimal> CalculateGrossSalaryAsync(decimal basicSalary, List<PayslipAllowance> allowances);
    Task<decimal> CalculateNetSalaryAsync(decimal grossSalary, decimal totalDeductions, decimal taxAmount);
}

public class PayslipCalculationResult
{
    public decimal BasicSalary { get; set; }
    public decimal TotalAllowances { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal NationalInsurance { get; set; }
    public decimal TotalTax => IncomeTax + NationalInsurance;
    public decimal NetSalary { get; set; }
    public int WorkingDays { get; set; }
    public int ActualWorkingDays { get; set; }
    public List<PayslipAllowance> Allowances { get; set; } = new();
    public List<PayslipDeduction> Deductions { get; set; } = new();
}