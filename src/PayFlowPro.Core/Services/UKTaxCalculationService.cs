using PayFlowPro.Core.Interfaces;
using PayFlowPro.Models.Enums;

namespace PayFlowPro.Core.Services;

public class UKTaxCalculationService : IUKTaxCalculationService
{
    // UK Tax Year 2024-25 Constants
    private const decimal PERSONAL_ALLOWANCE_2024_25 = 12570m;
    private const decimal BASIC_RATE_THRESHOLD = 50270m; // Personal Allowance + Basic Rate Band (£37,700)
    private const decimal HIGHER_RATE_THRESHOLD = 125140m; // Where additional rate starts
    
    // Tax Rates 2024-25
    private const decimal BASIC_RATE = 0.20m; // 20%
    private const decimal HIGHER_RATE = 0.40m; // 40%
    private const decimal ADDITIONAL_RATE = 0.45m; // 45%
    
    // National Insurance 2024-25
    private const decimal NI_LOWER_EARNINGS_LIMIT = 12570m; // Same as personal allowance
    private const decimal NI_UPPER_EARNINGS_LIMIT = 50270m; // Where rate reduces
    private const decimal NI_STANDARD_RATE = 0.12m; // 12% between LEL and UEL
    private const decimal NI_REDUCED_RATE = 0.02m; // 2% above UEL
    
    // Personal Allowance Tapering
    private const decimal TAPERING_START = 100000m; // Income level where tapering starts
    private const decimal TAPERING_RATE = 0.50m; // £1 reduction per £2 of income above threshold

    public decimal CalculateIncomeTax(decimal annualGrossSalary, string taxCode = "1257L", PayFrequency payFrequency = PayFrequency.Monthly)
    {
        var personalAllowance = GetPersonalAllowance(annualGrossSalary);
        var taxableIncome = Math.Max(0, annualGrossSalary - personalAllowance);
        
        decimal annualTax = 0;
        
        if (taxableIncome > 0)
        {
            // Basic Rate (20%) - up to £37,700 above personal allowance
            var basicRateBand = Math.Min(taxableIncome, BASIC_RATE_THRESHOLD - personalAllowance);
            annualTax += basicRateBand * BASIC_RATE;
            
            // Higher Rate (40%) - £37,701 to £125,140
            if (taxableIncome > (BASIC_RATE_THRESHOLD - personalAllowance))
            {
                var higherRateIncome = taxableIncome - (BASIC_RATE_THRESHOLD - personalAllowance);
                var higherRateBand = Math.Min(higherRateIncome, HIGHER_RATE_THRESHOLD - BASIC_RATE_THRESHOLD);
                annualTax += higherRateBand * HIGHER_RATE;
                
                // Additional Rate (45%) - above £125,140
                if (taxableIncome > (HIGHER_RATE_THRESHOLD - personalAllowance))
                {
                    var additionalRateIncome = taxableIncome - (HIGHER_RATE_THRESHOLD - personalAllowance);
                    annualTax += additionalRateIncome * ADDITIONAL_RATE;
                }
            }
        }
        
        return ConvertToPayPeriod(annualTax, payFrequency);
    }
    
    public decimal CalculateNationalInsurance(decimal annualGrossSalary, PayFrequency payFrequency = PayFrequency.Monthly)
    {
        decimal annualNI = 0;
        
        if (annualGrossSalary > NI_LOWER_EARNINGS_LIMIT)
        {
            // Standard Rate (12%) - between £12,570 and £50,270
            var standardRateEarnings = Math.Min(annualGrossSalary - NI_LOWER_EARNINGS_LIMIT, 
                                              NI_UPPER_EARNINGS_LIMIT - NI_LOWER_EARNINGS_LIMIT);
            annualNI += standardRateEarnings * NI_STANDARD_RATE;
            
            // Reduced Rate (2%) - above £50,270
            if (annualGrossSalary > NI_UPPER_EARNINGS_LIMIT)
            {
                var reducedRateEarnings = annualGrossSalary - NI_UPPER_EARNINGS_LIMIT;
                annualNI += reducedRateEarnings * NI_REDUCED_RATE;
            }
        }
        
        return ConvertToPayPeriod(annualNI, payFrequency);
    }
    
