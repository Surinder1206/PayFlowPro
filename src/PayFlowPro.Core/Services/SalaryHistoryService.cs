using Microsoft.EntityFrameworkCore;
using PayFlowPro.Data.Context;
using PayFlowPro.Shared.DTOs.SalaryHistory;
using PayFlowPro.Models.Entities;
using PayFlowPro.Shared.DTOs;

namespace PayFlowPro.Core.Services;

/// <summary>
/// Interface for salary history service
/// </summary>
public interface ISalaryHistoryService
{
    Task<ServiceResponse<SalaryHistorySummaryDto>> GetEmployeeSalaryHistoryAsync(int employeeId);
    Task<ServiceResponse<List<SalaryHistoryEntryDto>>> GetSalaryHistoryEntriesAsync(int employeeId);
    Task<ServiceResponse<SalaryAnalyticsDto>> GetSalaryAnalyticsAsync();
    Task<ServiceResponse<List<SalaryComparisonDto>>> GetSalaryComparisonsAsync(int employeeId);
    Task<ServiceResponse<List<SalaryProjectionDto>>> GetSalaryProjectionsAsync(int employeeId);
    Task<ServiceResponse<SalaryHistoryEntryDto>> CreateSalaryHistoryEntryAsync(CreateSalaryHistoryDto dto);
    Task<ServiceResponse<List<DepartmentSalaryDto>>> GetDepartmentSalaryStatsAsync();
    Task<ServiceResponse<List<YearlySalaryStatsDto>>> GetYearlySalaryStatsAsync();
}

/// <summary>
/// Service for managing salary history and analytics
/// </summary>
public class SalaryHistoryService : ISalaryHistoryService
{
    private readonly ApplicationDbContext _context;

    public SalaryHistoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResponse<SalaryHistorySummaryDto>> GetEmployeeSalaryHistoryAsync(int employeeId)
    {
        try
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Company)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
            {
                return ServiceResponse<SalaryHistorySummaryDto>.Failure("Employee not found");
            }

            var salaryEntries = await _context.SalaryHistories
                .Where(sh => sh.EmployeeId == employeeId)
                .OrderBy(sh => sh.EffectiveDate)
                .ToListAsync();

            var historyEntries = salaryEntries.Select(MapToSalaryHistoryEntryDto).ToList();

            var initialSalary = salaryEntries.Any() ? salaryEntries.First().PreviousSalary : employee.BasicSalary;
            var currentSalary = employee.BasicSalary;
            var totalIncrease = currentSalary - initialSalary;
            var totalIncreasePercentage = initialSalary > 0 ? (totalIncrease / initialSalary) * 100 : 0;

            var yearsOfService = DateTime.Now.Year - employee.DateOfJoining.Year;
            var averageAnnualIncrease = yearsOfService > 0 ? totalIncrease / yearsOfService : 0;

            var summary = new SalaryHistorySummaryDto
            {
                EmployeeId = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                EmployeeName = employee.FullName,
                CurrentSalary = currentSalary,
                InitialSalary = initialSalary,
                TotalIncrease = totalIncrease,
                TotalIncreasePercentage = totalIncreasePercentage,
                NumberOfIncreases = salaryEntries.Count,
                LastIncreaseDate = salaryEntries.Any() ? salaryEntries.Last().EffectiveDate : employee.DateOfJoining,
                JoiningDate = employee.DateOfJoining,
                YearsOfService = yearsOfService,
                AverageAnnualIncrease = averageAnnualIncrease,
                History = historyEntries
            };

