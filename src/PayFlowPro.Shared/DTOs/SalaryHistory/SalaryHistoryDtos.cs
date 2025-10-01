using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Shared.DTOs.SalaryHistory;

/// <summary>
/// DTO for salary history entry
/// </summary>
public class SalaryHistoryEntryDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public decimal PreviousSalary { get; set; }
    public decimal NewSalary { get; set; }
    public decimal SalaryIncrease { get; set; }
    public decimal IncreasePercentage { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for salary history summary
/// </summary>
public class SalaryHistorySummaryDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public decimal CurrentSalary { get; set; }
    public decimal InitialSalary { get; set; }
    public decimal TotalIncrease { get; set; }
    public decimal TotalIncreasePercentage { get; set; }
    public int NumberOfIncreases { get; set; }
    public DateTime LastIncreaseDate { get; set; }
    public DateTime JoiningDate { get; set; }
    public int YearsOfService { get; set; }
    public decimal AverageAnnualIncrease { get; set; }
    public List<SalaryHistoryEntryDto> History { get; set; } = new();
}

/// <summary>
/// DTO for salary analytics
/// </summary>
public class SalaryAnalyticsDto
{
    public decimal AverageSalary { get; set; }
    public decimal MedianSalary { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public decimal AverageIncrease { get; set; }
    public decimal MedianIncrease { get; set; }
    public List<SalaryTrendDto> SalaryTrends { get; set; } = new();
    public List<DepartmentSalaryDto> DepartmentAverages { get; set; } = new();
    public List<YearlySalaryStatsDto> YearlyStats { get; set; } = new();
}

/// <summary>
/// DTO for salary trend data
/// </summary>
public class SalaryTrendDto
{
    public DateTime Date { get; set; }
    public decimal AverageSalary { get; set; }
    public int EmployeeCount { get; set; }
    public decimal TotalPayroll { get; set; }
}

/// <summary>
/// DTO for department salary statistics
/// </summary>
public class DepartmentSalaryDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public decimal AverageSalary { get; set; }
    public decimal MedianSalary { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public int EmployeeCount { get; set; }
    public decimal TotalPayroll { get; set; }
}

/// <summary>
/// DTO for yearly salary statistics
/// </summary>
public class YearlySalaryStatsDto
{
    public int Year { get; set; }
    public decimal AverageSalary { get; set; }
    public decimal MedianSalary { get; set; }
    public decimal TotalPayroll { get; set; }
    public int EmployeeCount { get; set; }
    public decimal AverageIncrease { get; set; }
    public int NumberOfIncreases { get; set; }
}

/// <summary>
/// DTO for creating a new salary history entry
/// </summary>
public class CreateSalaryHistoryDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Previous salary must be greater than 0")]
    public decimal PreviousSalary { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "New salary must be greater than 0")]
    public decimal NewSalary { get; set; }

    [Required]
    public DateTime EffectiveDate { get; set; } = DateTime.Now;

    [Required]
    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MaxLength(100)]
    public string ApprovedBy { get; set; } = string.Empty;
}

/// <summary>
/// DTO for salary comparison data
/// </summary>
public class SalaryComparisonDto
{
    public string ComparisonType { get; set; } = string.Empty; // Department, Experience Level, etc.
    public string EmployeeName { get; set; } = string.Empty;
    public decimal EmployeeSalary { get; set; }
    public decimal ComparisonAverage { get; set; }
    public decimal DifferenceAmount { get; set; }
    public decimal DifferencePercentage { get; set; }
    public string Position { get; set; } = string.Empty; // Above/Below average
}

/// <summary>
/// DTO for salary projection data
/// </summary>
public class SalaryProjectionDto
{
    public int Year { get; set; }
    public decimal ProjectedSalary { get; set; }
    public decimal ProjectedIncrease { get; set; }
    public decimal ProjectedIncreasePercentage { get; set; }
    public string ProjectionBasis { get; set; } = string.Empty; // Historical average, performance, etc.
}