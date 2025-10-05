using PayFlowPro.Models.Enums;

namespace PayFlowPro.Core.Interfaces;

public interface IUKTaxCalculationService
{
    /// <summary>
    /// Calculate UK Income Tax for the 2024-25 tax year
    /// </summary>
    /// <param name="annualGrossSalary">Annual gross salary in pounds</param>
    /// <param name="taxCode">UK tax code (default 1257L for 2024-25)</param>
    /// <param name="payFrequency">Pay frequency (Monthly, Weekly, etc.)</param>
    /// <returns>Tax amount for the specified pay period</returns>
    decimal CalculateIncomeTax(decimal annualGrossSalary, string taxCode = "1257L", PayFrequency payFrequency = PayFrequency.Monthly);

    /// <summary>
    /// Calculate UK National Insurance contributions (Class 1) for 2024-25 tax year
    /// </summary>
    /// <param name="annualGrossSalary">Annual gross salary in pounds</param>
    /// <param name="payFrequency">Pay frequency (Monthly, Weekly, etc.)</param>
    /// <returns>National Insurance amount for the specified pay period</returns>
    decimal CalculateNationalInsurance(decimal annualGrossSalary, PayFrequency payFrequency = PayFrequency.Monthly);

    /// <summary>
    /// Calculate total UK deductions (Income Tax + National Insurance)
    /// </summary>
    /// <param name="annualGrossSalary">Annual gross salary in pounds</param>
    /// <param name="taxCode">UK tax code (default 1257L for 2024-25)</param>
    /// <param name="payFrequency">Pay frequency (Monthly, Weekly, etc.)</param>
    /// <returns>Breakdown of all UK tax deductions</returns>
    UKTaxCalculationResult CalculateUKTaxDeductions(decimal annualGrossSalary, string taxCode = "1257L", PayFrequency payFrequency = PayFrequency.Monthly);

    /// <summary>
    /// Get personal allowance for the current tax year
    /// </summary>
    /// <param name="annualGrossSalary">Annual gross salary to check for tapered allowance</param>
    /// <returns>Personal allowance amount</returns>
    decimal GetPersonalAllowance(decimal annualGrossSalary);
}

/// <summary>
/// Result of UK tax calculations
/// </summary>
public class UKTaxCalculationResult
{
    public decimal GrossSalaryForPeriod { get; set; }
    public decimal AnnualGrossSalary { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal NationalInsurance { get; set; }
    public decimal TotalDeductions => IncomeTax + NationalInsurance;
    public decimal NetSalary => GrossSalaryForPeriod - TotalDeductions;
    public decimal PersonalAllowance { get; set; }
    public decimal TaxableIncome { get; set; }
    public string TaxCode { get; set; } = string.Empty;
    public PayFrequency PayFrequency { get; set; }

    // Breakdown details
    public decimal BasicRateTax { get; set; }
    public decimal HigherRateTax { get; set; }
    public decimal AdditionalRateTax { get; set; }
    public decimal NILowerRate { get; set; }
    public decimal NIHigherRate { get; set; }
}