    public UKTaxCalculationResult CalculateUKTaxDeductions(decimal annualGrossSalary, string taxCode = "1257L", PayFrequency payFrequency = PayFrequency.Monthly)
    {
        var personalAllowance = GetPersonalAllowance(annualGrossSalary);
        var taxableIncome = Math.Max(0, annualGrossSalary - personalAllowance);
        var grossForPeriod = ConvertToPayPeriod(annualGrossSalary, payFrequency);
        
        var result = new UKTaxCalculationResult
        {
            GrossSalaryForPeriod = grossForPeriod,
            AnnualGrossSalary = annualGrossSalary,
            PersonalAllowance = personalAllowance,
            TaxableIncome = taxableIncome,
            TaxCode = taxCode,
            PayFrequency = payFrequency
        };
        
        // Calculate Income Tax breakdown
        decimal annualTax = 0;
        
        if (taxableIncome > 0)
        {
            // Basic Rate
            var basicRateBand = Math.Min(taxableIncome, BASIC_RATE_THRESHOLD - personalAllowance);
            result.BasicRateTax = ConvertToPayPeriod(basicRateBand * BASIC_RATE, payFrequency);
            annualTax += basicRateBand * BASIC_RATE;
            
            // Higher Rate
            if (taxableIncome > (BASIC_RATE_THRESHOLD - personalAllowance))
            {
                var higherRateIncome = taxableIncome - (BASIC_RATE_THRESHOLD - personalAllowance);
                var higherRateBand = Math.Min(higherRateIncome, HIGHER_RATE_THRESHOLD - BASIC_RATE_THRESHOLD);
                result.HigherRateTax = ConvertToPayPeriod(higherRateBand * HIGHER_RATE, payFrequency);
                annualTax += higherRateBand * HIGHER_RATE;
                
                // Additional Rate
                if (taxableIncome > (HIGHER_RATE_THRESHOLD - personalAllowance))
                {
                    var additionalRateIncome = taxableIncome - (HIGHER_RATE_THRESHOLD - personalAllowance);
                    result.AdditionalRateTax = ConvertToPayPeriod(additionalRateIncome * ADDITIONAL_RATE, payFrequency);
                    annualTax += additionalRateIncome * ADDITIONAL_RATE;
                }
            }
        }
        
        result.IncomeTax = ConvertToPayPeriod(annualTax, payFrequency);
        
        // Calculate National Insurance breakdown
        decimal annualNI = 0;
        
        if (annualGrossSalary > NI_LOWER_EARNINGS_LIMIT)
        {
            // Standard Rate
            var standardRateEarnings = Math.Min(annualGrossSalary - NI_LOWER_EARNINGS_LIMIT, 
                                              NI_UPPER_EARNINGS_LIMIT - NI_LOWER_EARNINGS_LIMIT);
            result.NILowerRate = ConvertToPayPeriod(standardRateEarnings * NI_STANDARD_RATE, payFrequency);
            annualNI += standardRateEarnings * NI_STANDARD_RATE;
            
            // Reduced Rate
            if (annualGrossSalary > NI_UPPER_EARNINGS_LIMIT)
            {
                var reducedRateEarnings = annualGrossSalary - NI_UPPER_EARNINGS_LIMIT;
                result.NIHigherRate = ConvertToPayPeriod(reducedRateEarnings * NI_REDUCED_RATE, payFrequency);
                annualNI += reducedRateEarnings * NI_REDUCED_RATE;
            }
        }
        
        result.NationalInsurance = ConvertToPayPeriod(annualNI, payFrequency);
        
        return result;
    }
    
    public decimal GetPersonalAllowance(decimal annualGrossSalary)
    {
        if (annualGrossSalary <= TAPERING_START)
        {
            return PERSONAL_ALLOWANCE_2024_25;
        }
        
        // Calculate tapered allowance
        var excessIncome = annualGrossSalary - TAPERING_START;
        var allowanceReduction = Math.Floor(excessIncome / 2); // £1 reduction per £2 of excess income
        var taperedAllowance = PERSONAL_ALLOWANCE_2024_25 - allowanceReduction;
        
        // Personal allowance cannot go below zero
        return Math.Max(0, taperedAllowance);
    }
    
    private static decimal ConvertToPayPeriod(decimal annualAmount, PayFrequency payFrequency)
    {
        return payFrequency switch
        {
            PayFrequency.Monthly => Math.Round(annualAmount / 12, 2, MidpointRounding.AwayFromZero),
            PayFrequency.Weekly => Math.Round(annualAmount / 52, 2, MidpointRounding.AwayFromZero),
            PayFrequency.BiWeekly => Math.Round(annualAmount / 26, 2, MidpointRounding.AwayFromZero),
            PayFrequency.Quarterly => Math.Round(annualAmount / 4, 2, MidpointRounding.AwayFromZero),
            PayFrequency.Annual => Math.Round(annualAmount, 2, MidpointRounding.AwayFromZero),
            _ => Math.Round(annualAmount / 12, 2, MidpointRounding.AwayFromZero) // Default to monthly
        };
    }
}