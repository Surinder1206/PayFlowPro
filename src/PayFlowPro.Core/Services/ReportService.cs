using Microsoft.EntityFrameworkCore;
using PayFlowPro.Core.Interfaces;
using PayFlowPro.Data.Context;
using PayFlowPro.Shared.DTOs.Reports;
using PayFlowPro.Models.Enums;
using System.Globalization;
using System.Text.Json;

namespace PayFlowPro.Core.Services
{
    /// <summary>
    /// Service for generating reports and analytics
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public ReportService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<PayrollSummaryReportDto> GetPayrollSummaryAsync(PayrollSummaryFilterDto filter)
        {
            using var context = _contextFactory.CreateDbContext();

            var payslipsQuery = context.Payslips
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Department)
                .Where(p => p.PayPeriodStart >= filter.FromDate && 
                           p.PayPeriodEnd <= filter.ToDate);

            if (filter.DepartmentId.HasValue)
                payslipsQuery = payslipsQuery.Where(p => p.Employee.DepartmentId == filter.DepartmentId.Value);

            if (filter.EmployeeId.HasValue)
                payslipsQuery = payslipsQuery.Where(p => p.EmployeeId == filter.EmployeeId.Value);

            if (filter.Status.HasValue)
                payslipsQuery = payslipsQuery.Where(p => p.Status == filter.Status.Value);

            var payslips = await payslipsQuery.ToListAsync();
            
            var employeeIds = payslips.Select(p => p.EmployeeId).Distinct();
            var activeEmployees = await context.Employees
                .CountAsync(e => employeeIds.Contains(e.Id) && e.Status == EmploymentStatus.Active);

            var summary = new PayrollSummaryReportDto
            {
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                TotalEmployees = employeeIds.Count(),
                ActiveEmployees = activeEmployees,
                TotalGrossSalary = payslips.Sum(p => p.GrossSalary),
                TotalNetSalary = payslips.Sum(p => p.NetSalary),
                TotalTaxAmount = payslips.Sum(p => p.TaxAmount),
                TotalDeductions = payslips.Sum(p => p.TotalDeductions),
                TotalAllowances = payslips.Sum(p => p.TotalAllowances),
                AverageGrossSalary = payslips.Any() ? payslips.Average(p => p.GrossSalary) : 0,
                AverageNetSalary = payslips.Any() ? payslips.Average(p => p.NetSalary) : 0
            };

            if (filter.IncludeDepartmentBreakdown)
            {
                summary.DepartmentSummaries = payslips
                    .GroupBy(p => new { p.Employee.DepartmentId, p.Employee.Department.Name })
                    .Select(g => new DepartmentPayrollSummaryDto
                    {
                        DepartmentId = g.Key.DepartmentId,
                        DepartmentName = g.Key.Name,
                        EmployeeCount = g.Select(p => p.EmployeeId).Distinct().Count(),
                        TotalGrossSalary = g.Sum(p => p.GrossSalary),
                        TotalNetSalary = g.Sum(p => p.NetSalary),
                        AverageGrossSalary = g.Average(p => p.GrossSalary),
                        TaxPercentage = g.Sum(p => p.GrossSalary) > 0 ? 
                            (g.Sum(p => p.TaxAmount) / g.Sum(p => p.GrossSalary)) * 100 : 0,
                        DeductionPercentage = g.Sum(p => p.GrossSalary) > 0 ? 
                            (g.Sum(p => p.TotalDeductions) / g.Sum(p => p.GrossSalary)) * 100 : 0
                    })
                    .OrderByDescending(d => d.TotalGrossSalary)
                    .ToList();
            }

            if (filter.IncludeMonthlyTrends)
            {
                summary.MonthlyTrends = await GetPayrollTrendsAsync(filter.FromDate, filter.ToDate, filter.GroupBy);
            }