            return ServiceResponse<SalaryHistorySummaryDto>.Success(summary);
        }
        catch (Exception ex)
        {
            return ServiceResponse<SalaryHistorySummaryDto>.Failure($"Error retrieving salary history: {ex.Message}");
        }
    }

    public async Task<ServiceResponse<List<SalaryHistoryEntryDto>>> GetSalaryHistoryEntriesAsync(int employeeId)
    {
        try
        {
            var entries = await _context.SalaryHistories
                .Where(sh => sh.EmployeeId == employeeId)
                .Include(sh => sh.Employee)
                .OrderByDescending(sh => sh.EffectiveDate)
                .ToListAsync();

            var entryDtos = entries.Select(MapToSalaryHistoryEntryDto).ToList();
            return ServiceResponse<List<SalaryHistoryEntryDto>>.Success(entryDtos);
        }
        catch (Exception ex)
        {
            return ServiceResponse<List<SalaryHistoryEntryDto>>.Failure($"Error retrieving salary entries: {ex.Message}");
        }
    }

    public async Task<ServiceResponse<SalaryAnalyticsDto>> GetSalaryAnalyticsAsync()
    {
        try
        {
            var employees = await _context.Employees
                .Where(e => e.DateOfLeaving == null)
                .Include(e => e.Department)
                .ToListAsync();

            if (!employees.Any())
            {
                return ServiceResponse<SalaryAnalyticsDto>.Failure("No active employees found");
            }

            var salaries = employees.Select(e => e.BasicSalary).ToList();
            var sortedSalaries = salaries.OrderBy(s => s).ToList();

            var analytics = new SalaryAnalyticsDto
            {
                AverageSalary = salaries.Average(),
                MedianSalary = GetMedian(sortedSalaries),
                MinSalary = salaries.Min(),
                MaxSalary = salaries.Max()
            };

            // Get salary increases
            var increases = await _context.SalaryHistories
                .Select(sh => sh.SalaryIncrease)
                .ToListAsync();

            if (increases.Any())
            {
                var sortedIncreases = increases.OrderBy(i => i).ToList();
                analytics.AverageIncrease = increases.Average();
                analytics.MedianIncrease = GetMedian(sortedIncreases);
            }

            // Get department averages
            analytics.DepartmentAverages = await GetDepartmentSalaryStatsAsync()
                .ContinueWith(t => t.Result.IsSuccess ? t.Result.Data! : new List<DepartmentSalaryDto>());

            // Get yearly stats
            analytics.YearlyStats = await GetYearlySalaryStatsAsync()
                .ContinueWith(t => t.Result.IsSuccess ? t.Result.Data! : new List<YearlySalaryStatsDto>());

            return ServiceResponse<SalaryAnalyticsDto>.Success(analytics);
        }
        catch (Exception ex)
        {
            return ServiceResponse<SalaryAnalyticsDto>.Failure($"Error calculating salary analytics: {ex.Message}");
        }
    }

    public async Task<ServiceResponse<List<SalaryComparisonDto>>> GetSalaryComparisonsAsync(int employeeId)
    {
        try
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
            {
                return ServiceResponse<List<SalaryComparisonDto>>.Failure("Employee not found");
            }

            var comparisons = new List<SalaryComparisonDto>();

            // Department comparison
            if (employee.DepartmentId > 0)
            {
                var departmentAvg = await _context.Employees
                    .Where(e => e.DepartmentId == employee.DepartmentId && e.DateOfLeaving == null && e.Id != employeeId)
                    .AverageAsync(e => e.BasicSalary);

                if (departmentAvg > 0)
                {
                    var difference = employee.BasicSalary - departmentAvg;
                    var percentDifference = (difference / departmentAvg) * 100;

                    comparisons.Add(new SalaryComparisonDto
                    {
                        ComparisonType = "Department Average",
                        EmployeeName = employee.FullName,
                        EmployeeSalary = employee.BasicSalary,
                        ComparisonAverage = departmentAvg,
                        DifferenceAmount = difference,
                        DifferencePercentage = percentDifference,
                        Position = difference >= 0 ? "Above Average" : "Below Average"
                    });
                }
            }

            // Company-wide comparison
            var companyAvg = await _context.Employees
                .Where(e => e.CompanyId == employee.CompanyId && e.DateOfLeaving == null && e.Id != employeeId)
                .AverageAsync(e => e.BasicSalary);

            if (companyAvg > 0)
            {
                var difference = employee.BasicSalary - companyAvg;
                var percentDifference = (difference / companyAvg) * 100;

                comparisons.Add(new SalaryComparisonDto
                {
                    ComparisonType = "Company Average",
                    EmployeeName = employee.FullName,
                    EmployeeSalary = employee.BasicSalary,
                    ComparisonAverage = companyAvg,
                    DifferenceAmount = difference,
                    DifferencePercentage = percentDifference,
                    Position = difference >= 0 ? "Above Average" : "Below Average"
                });
            }

            return ServiceResponse<List<SalaryComparisonDto>>.Success(comparisons);
        }
        catch (Exception ex)
        {
            return ServiceResponse<List<SalaryComparisonDto>>.Failure($"Error calculating salary comparisons: {ex.Message}");
        }
    }

    public async Task<ServiceResponse<List<SalaryProjectionDto>>> GetSalaryProjectionsAsync(int employeeId)
    {
        try
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
            {
                return ServiceResponse<List<SalaryProjectionDto>>.Failure("Employee not found");
            }

            var salaryHistory = await _context.SalaryHistories
                .Where(sh => sh.EmployeeId == employeeId)
                .OrderBy(sh => sh.EffectiveDate)
                .ToListAsync();

            var projections = new List<SalaryProjectionDto>();
            var currentSalary = employee.BasicSalary;

            // Calculate average annual increase
            var averageIncreasePercentage = 0m;
            if (salaryHistory.Any())
            {
                averageIncreasePercentage = salaryHistory.Average(sh => sh.IncreasePercentage);
            }
            else
            {
                // Use company average if no history
                var companyAvgIncrease = await _context.SalaryHistories
                    .Join(_context.Employees, sh => sh.EmployeeId, e => e.Id, (sh, e) => new { sh.IncreasePercentage, e.CompanyId })
                    .Where(x => x.CompanyId == employee.CompanyId)
                    .AverageAsync(x => (decimal?)x.IncreasePercentage) ?? 3.0m; // Default 3% if no data

                averageIncreasePercentage = companyAvgIncrease;
            }

            // Generate 5-year projections
            for (int i = 1; i <= 5; i++)
            {
                var projectedIncrease = currentSalary * (averageIncreasePercentage / 100);
                currentSalary += projectedIncrease;

                projections.Add(new SalaryProjectionDto
                {
                    Year = DateTime.Now.Year + i,
                    ProjectedSalary = currentSalary,
                    ProjectedIncrease = projectedIncrease,
                    ProjectedIncreasePercentage = averageIncreasePercentage,
                    ProjectionBasis = salaryHistory.Any() ? "Historical Average" : "Company Average"
                });
            }

            return ServiceResponse<List<SalaryProjectionDto>>.Success(projections);
        }
        catch (Exception ex)
        {
            return ServiceResponse<List<SalaryProjectionDto>>.Failure($"Error calculating salary projections: {ex.Message}");
        }
    }

    public async Task<ServiceResponse<SalaryHistoryEntryDto>> CreateSalaryHistoryEntryAsync(CreateSalaryHistoryDto dto)
    {
        try
        {
            var employee = await _context.Employees.FindAsync(dto.EmployeeId);
            if (employee == null)
            {
                return ServiceResponse<SalaryHistoryEntryDto>.Failure("Employee not found");
            }

            var salaryIncrease = dto.NewSalary - dto.PreviousSalary;
            var increasePercentage = dto.PreviousSalary > 0 ? (salaryIncrease / dto.PreviousSalary) * 100 : 0;

            var salaryHistory = new SalaryHistory
            {
                EmployeeId = dto.EmployeeId,
                PreviousSalary = dto.PreviousSalary,
                NewSalary = dto.NewSalary,
                SalaryIncrease = salaryIncrease,
                IncreasePercentage = increasePercentage,
                EffectiveDate = dto.EffectiveDate,
                Reason = dto.Reason,
                Notes = dto.Notes,
                ApprovedBy = dto.ApprovedBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.SalaryHistories.Add(salaryHistory);

            // Update employee's current salary
            employee.BasicSalary = dto.NewSalary;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var entryDto = MapToSalaryHistoryEntryDto(salaryHistory);
            return ServiceResponse<SalaryHistoryEntryDto>.Success(entryDto);
        }
        catch (Exception ex)
        {
            return ServiceResponse<SalaryHistoryEntryDto>.Failure($"Error creating salary history entry: {ex.Message}");
        }
    }

    public async Task<ServiceResponse<List<DepartmentSalaryDto>>> GetDepartmentSalaryStatsAsync()
    {
        try
        {
            var departmentStats = await _context.Employees
                .Where(e => e.DateOfLeaving == null && e.Department != null)
                .GroupBy(e => new { e.DepartmentId, e.Department!.Name })
                .Select(g => new DepartmentSalaryDto
                {
                    DepartmentName = g.Key.Name,
                    AverageSalary = g.Average(e => e.BasicSalary),
                    MinSalary = g.Min(e => e.BasicSalary),
                    MaxSalary = g.Max(e => e.BasicSalary),
                    EmployeeCount = g.Count(),
                    TotalPayroll = g.Sum(e => e.BasicSalary),
                    MedianSalary = 0 // Will calculate separately
                })
                .ToListAsync();

            // Calculate median for each department
            foreach (var dept in departmentStats)
            {
                var salaries = await _context.Employees
                    .Where(e => e.Department!.Name == dept.DepartmentName && e.DateOfLeaving == null)
                    .Select(e => e.BasicSalary)
                    .OrderBy(s => s)
                    .ToListAsync();

                dept.MedianSalary = GetMedian(salaries);
            }

            return ServiceResponse<List<DepartmentSalaryDto>>.Success(departmentStats);
        }
        catch (Exception ex)
        {
            return ServiceResponse<List<DepartmentSalaryDto>>.Failure($"Error getting department salary stats: {ex.Message}");
        }
    }

    public async Task<ServiceResponse<List<YearlySalaryStatsDto>>> GetYearlySalaryStatsAsync()
    {
        try
        {
            var currentYear = DateTime.Now.Year;
            var years = Enumerable.Range(currentYear - 4, 5).ToList(); // Last 5 years

            var yearlyStats = new List<YearlySalaryStatsDto>();

            foreach (var year in years)
            {
                var employees = await _context.Employees
                    .Where(e => e.DateOfJoining.Year <= year && 
                               (e.DateOfLeaving == null || e.DateOfLeaving.Value.Year > year))
                    .ToListAsync();

                if (employees.Any())
                {
                    var salaries = employees.Select(e => e.BasicSalary).ToList();
                    var sortedSalaries = salaries.OrderBy(s => s).ToList();

                    var increases = await _context.SalaryHistories
                        .Where(sh => sh.EffectiveDate.Year == year)
                        .ToListAsync();

                    yearlyStats.Add(new YearlySalaryStatsDto
                    {
                        Year = year,
                        AverageSalary = salaries.Average(),
                        MedianSalary = GetMedian(sortedSalaries),
                        TotalPayroll = salaries.Sum(),
                        EmployeeCount = employees.Count,
                        AverageIncrease = increases.Any() ? increases.Average(i => i.SalaryIncrease) : 0,
                        NumberOfIncreases = increases.Count
                    });
                }
            }

            return ServiceResponse<List<YearlySalaryStatsDto>>.Success(yearlyStats);
        }
        catch (Exception ex)
        {
            return ServiceResponse<List<YearlySalaryStatsDto>>.Failure($"Error getting yearly salary stats: {ex.Message}");
        }
    }

    private SalaryHistoryEntryDto MapToSalaryHistoryEntryDto(SalaryHistory salaryHistory)
    {
        return new SalaryHistoryEntryDto
        {
            Id = salaryHistory.Id,
            EmployeeId = salaryHistory.EmployeeId,
            EmployeeCode = salaryHistory.Employee?.EmployeeCode ?? "",
            EmployeeName = salaryHistory.Employee?.FullName ?? "",
            PreviousSalary = salaryHistory.PreviousSalary,
            NewSalary = salaryHistory.NewSalary,
            SalaryIncrease = salaryHistory.SalaryIncrease,
            IncreasePercentage = salaryHistory.IncreasePercentage,
            EffectiveDate = salaryHistory.EffectiveDate,
            Reason = salaryHistory.Reason,
            Notes = salaryHistory.Notes,
            ApprovedBy = salaryHistory.ApprovedBy,
            CreatedAt = salaryHistory.CreatedAt
        };
    }

    private static decimal GetMedian(List<decimal> sortedValues)
    {
        if (!sortedValues.Any()) return 0;
        
        var count = sortedValues.Count;
        if (count % 2 == 0)
        {
            return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2;
        }
        else
        {
            return sortedValues[count / 2];
        }
    }
}