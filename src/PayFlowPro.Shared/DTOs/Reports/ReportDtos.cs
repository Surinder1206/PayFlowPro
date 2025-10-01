namespace PayFlowPro.Shared.DTOs.Reports
{
    /// <summary>
    /// Payroll summary report for a specific period
    /// </summary>
    public class PayrollSummaryReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public decimal TotalGrossSalary { get; set; }
        public decimal TotalNetSalary { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal AverageGrossSalary { get; set; }
        public decimal AverageNetSalary { get; set; }
        public List<DepartmentPayrollSummaryDto> DepartmentSummaries { get; set; } = new();
        public List<MonthlyPayrollTrendDto> MonthlyTrends { get; set; } = new();
    }

    /// <summary>
    /// Department-wise payroll summary
    /// </summary>
    public class DepartmentPayrollSummaryDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public decimal TotalGrossSalary { get; set; }
        public decimal TotalNetSalary { get; set; }
        public decimal AverageGrossSalary { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal DeductionPercentage { get; set; }
    }

    /// <summary>
    /// Monthly payroll trend data
    /// </summary>
    public class MonthlyPayrollTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalGrossSalary { get; set; }
        public decimal TotalNetSalary { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public int EmployeeCount { get; set; }
        public decimal AverageSalary { get; set; }
    }

    /// <summary>
    /// Employee salary report
    /// </summary>
    public class EmployeeSalaryReportDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public DateTime DateOfJoining { get; set; }
        public decimal CurrentBasicSalary { get; set; }
        public decimal CurrentGrossSalary { get; set; }
        public decimal CurrentNetSalary { get; set; }
        public decimal YearToDateGross { get; set; }
        public decimal YearToDateNet { get; set; }
        public decimal YearToDateTax { get; set; }
        public List<MonthlySalaryDto> MonthlySalaries { get; set; } = new();
    }

    /// <summary>
    /// Monthly salary breakdown for an employee
    /// </summary>
    public class MonthlySalaryDto
    {
        public int PayslipId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal NetSalary { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal TotalDeductions { get; set; }
        public int WorkingDays { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Tax summary report
    /// </summary>
    public class TaxSummaryReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalTaxCollected { get; set; }
        public decimal AverageTaxRate { get; set; }
        public int TaxableEmployees { get; set; }
        public List<TaxBracketSummaryDto> TaxBrackets { get; set; } = new();
        public List<EmployeeTaxSummaryDto> EmployeeTaxSummaries { get; set; } = new();
    }

    /// <summary>
    /// Tax bracket summary
    /// </summary>
    public class TaxBracketSummaryDto
    {
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
        public decimal TaxRate { get; set; }
        public int EmployeeCount { get; set; }
        public decimal TotalTaxInBracket { get; set; }
    }

    /// <summary>
    /// Employee tax summary
    /// </summary>
    public class EmployeeTaxSummaryDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public decimal YearToDateGross { get; set; }
        public decimal YearToDateTax { get; set; }
        public decimal EffectiveTaxRate { get; set; }
        public decimal EstimatedAnnualTax { get; set; }
    }

    /// <summary>
    /// Attendance and payroll correlation report
    /// </summary>
    public class AttendancePayrollReportDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int TotalWorkingDays { get; set; }
        public int ActualWorkingDays { get; set; }
        public decimal AttendancePercentage { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal NetSalary { get; set; }
        public decimal SalaryPerDay { get; set; }
        public decimal DeductedAmount { get; set; }
    }

    /// <summary>
    /// Compensation analysis report
    /// </summary>
    public class CompensationAnalysisDto
    {
        public string JobTitle { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public decimal MinimumSalary { get; set; }
        public decimal MaximumSalary { get; set; }
        public decimal AverageSalary { get; set; }
        public decimal MedianSalary { get; set; }
        public decimal StandardDeviation { get; set; }
        public decimal MarketRate { get; set; }
        public decimal CompetitiveRatio { get; set; }
    }
}