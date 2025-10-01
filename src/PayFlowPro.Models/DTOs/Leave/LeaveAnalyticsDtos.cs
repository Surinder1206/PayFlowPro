using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Models.DTOs.Leave
{
    // Leave Analytics DTOs
    public class LeaveAnalyticsDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int FinancialYear { get; set; }
        public decimal TotalLeavesTaken { get; set; }
        public decimal TotalLeavesAllocated { get; set; }
        public decimal LeaveUtilizationPercentage { get; set; }
        public decimal AverageLeavePerMonth { get; set; }
        public int TotalLeaveRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int PendingRequests { get; set; }
        public List<MonthlyLeaveUsageDto> MonthlyUsage { get; set; } = new();
        public List<LeaveTypeUsageDto> LeaveTypeUsage { get; set; } = new();
    }

    public class MonthlyLeaveUsageDto
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal DaysTaken { get; set; }
        public int RequestCount { get; set; }
    }

    public class LeaveTypeUsageDto
    {
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public string ColorCode { get; set; } = "#007bff";
        public decimal DaysTaken { get; set; }
        public decimal DaysAllocated { get; set; }
        public decimal UsagePercentage { get; set; }
        public int RequestCount { get; set; }
    }

    // Department Leave Analytics
    public class DepartmentLeaveAnalyticsDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalEmployees { get; set; }
        public decimal TotalLeavesTaken { get; set; }
        public decimal TotalLeavesAllocated { get; set; }
        public decimal AverageLeavePerEmployee { get; set; }
        public decimal DepartmentUtilizationRate { get; set; }
        public List<EmployeeLeaveUsageDto> EmployeeUsage { get; set; } = new();
        public List<LeaveTypeUsageDto> LeaveTypeBreakdown { get; set; } = new();
    }

    public class EmployeeLeaveUsageDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public decimal DaysTaken { get; set; }
        public decimal DaysAllocated { get; set; }
        public decimal UsagePercentage { get; set; }
        public int RequestCount { get; set; }
    }

    // Leave Balance Management DTOs
    public class LeaveBalanceDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string LeaveTypeName { get; set; } = string.Empty;
        public string LeaveTypeColor { get; set; } = "#007bff";
        public int FinancialYear { get; set; }
        public decimal AllocatedDays { get; set; }
        public decimal UsedDays { get; set; }
        public decimal CarriedOverDays { get; set; }
        public decimal ExpiringDays { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal AccruedDays { get; set; }
        public decimal PendingDays { get; set; }
        public decimal AvailableDays { get; set; }
        public DateTime? LastAccrualProcessed { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class UpdateLeaveBalanceDto
    {
        [Required]
        public int Id { get; set; }
        
        [Required]
        [Range(0, 9999)]
        public decimal AllocatedDays { get; set; }
        
        [Range(0, 9999)]
        public decimal CarriedOverDays { get; set; }
        
        [Range(0, 9999)]
        public decimal ExpiringDays { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }
        
        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;
    }

    public class CreateLeaveBalanceDto
    {
        [Required]
        public int EmployeeId { get; set; }
        
        [Required]
        public int LeaveTypeId { get; set; }
        
        [Required]
        public int FinancialYear { get; set; }
        
        [Required]
        [Range(0, 9999)]
        public decimal AllocatedDays { get; set; }
        
        [Range(0, 9999)]
        public decimal CarriedOverDays { get; set; }
        
        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;
    }

    // Leave Accrual DTOs
    public class LeaveAccrualDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public decimal AccrualRate { get; set; }
        public string AccrualFrequency { get; set; } = string.Empty;
        public decimal AccruedThisMonth { get; set; }
        public decimal AccruedThisYear { get; set; }
        public decimal TotalAccrued { get; set; }
        public DateTime LastProcessed { get; set; }
        public DateTime NextAccrualDate { get; set; }
    }

    public class ProcessLeaveAccrualDto
    {
        [Required]
        public int FinancialYear { get; set; }
        
        [Required]
        public int Month { get; set; }
        
        public List<int> EmployeeIds { get; set; } = new();
        public List<int> LeaveTypeIds { get; set; } = new();
        public bool ForceReprocess { get; set; }
    }

    // Leave Report DTOs
    public class LeaveReportFilterDto
    {
        public List<int> EmployeeIds { get; set; } = new();
        public List<int> DepartmentIds { get; set; } = new();
        public List<int> LeaveTypeIds { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<string> StatusList { get; set; } = new();
        public int FinancialYear { get; set; }
        public string ReportType { get; set; } = string.Empty;
    }

    public class LeaveReportDto
    {
        public string ReportTitle { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public LeaveReportFilterDto Filters { get; set; } = new();
        public LeaveReportSummaryDto Summary { get; set; } = new();
        public List<LeaveRequestDto> LeaveRequests { get; set; } = new();
        public List<LeaveBalanceDto> LeaveBalances { get; set; } = new();
        public List<DepartmentLeaveAnalyticsDto> DepartmentAnalytics { get; set; } = new();
    }

    public class LeaveReportSummaryDto
    {
        public int TotalEmployees { get; set; }
        public int TotalLeaveRequests { get; set; }
        public decimal TotalLeavesTaken { get; set; }
        public decimal TotalLeavesAllocated { get; set; }
        public decimal OverallUtilizationRate { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int PendingRequests { get; set; }
        public Dictionary<string, decimal> LeaveTypeBreakdown { get; set; } = new();
        public Dictionary<string, decimal> DepartmentBreakdown { get; set; } = new();
    }

    // Service Response DTOs
    public class LeaveValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public decimal CalculatedDays { get; set; }
        public decimal AvailableBalance { get; set; }
        public bool RequiresApproval { get; set; }
        public List<string> ApprovalLevels { get; set; } = new();
    }

    public class LeaveCalendarDto
    {
        public DateTime Date { get; set; }
        public List<LeaveCalendarEventDto> Events { get; set; } = new();
        public bool IsWorkingDay { get; set; }
        public bool IsPublicHoliday { get; set; }
        public string PublicHolidayName { get; set; } = string.Empty;
    }

    public class LeaveCalendarEventDto
    {
        public int LeaveRequestId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string LeaveTypeName { get; set; } = string.Empty;
        public string LeaveTypeColor { get; set; } = "#007bff";
        public string Status { get; set; } = string.Empty;
        public bool IsHalfDay { get; set; }
        public string HalfDaySession { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}