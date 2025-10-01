using PayFlowPro.Shared.DTOs.Reports;

namespace PayFlowPro.Core.Interfaces
{
    /// <summary>
    /// Interface for report and analytics services
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Generate payroll summary report for a specific period
        /// </summary>
        Task<PayrollSummaryReportDto> GetPayrollSummaryAsync(PayrollSummaryFilterDto filter);

        /// <summary>
        /// Generate employee salary reports with filtering options
        /// </summary>
        Task<List<EmployeeSalaryReportDto>> GetEmployeeSalaryReportsAsync(EmployeeSalaryFilterDto filter);

        /// <summary>
        /// Generate tax summary report for tax analysis
        /// </summary>
        Task<TaxSummaryReportDto> GetTaxSummaryAsync(TaxSummaryFilterDto filter);

        /// <summary>
        /// Generate attendance and payroll correlation report
        /// </summary>
        Task<List<AttendancePayrollReportDto>> GetAttendancePayrollReportAsync(AttendancePayrollFilterDto filter);

        /// <summary>
        /// Generate compensation analysis report by job title/department
        /// </summary>
        Task<List<CompensationAnalysisDto>> GetCompensationAnalysisAsync(CompensationAnalysisFilterDto filter);

        /// <summary>
        /// Get dashboard summary data for main reports page
        /// </summary>
        Task<DashboardSummaryDto> GetDashboardSummaryAsync();

        /// <summary>
        /// Get chart data for various visualizations
        /// </summary>
        Task<ChartDataDto> GetChartDataAsync(ChartDataRequestDto request);

        /// <summary>
        /// Generate department-wise payroll comparison
        /// </summary>
        Task<List<DepartmentPayrollSummaryDto>> GetDepartmentComparisonAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get payroll trends over time
        /// </summary>
        Task<List<MonthlyPayrollTrendDto>> GetPayrollTrendsAsync(DateTime fromDate, DateTime toDate, ReportGroupBy groupBy = ReportGroupBy.Month);

        /// <summary>
        /// Get top earning employees
        /// </summary>
        Task<List<EmployeeSalaryReportDto>> GetTopEarnersAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get employees with salary changes
        /// </summary>
        Task<List<EmployeeSalaryReportDto>> GetSalaryChangesAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Generate custom report based on parameters
        /// </summary>
        Task<object> GenerateCustomReportAsync(string reportType, Dictionary<string, object> parameters);

        /// <summary>
        /// Export report to specified format
        /// </summary>
        Task<byte[]> ExportReportAsync(ReportExportRequestDto request);

        /// <summary>
        /// Get report data for Excel export
        /// </summary>
        Task<byte[]> ExportToExcelAsync<T>(List<T> data, string sheetName, Dictionary<string, string>? columnHeaders = null);

        /// <summary>
        /// Get report data for CSV export
        /// </summary>
        Task<byte[]> ExportToCsvAsync<T>(List<T> data, Dictionary<string, string>? columnHeaders = null);

        /// <summary>
        /// Schedule recurring report generation
        /// </summary>
        Task<bool> ScheduleReportAsync(string reportType, object parameters, string cronExpression, string recipientEmails);

        /// <summary>
        /// Get available report templates
        /// </summary>
        Task<List<ReportTemplateDto>> GetReportTemplatesAsync();

        /// <summary>
        /// Validate report parameters
        /// </summary>
        Task<ReportValidationResult> ValidateReportParametersAsync(string reportType, object parameters);
    }

    /// <summary>
    /// Report template definition
    /// </summary>
    public class ReportTemplateDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public List<ReportParameterDto> Parameters { get; set; } = new();
        public bool IsCustomizable { get; set; }
        public List<ExportFormat> SupportedFormats { get; set; } = new();
    }

    /// <summary>
    /// Report parameter definition
    /// </summary>
    public class ReportParameterDto
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "string", "number", "date", "boolean", "select"
        public bool IsRequired { get; set; }
        public object? DefaultValue { get; set; }
        public List<object>? Options { get; set; }
        public string? ValidationRule { get; set; }
    }

    /// <summary>
    /// Report validation result
    /// </summary>
    public class ReportValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}