            return summary;
        }

        public async Task<List<EmployeeSalaryReportDto>> GetEmployeeSalaryReportsAsync(EmployeeSalaryFilterDto filter)
        {
            using var context = _contextFactory.CreateDbContext();

            var employeesQuery = context.Employees
                .Include(e => e.Department)
                .Include(e => e.Payslips.Where(p => p.PayPeriodStart >= filter.FromDate && 
                                                   p.PayPeriodEnd <= filter.ToDate))
                .Where(e => true); // Base condition

            if (filter.DepartmentId.HasValue)
                employeesQuery = employeesQuery.Where(e => e.DepartmentId == filter.DepartmentId.Value);

            if (filter.EmployeeId.HasValue)
                employeesQuery = employeesQuery.Where(e => e.Id == filter.EmployeeId.Value);

            if (!string.IsNullOrEmpty(filter.EmployeeCode))
                employeesQuery = employeesQuery.Where(e => e.EmployeeCode.Contains(filter.EmployeeCode));

            if (!string.IsNullOrEmpty(filter.SearchTerm))
                employeesQuery = employeesQuery.Where(e => 
                    e.FirstName.Contains(filter.SearchTerm) || 
                    e.LastName.Contains(filter.SearchTerm) ||
                    e.EmployeeCode.Contains(filter.SearchTerm));

            if (filter.MinSalary.HasValue)
                employeesQuery = employeesQuery.Where(e => e.BasicSalary >= filter.MinSalary.Value);

            if (filter.MaxSalary.HasValue)
                employeesQuery = employeesQuery.Where(e => e.BasicSalary <= filter.MaxSalary.Value);

            if (filter.EmploymentStatus.HasValue)
                employeesQuery = employeesQuery.Where(e => e.Status == filter.EmploymentStatus.Value);

            var employees = await employeesQuery.ToListAsync();

            return employees.Select(e => new EmployeeSalaryReportDto
            {
                EmployeeId = e.Id,
                EmployeeCode = e.EmployeeCode,
                EmployeeName = e.FullName,
                Department = e.Department.Name,
                JobTitle = e.JobTitle ?? "N/A",
                DateOfJoining = e.DateOfJoining,
                CurrentBasicSalary = e.BasicSalary,
                CurrentGrossSalary = e.Payslips.LastOrDefault()?.GrossSalary ?? 0,
                CurrentNetSalary = e.Payslips.LastOrDefault()?.NetSalary ?? 0,
                YearToDateGross = e.Payslips.Where(p => p.PayPeriodStart.Year == DateTime.Now.Year).Sum(p => p.GrossSalary),
                YearToDateNet = e.Payslips.Where(p => p.PayPeriodStart.Year == DateTime.Now.Year).Sum(p => p.NetSalary),
                YearToDateTax = e.Payslips.Where(p => p.PayPeriodStart.Year == DateTime.Now.Year).Sum(p => p.TaxAmount),
                MonthlySalaries = filter.IncludeSalaryHistory ? e.Payslips.Select(p => new MonthlySalaryDto
                {
                    PayslipId = p.Id,
                    Year = p.PayPeriodStart.Year,
                    Month = p.PayPeriodStart.Month,
                    MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(p.PayPeriodStart.Month),
                    BasicSalary = p.BasicSalary,
                    GrossSalary = p.GrossSalary,
                    NetSalary = p.NetSalary,
                    TaxAmount = p.TaxAmount,
                    TotalAllowances = p.TotalAllowances,
                    TotalDeductions = p.TotalDeductions,
                    WorkingDays = p.WorkingDays,
                    Status = p.Status.ToString()
                }).OrderByDescending(m => m.Year).ThenByDescending(m => m.Month).ToList() : new()
            }).ToList();
        }

        public async Task<TaxSummaryReportDto> GetTaxSummaryAsync(TaxSummaryFilterDto filter)
        {
            using var context = _contextFactory.CreateDbContext();

            var payslips = await context.Payslips
                .Include(p => p.Employee)
                .Where(p => p.PayPeriodStart >= filter.FromDate && 
                           p.PayPeriodEnd <= filter.ToDate &&
                           p.TaxAmount > 0)
                .ToListAsync();

            if (filter.DepartmentId.HasValue)
                payslips = payslips.Where(p => p.Employee.DepartmentId == filter.DepartmentId.Value).ToList();

            if (filter.EmployeeId.HasValue)
                payslips = payslips.Where(p => p.EmployeeId == filter.EmployeeId.Value).ToList();

            if (filter.MinTaxAmount.HasValue)
                payslips = payslips.Where(p => p.TaxAmount >= filter.MinTaxAmount.Value).ToList();

            if (filter.MaxTaxAmount.HasValue)
                payslips = payslips.Where(p => p.TaxAmount <= filter.MaxTaxAmount.Value).ToList();

            var totalTax = payslips.Sum(p => p.TaxAmount);
            var totalGross = payslips.Sum(p => p.GrossSalary);
            var taxableEmployees = payslips.Select(p => p.EmployeeId).Distinct().Count();

            var summary = new TaxSummaryReportDto
            {
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                TotalTaxCollected = totalTax,
                AverageTaxRate = totalGross > 0 ? (totalTax / totalGross) * 100 : 0,
                TaxableEmployees = taxableEmployees
            };

            if (filter.IncludeTaxBrackets)
            {
                // Define tax brackets (this could be configured)
                var brackets = new[]
                {
                    new { From = 0m, To = 250000m, Rate = 0m },
                    new { From = 250000m, To = 500000m, Rate = 5m },
                    new { From = 500000m, To = 1000000m, Rate = 10m },
                    new { From = 1000000m, To = decimal.MaxValue, Rate = 15m }
                };

                summary.TaxBrackets = brackets.Select(b => 
                {
                    var employeesInBracket = payslips.Where(p => 
                        p.GrossSalary * 12 >= b.From && 
                        p.GrossSalary * 12 < b.To).ToList();
                    
                    return new TaxBracketSummaryDto
                    {
                        FromAmount = b.From,
                        ToAmount = b.To == decimal.MaxValue ? 0 : b.To,
                        TaxRate = b.Rate,
                        EmployeeCount = employeesInBracket.Select(p => p.EmployeeId).Distinct().Count(),
                        TotalTaxInBracket = employeesInBracket.Sum(p => p.TaxAmount)
                    };
                }).ToList();
            }

            if (filter.IncludeEmployeeDetails)
            {
                summary.EmployeeTaxSummaries = payslips
                    .GroupBy(p => new { p.EmployeeId, p.Employee.EmployeeCode, p.Employee.FirstName, p.Employee.LastName })
                    .Select(g => new EmployeeTaxSummaryDto
                    {
                        EmployeeId = g.Key.EmployeeId,
                        EmployeeCode = g.Key.EmployeeCode,
                        EmployeeName = $"{g.Key.FirstName} {g.Key.LastName}",
                        YearToDateGross = g.Sum(p => p.GrossSalary),
                        YearToDateTax = g.Sum(p => p.TaxAmount),
                        EffectiveTaxRate = g.Sum(p => p.GrossSalary) > 0 ? 
                            (g.Sum(p => p.TaxAmount) / g.Sum(p => p.GrossSalary)) * 100 : 0,
                        EstimatedAnnualTax = g.Sum(p => p.TaxAmount) * (12 / Math.Max(1, g.Count()))
                    })
                    .OrderByDescending(e => e.YearToDateTax)
                    .ToList();
            }

            return summary;
        }

        public async Task<List<AttendancePayrollReportDto>> GetAttendancePayrollReportAsync(AttendancePayrollFilterDto filter)
        {
            using var context = _contextFactory.CreateDbContext();

            var payslips = await context.Payslips
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Department)
                .Where(p => p.PayPeriodStart >= filter.FromDate && 
                           p.PayPeriodEnd <= filter.ToDate)
                .ToListAsync();

            if (filter.DepartmentId.HasValue)
                payslips = payslips.Where(p => p.Employee.DepartmentId == filter.DepartmentId.Value).ToList();

            if (filter.EmployeeId.HasValue)
                payslips = payslips.Where(p => p.EmployeeId == filter.EmployeeId.Value).ToList();

            var reports = payslips.Select(p =>
            {
                var attendancePercentage = p.WorkingDays > 0 ? 
                    ((decimal)p.ActualWorkingDays / p.WorkingDays) * 100 : 0;
                
                var salaryPerDay = p.WorkingDays > 0 ? p.GrossSalary / p.WorkingDays : 0;
                var deductedAmount = (p.WorkingDays - p.ActualWorkingDays) * salaryPerDay;

                return new AttendancePayrollReportDto
                {
                    EmployeeId = p.EmployeeId,
                    EmployeeCode = p.Employee.EmployeeCode,
                    EmployeeName = p.Employee.FullName,
                    Department = p.Employee.Department.Name,
                    TotalWorkingDays = p.WorkingDays,
                    ActualWorkingDays = p.ActualWorkingDays,
                    AttendancePercentage = attendancePercentage,
                    GrossSalary = p.GrossSalary,
                    NetSalary = p.NetSalary,
                    SalaryPerDay = salaryPerDay,
                    DeductedAmount = deductedAmount
                };
            }).ToList();

            if (filter.MinAttendancePercentage.HasValue)
                reports = reports.Where(r => r.AttendancePercentage >= filter.MinAttendancePercentage.Value).ToList();

            if (filter.MaxAttendancePercentage.HasValue)
                reports = reports.Where(r => r.AttendancePercentage <= filter.MaxAttendancePercentage.Value).ToList();

            if (filter.ShowOnlyPoorAttendance)
                reports = reports.Where(r => r.AttendancePercentage < 90).ToList();

            return reports.OrderBy(r => r.AttendancePercentage).ToList();
        }

        public async Task<List<CompensationAnalysisDto>> GetCompensationAnalysisAsync(CompensationAnalysisFilterDto filter)
        {
            using var context = _contextFactory.CreateDbContext();

            var employeesQuery = context.Employees
                .Include(e => e.Department)
                .Include(e => e.Payslips)
                .Where(e => e.Status == EmploymentStatus.Active);

            if (filter.DepartmentId.HasValue)
                employeesQuery = employeesQuery.Where(e => e.DepartmentId == filter.DepartmentId.Value);

            if (!string.IsNullOrEmpty(filter.JobTitle))
                employeesQuery = employeesQuery.Where(e => e.JobTitle != null && e.JobTitle.Contains(filter.JobTitle));

            var employees = await employeesQuery.ToListAsync();

            var analysis = employees
                .GroupBy(e => new { e.JobTitle, e.Department.Name })
                .Where(g => g.Count() >= 2) // Only include groups with at least 2 employees
                .Select(g =>
                {
                    var salaries = g.Select(e => e.BasicSalary).OrderBy(s => s).ToList();
                    var mean = salaries.Average();
                    var median = salaries.Count % 2 == 0 
                        ? (salaries[salaries.Count / 2 - 1] + salaries[salaries.Count / 2]) / 2
                        : salaries[salaries.Count / 2];

                    var variance = salaries.Average(s => Math.Pow((double)(s - mean), 2));
                    var stdDev = (decimal)Math.Sqrt(variance);

                    return new CompensationAnalysisDto
                    {
                        JobTitle = g.Key.JobTitle ?? "N/A",
                        Department = g.Key.Name,
                        EmployeeCount = g.Count(),
                        MinimumSalary = salaries.First(),
                        MaximumSalary = salaries.Last(),
                        AverageSalary = mean,
                        MedianSalary = median,
                        StandardDeviation = stdDev,
                        MarketRate = filter.IncludeMarketComparison ? GetMarketRate(g.Key.JobTitle) : 0,
                        CompetitiveRatio = filter.IncludeMarketComparison ? 
                            GetMarketRate(g.Key.JobTitle) > 0 ? mean / GetMarketRate(g.Key.JobTitle) : 1 : 1
                    };
                })
                .OrderBy(c => c.Department)
                .ThenBy(c => c.JobTitle)
                .ToList();

            return analysis;
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var previousMonth = currentMonth.AddMonths(-1);
            var currentMonthEnd = currentMonth.AddMonths(1).AddDays(-1);
            var previousMonthEnd = currentMonth.AddDays(-1);

            var currentMonthFilter = new PayrollSummaryFilterDto 
            { 
                FromDate = currentMonth, 
                ToDate = currentMonthEnd,
                IncludeDepartmentBreakdown = true,
                IncludeMonthlyTrends = false
            };

            var previousMonthFilter = new PayrollSummaryFilterDto 
            { 
                FromDate = previousMonth, 
                ToDate = previousMonthEnd,
                IncludeDepartmentBreakdown = false,
                IncludeMonthlyTrends = false
            };

            var summary = new DashboardSummaryDto
            {
                CurrentMonthPayroll = await GetPayrollSummaryAsync(currentMonthFilter),
                PreviousMonthPayroll = await GetPayrollSummaryAsync(previousMonthFilter),
                TopDepartments = (await GetDepartmentComparisonAsync(currentMonth, currentMonthEnd)).Take(5).ToList(),
                TopEarners = (await GetTopEarnersAsync(10, currentMonth, currentMonthEnd)).Take(5).ToList(),
                PayrollTrendChart = await GetChartDataAsync(new ChartDataRequestDto 
                { 
                    ChartType = "line", 
                    DataType = "payroll", 
                    FromDate = DateTime.Now.AddMonths(-12), 
                    ToDate = DateTime.Now,
                    GroupBy = ReportGroupBy.Month
                }),
                DepartmentComparisonChart = await GetChartDataAsync(new ChartDataRequestDto 
                { 
                    ChartType = "doughnut", 
                    DataType = "department", 
                    FromDate = currentMonth, 
                    ToDate = currentMonthEnd
                })
            };

            // Calculate growth percentages
            if (summary.PreviousMonthPayroll.TotalGrossSalary > 0)
            {
                summary.PayrollGrowthPercentage = ((summary.CurrentMonthPayroll.TotalGrossSalary - 
                    summary.PreviousMonthPayroll.TotalGrossSalary) / summary.PreviousMonthPayroll.TotalGrossSalary) * 100;
            }

            using var context = _contextFactory.CreateDbContext();
            
            // New employees this month
            summary.NewEmployeesThisMonth = await context.Employees
                .CountAsync(e => e.DateOfJoining >= currentMonth && e.DateOfJoining <= currentMonthEnd);

            // Employees who left this month
            summary.EmployeesLeftThisMonth = await context.Employees
                .CountAsync(e => e.DateOfLeaving.HasValue && 
                                e.DateOfLeaving.Value >= currentMonth && 
                                e.DateOfLeaving.Value <= currentMonthEnd);

            return summary;
        }

        // Additional implementation methods would continue here...
        // For brevity, I'm showing the key methods. The remaining methods would follow similar patterns.

        private decimal GetMarketRate(string? jobTitle)
        {
            // This would typically come from external market data or configuration
            // For now, returning a placeholder value
            return jobTitle switch
            {
                "Software Engineer" => 80000m,
                "Senior Software Engineer" => 120000m,
                "Manager" => 100000m,
                "HR Manager" => 75000m,
                "Accountant" => 55000m,
                _ => 60000m
            };
        }

        // Additional implementation methods
        public async Task<ChartDataDto> GetChartDataAsync(ChartDataRequestDto request)
        {
            using var context = _contextFactory.CreateDbContext();
            
            var chartData = new ChartDataDto
            {
                Title = request.ChartType,
                Labels = new List<string>(),
                Datasets = new List<ChartDatasetDto>()
            };

            if (request.DataType == "payroll")
            {
                var payslips = await context.Payslips
                    .Include(p => p.Employee)
                        .ThenInclude(e => e.Department)
                    .Where(p => p.PayPeriodStart >= request.FromDate && p.PayPeriodEnd <= request.ToDate)
                    .OrderBy(p => p.PayPeriodStart)
                    .ToListAsync();

                if (payslips.Any())
                {
                    var monthlyData = payslips
                        .GroupBy(p => new { p.PayPeriodStart.Year, p.PayPeriodStart.Month })
                        .Select(g => new
                        {
                            g.Key.Year,
                            g.Key.Month,
                            TotalAmount = g.Sum(p => p.NetSalary)
                        })
                        .OrderBy(x => x.Year)
                        .ThenBy(x => x.Month)
                        .ToList();

                    chartData.Labels = monthlyData.Select(d => $"{d.Year}-{d.Month:D2}").ToList();
                    chartData.Datasets.Add(new ChartDatasetDto
                    {
                        Label = "Net Salary",
                        Data = monthlyData.Select(d => d.TotalAmount).ToList(),
                        BackgroundColor = "rgba(54, 162, 235, 0.2)",
                        BorderColor = "rgba(54, 162, 235, 1)"
                    });
                }
            }
            else if (request.DataType == "department")
            {
                var departmentData = await context.Departments
                    .Where(d => d.IsActive)
                    .Select(d => new
                    {
                        d.Name,
                        TotalSalary = d.Employees
                            .SelectMany(e => e.Payslips)
                            .Where(p => p.PayPeriodStart >= request.FromDate && p.PayPeriodEnd <= request.ToDate)
                            .Sum(p => p.NetSalary)
                    })
                    .Where(d => d.TotalSalary > 0)
                    .ToListAsync();

                chartData.Labels = departmentData.Select(d => d.Name).ToList();
                chartData.Datasets.Add(new ChartDatasetDto
                {
                    Label = "Department Payroll",
                    Data = departmentData.Select(d => d.TotalSalary).ToList(),
                    BackgroundColor = "rgba(255, 99, 132, 0.2)",
                    BorderColor = "rgba(255, 99, 132, 1)"
                });
            }

            return chartData;
        }

        public async Task<List<DepartmentPayrollSummaryDto>> GetDepartmentComparisonAsync(DateTime fromDate, DateTime toDate)
        {
            using var context = _contextFactory.CreateDbContext();
            
            return await context.Departments
                .Where(d => d.IsActive)
                .Select(d => new DepartmentPayrollSummaryDto
                {
                    DepartmentId = d.Id,
                    DepartmentName = d.Name,
                    EmployeeCount = d.Employees.Count(e => e.Status == EmploymentStatus.Active),
                    TotalGrossSalary = d.Employees
                        .Where(e => e.Status == EmploymentStatus.Active)
                        .SelectMany(e => e.Payslips)
                        .Where(p => p.PayPeriodStart >= fromDate && p.PayPeriodEnd <= toDate)
                        .Sum(p => p.GrossSalary),
                    TotalNetSalary = d.Employees
                        .Where(e => e.Status == EmploymentStatus.Active)
                        .SelectMany(e => e.Payslips)
                        .Where(p => p.PayPeriodStart >= fromDate && p.PayPeriodEnd <= toDate)
                        .Sum(p => p.NetSalary),
                    AverageGrossSalary = d.Employees
                        .Where(e => e.Status == EmploymentStatus.Active)
                        .SelectMany(e => e.Payslips)
                        .Where(p => p.PayPeriodStart >= fromDate && p.PayPeriodEnd <= toDate)
                        .Any() ? d.Employees
                            .Where(e => e.Status == EmploymentStatus.Active)
                            .SelectMany(e => e.Payslips)
                            .Where(p => p.PayPeriodStart >= fromDate && p.PayPeriodEnd <= toDate)
                            .Average(p => p.GrossSalary) : 0,
                    TaxPercentage = 0, // Calculate tax percentage
                    DeductionPercentage = 0 // Calculate deduction percentage
                })
                .Where(d => d.TotalNetSalary > 0)
                .OrderByDescending(d => d.TotalNetSalary)
                .ToListAsync();
        }

        public async Task<List<MonthlyPayrollTrendDto>> GetPayrollTrendsAsync(DateTime fromDate, DateTime toDate, ReportGroupBy groupBy = ReportGroupBy.Month)
        {
            using var context = _contextFactory.CreateDbContext();
            
            return await context.Payslips
                .Where(p => p.PayPeriodStart >= fromDate && p.PayPeriodStart <= toDate)
                .GroupBy(p => new { p.PayPeriodStart.Year, p.PayPeriodStart.Month })
                .Select(g => new MonthlyPayrollTrendDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month),
                    TotalGrossSalary = g.Sum(p => p.GrossSalary),
                    TotalNetSalary = g.Sum(p => p.NetSalary),
                    TotalTaxAmount = g.Sum(p => p.TaxAmount),
                    EmployeeCount = g.Select(p => p.EmployeeId).Distinct().Count(),
                    AverageSalary = g.Average(p => p.NetSalary)
                })
                .OrderBy(t => t.Year)
                .ThenBy(t => t.Month)
                .ToListAsync();
        }

        public async Task<List<EmployeeSalaryReportDto>> GetTopEarnersAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var context = _contextFactory.CreateDbContext();
            
            var query = context.Employees
                .Where(e => e.Status == EmploymentStatus.Active && e.Payslips.Any())
                .Include(e => e.Department)
                .AsQueryable();

            if (fromDate.HasValue && toDate.HasValue)
            {
                query = query.Where(e => e.Payslips.Any(p => p.PayPeriodStart >= fromDate.Value && p.PayPeriodEnd <= toDate.Value));
            }

            return await query
                .Select(e => new EmployeeSalaryReportDto
                {
                    EmployeeId = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    EmployeeName = $"{e.FirstName} {e.LastName}",
                    Department = e.Department.Name,
                    JobTitle = e.JobTitle,
                    DateOfJoining = e.DateOfJoining,
                    CurrentBasicSalary = e.BasicSalary,
                    CurrentGrossSalary = fromDate.HasValue && toDate.HasValue 
                        ? e.Payslips.Where(p => p.PayPeriodStart >= fromDate.Value && p.PayPeriodEnd <= toDate.Value).Any()
                            ? e.Payslips.Where(p => p.PayPeriodStart >= fromDate.Value && p.PayPeriodEnd <= toDate.Value).Average(p => p.GrossSalary)
                            : 0
                        : e.Payslips.Any() ? e.Payslips.Average(p => p.GrossSalary) : 0,
                    CurrentNetSalary = fromDate.HasValue && toDate.HasValue 
                        ? e.Payslips.Where(p => p.PayPeriodStart >= fromDate.Value && p.PayPeriodEnd <= toDate.Value).Any()
                            ? e.Payslips.Where(p => p.PayPeriodStart >= fromDate.Value && p.PayPeriodEnd <= toDate.Value).Average(p => p.NetSalary)
                            : 0
                        : e.Payslips.Any() ? e.Payslips.Average(p => p.NetSalary) : 0,
                    YearToDateGross = fromDate.HasValue && toDate.HasValue 
                        ? e.Payslips.Where(p => p.PayPeriodStart >= fromDate.Value && p.PayPeriodEnd <= toDate.Value).Sum(p => p.GrossSalary)
                        : e.Payslips.Sum(p => p.GrossSalary),
                    YearToDateNet = fromDate.HasValue && toDate.HasValue 
                        ? e.Payslips.Where(p => p.PayPeriodStart >= fromDate.Value && p.PayPeriodEnd <= toDate.Value).Sum(p => p.NetSalary)
                        : e.Payslips.Sum(p => p.NetSalary),
                    YearToDateTax = fromDate.HasValue && toDate.HasValue 
                        ? e.Payslips.Where(p => p.PayPeriodStart >= fromDate.Value && p.PayPeriodEnd <= toDate.Value).Sum(p => p.TaxAmount)
                        : e.Payslips.Sum(p => p.TaxAmount),
                    MonthlySalaries = new List<MonthlySalaryDto>()
                })
                .OrderByDescending(e => e.CurrentNetSalary)
                .Take(count)
                .ToListAsync();
        }
        public Task<List<EmployeeSalaryReportDto>> GetSalaryChangesAsync(DateTime fromDate, DateTime toDate) => throw new NotImplementedException();
        public Task<object> GenerateCustomReportAsync(string reportType, Dictionary<string, object> parameters) => throw new NotImplementedException();
        public Task<byte[]> ExportReportAsync(ReportExportRequestDto request) => throw new NotImplementedException();
        public Task<byte[]> ExportToExcelAsync<T>(List<T> data, string sheetName, Dictionary<string, string>? columnHeaders = null) => throw new NotImplementedException();
        public Task<byte[]> ExportToCsvAsync<T>(List<T> data, Dictionary<string, string>? columnHeaders = null) => throw new NotImplementedException();
        public Task<bool> ScheduleReportAsync(string reportType, object parameters, string cronExpression, string recipientEmails) => throw new NotImplementedException();
        public Task<List<ReportTemplateDto>> GetReportTemplatesAsync() => throw new NotImplementedException();
        public Task<ReportValidationResult> ValidateReportParametersAsync(string reportType, object parameters) => throw new NotImplementedException();
    }
}