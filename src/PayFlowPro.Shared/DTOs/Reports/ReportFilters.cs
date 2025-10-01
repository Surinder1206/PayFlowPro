using PayFlowPro.Models.Enums;

namespace PayFlowPro.Shared.DTOs.Reports
{
    /// <summary>
    /// Base class for report filters
    /// </summary>
    public abstract class BaseReportFilter
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? DepartmentId { get; set; }
        public int? EmployeeId { get; set; }
    }

    /// <summary>
    /// Payroll summary report filter
    /// </summary>
    public class PayrollSummaryFilterDto : BaseReportFilter
    {
        public bool IncludeDepartmentBreakdown { get; set; } = true;
        public bool IncludeMonthlyTrends { get; set; } = true;
        public PayslipStatus? Status { get; set; }
        public ReportGroupBy GroupBy { get; set; } = ReportGroupBy.Month;
    }

    /// <summary>
    /// Employee salary report filter
    /// </summary>
    public class EmployeeSalaryFilterDto : BaseReportFilter
    {
        public string? EmployeeCode { get; set; }
        public string? SearchTerm { get; set; }
        public bool IncludeSalaryHistory { get; set; } = true;
        public int? MinSalary { get; set; }
        public int? MaxSalary { get; set; }
        public EmploymentStatus? EmploymentStatus { get; set; }
    }

    /// <summary>
    /// Tax summary report filter
    /// </summary>
    public class TaxSummaryFilterDto : BaseReportFilter
    {
        public bool IncludeTaxBrackets { get; set; } = true;
        public bool IncludeEmployeeDetails { get; set; } = true;
        public decimal? MinTaxAmount { get; set; }
        public decimal? MaxTaxAmount { get; set; }
    }

    /// <summary>
    /// Attendance payroll report filter
    /// </summary>
    public class AttendancePayrollFilterDto : BaseReportFilter
    {
        public decimal? MinAttendancePercentage { get; set; }
        public decimal? MaxAttendancePercentage { get; set; }
        public bool ShowOnlyPoorAttendance { get; set; }
    }

    /// <summary>
    /// Compensation analysis filter
    /// </summary>
    public class CompensationAnalysisFilterDto
    {
        public int? DepartmentId { get; set; }
        public string? JobTitle { get; set; }
        public int? MinExperience { get; set; }
        public int? MaxExperience { get; set; }
        public bool IncludeMarketComparison { get; set; } = false;
    }

    /// <summary>
    /// Chart data request
    /// </summary>
    public class ChartDataRequestDto
    {
        public string ChartType { get; set; } = string.Empty; // "line", "bar", "pie", "doughnut"
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? DepartmentId { get; set; }
        public string DataType { get; set; } = string.Empty; // "payroll", "tax", "attendance", "trends"
        public ReportGroupBy GroupBy { get; set; } = ReportGroupBy.Month;
    }

    /// <summary>
    /// Chart data response
    /// </summary>
    public class ChartDataDto
    {
        public List<string> Labels { get; set; } = new();
        public List<ChartDatasetDto> Datasets { get; set; } = new();
        public string Title { get; set; } = string.Empty;
        public string XAxisLabel { get; set; } = string.Empty;
        public string YAxisLabel { get; set; } = string.Empty;
    }

    /// <summary>
    /// Chart dataset
    /// </summary>
    public class ChartDatasetDto
    {
        public string Label { get; set; } = string.Empty;
        public List<decimal> Data { get; set; } = new();
        public string BackgroundColor { get; set; } = string.Empty;
        public string BorderColor { get; set; } = string.Empty;
        public int BorderWidth { get; set; } = 1;
        public bool Fill { get; set; } = false;
    }

    /// <summary>
    /// Report export request
    /// </summary>
    public class ReportExportRequestDto
    {
        public string ReportType { get; set; } = string.Empty;
        public string ExportFormat { get; set; } = string.Empty; // "excel", "csv", "pdf"
        public object FilterParameters { get; set; } = new();
        public string FileName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Dashboard summary data
    /// </summary>
    public class DashboardSummaryDto
    {
        public PayrollSummaryReportDto CurrentMonthPayroll { get; set; } = new();
        public PayrollSummaryReportDto PreviousMonthPayroll { get; set; } = new();
        public List<DepartmentPayrollSummaryDto> TopDepartments { get; set; } = new();
        public List<EmployeeSalaryReportDto> TopEarners { get; set; } = new();
        public ChartDataDto PayrollTrendChart { get; set; } = new();
        public ChartDataDto DepartmentComparisonChart { get; set; } = new();
        public decimal PayrollGrowthPercentage { get; set; }
        public decimal AveragePayrollIncrease { get; set; }
        public int NewEmployeesThisMonth { get; set; }
        public int EmployeesLeftThisMonth { get; set; }
    }

    /// <summary>
    /// Report grouping options
    /// </summary>
    public enum ReportGroupBy
    {
        Day,
        Week,
        Month,
        Quarter,
        Year,
        Department,
        JobTitle
    }

    /// <summary>
    /// Report export formats
    /// </summary>
    public enum ExportFormat
    {
        Excel,
        CSV,
        PDF,
        JSON
    }

    /// <summary>
    /// Report types
    /// </summary>
    public enum ReportType
    {
        PayrollSummary,
        EmployeeSalary,
        TaxSummary,
        AttendancePayroll,
        CompensationAnalysis,
        DepartmentAnalysis,
        PayrollTrends
    }
}