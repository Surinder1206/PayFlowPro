using Microsoft.EntityFrameworkCore;
using PayFlowPro.Core.Interfaces;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PayFlowPro.Core.Services
{
    /// <summary>
    /// Service for generating PDF documents
    /// </summary>
    public class PdfService : IPdfService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public PdfService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
            
            // Configure QuestPDF license (Community license)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Generate a PDF document for a payslip
        /// </summary>
        public async Task<byte[]> GeneratePayslipPdfAsync(Payslip payslip)
        {
            using var context = _contextFactory.CreateDbContext();
            
            // Load related data
            var fullPayslip = await context.Payslips
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Department)
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Company)
                .Include(p => p.PayslipAllowances)
                    .ThenInclude(pa => pa.AllowanceType)
                .Include(p => p.PayslipDeductions)
                    .ThenInclude(pd => pd.DeductionType)
                .FirstOrDefaultAsync(p => p.Id == payslip.Id);

            if (fullPayslip == null)
                throw new ArgumentException("Payslip not found", nameof(payslip));

            return GeneratePayslipPdf(fullPayslip);
        }

        /// <summary>
        /// Generate a PDF document for multiple payslips
        /// </summary>
        public async Task<byte[]> GenerateBatchPayslipsPdfAsync(IEnumerable<Payslip> payslips)
        {
            using var context = _contextFactory.CreateDbContext();
            
            var payslipIds = payslips.Select(p => p.Id).ToList();
            var fullPayslips = await context.Payslips
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Department)
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Company)
                .Include(p => p.PayslipAllowances)
                    .ThenInclude(pa => pa.AllowanceType)
                .Include(p => p.PayslipDeductions)
                    .ThenInclude(pd => pd.DeductionType)
                .Where(p => payslipIds.Contains(p.Id))
                .ToListAsync();

            return Document.Create(container =>
            {
                foreach (var payslip in fullPayslips)
                {
                    container.Page(page => BuildPayslipPage(page, payslip));
                }
            }).GeneratePdf();
        }

        /// <summary>
        /// Generate a salary statement PDF for an employee
        /// </summary>
        public async Task<byte[]> GenerateSalaryStatementPdfAsync(int employeeId, DateTime fromDate, DateTime toDate)
        {
            using var context = _contextFactory.CreateDbContext();
            
            var employee = await context.Employees
                .Include(e => e.Department)
                .Include(e => e.Company)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
                throw new ArgumentException("Employee not found", nameof(employeeId));

            var payslips = await context.Payslips
                .Include(p => p.PayslipAllowances)
                    .ThenInclude(pa => pa.AllowanceType)
                .Include(p => p.PayslipDeductions)
                    .ThenInclude(pd => pd.DeductionType)
                .Where(p => p.EmployeeId == employeeId && 
                           p.PayPeriodStart >= fromDate && 
                           p.PayPeriodEnd <= toDate)
                .OrderBy(p => p.PayPeriodStart)
                .ToListAsync();

            return GenerateSalaryStatementPdf(employee, payslips, fromDate, toDate);
        }

        /// <summary>
        /// Generate a payroll summary PDF for a specific period
        /// </summary>
        public async Task<byte[]> GeneratePayrollSummaryPdfAsync(DateTime fromDate, DateTime toDate)
        {
            using var context = _contextFactory.CreateDbContext();
            
            var payslips = await context.Payslips
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Department)
                .Include(p => p.PayslipAllowances)
                    .ThenInclude(pa => pa.AllowanceType)
                .Include(p => p.PayslipDeductions)
                    .ThenInclude(pd => pd.DeductionType)
                .Where(p => p.PayPeriodStart >= fromDate && p.PayPeriodEnd <= toDate)
                .OrderBy(p => p.Employee.EmployeeCode)
                .ToListAsync();

            return GeneratePayrollSummaryPdf(payslips, fromDate, toDate);
        }

        private byte[] GeneratePayslipPdf(Payslip payslip)
        {
            return Document.Create(container =>
            {
                container.Page(page => BuildPayslipPage(page, payslip));
            }).GeneratePdf();
        }

        private void BuildPayslipPage(PageDescriptor page, Payslip payslip)
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

            page.Header()
                .Height(100)
                .Background(Colors.Grey.Lighten3)
                .Padding(20)
                .Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text(payslip.Employee.Company.Name)
                            .FontSize(18)
                            .FontColor(Colors.Blue.Darken2)
                            .Bold();
                        
                        column.Item().Text(payslip.Employee.Company.Address)
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);
                        
                        if (!string.IsNullOrEmpty(payslip.Employee.Company.PhoneNumber))
                        {
                            column.Item().Text($"Phone: {payslip.Employee.Company.PhoneNumber}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);
                        }
                        
                        if (!string.IsNullOrEmpty(payslip.Employee.Company.Email))
                        {
                            column.Item().Text($"Email: {payslip.Employee.Company.Email}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);
                        }
                    });

                    row.ConstantItem(100).Column(column =>
                    {
                        column.Item().AlignRight().Text("PAYSLIP")
                            .FontSize(16)
                            .FontColor(Colors.Blue.Darken2)
                            .Bold();
                    });
                });

            page.Content()
                .PaddingVertical(1, Unit.Centimetre)
                .Column(column =>
                {
                    // Employee and Pay Period Information
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Employee Information").FontSize(12).Bold().FontColor(Colors.Blue.Darken1);
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(100);
                                    columns.RelativeColumn();
                                });

                                table.Cell().Text("Employee ID:").Bold();
                                table.Cell().Text(payslip.Employee.EmployeeCode);

                                table.Cell().Text("Name:").Bold();
                                table.Cell().Text($"{payslip.Employee.FirstName} {payslip.Employee.LastName}");

                                table.Cell().Text("Department:").Bold();
                                table.Cell().Text(payslip.Employee.Department.Name);

                                table.Cell().Text("Designation:").Bold();
                                table.Cell().Text(payslip.Employee.JobTitle ?? "N/A");

                                table.Cell().Text("Date of Joining:").Bold();
                                table.Cell().Text(payslip.Employee.DateOfJoining.ToString("MMM dd, yyyy"));
                            });
                        });

                        row.ConstantItem(20); // Spacer

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Pay Information").FontSize(12).Bold().FontColor(Colors.Blue.Darken1);
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(100);
                                    columns.RelativeColumn();
                                });

                                table.Cell().Text("Payslip No:").Bold();
                                table.Cell().Text(payslip.PayslipNumber);

                                table.Cell().Text("Pay Period:").Bold();
                                table.Cell().Text($"{payslip.PayPeriodStart:MMM dd} - {payslip.PayPeriodEnd:MMM dd, yyyy}");

                                table.Cell().Text("Working Days:").Bold();
                                table.Cell().Text($"{payslip.WorkingDays} days");

                                table.Cell().Text("Status:").Bold();
                                table.Cell().Text(payslip.Status.ToString()).FontColor(GetStatusColor(payslip.Status));

                                table.Cell().Text("Generated On:").Bold();
                                table.Cell().Text(payslip.CreatedAt.ToString("MMM dd, yyyy"));
                            });
                        });
                    });

                    column.Item().PaddingTop(20);

                    // Earnings and Deductions
                    column.Item().Row(row =>
                    {
                        // Earnings
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Earnings").FontSize(12).Bold().FontColor(Colors.Green.Darken1);
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(80);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Green.Lighten3).Padding(5).Text("Description").Bold();
                                    header.Cell().Background(Colors.Green.Lighten3).Padding(5).AlignRight().Text("Amount").Bold();
                                });

                                // Basic Salary
                                table.Cell().Padding(3).Text("Basic Salary");
                                table.Cell().Padding(3).AlignRight().Text($"₹{payslip.BasicSalary:N2}");

                                // Allowances
                                foreach (var allowance in payslip.PayslipAllowances)
                                {
                                    table.Cell().Padding(3).Text(allowance.AllowanceType.Name);
                                    table.Cell().Padding(3).AlignRight().Text($"₹{allowance.Amount:N2}");
                                }

                                // Total Earnings
                                table.Cell().Background(Colors.Green.Lighten4).Padding(3).Text("Total Earnings").Bold();
                                table.Cell().Background(Colors.Green.Lighten4).Padding(3).AlignRight().Text($"₹{payslip.GrossSalary:N2}").Bold();
                            });
                        });

                        row.ConstantItem(20); // Spacer

                        // Deductions
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Deductions").FontSize(12).Bold().FontColor(Colors.Red.Darken1);
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(80);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Red.Lighten3).Padding(5).Text("Description").Bold();
                                    header.Cell().Background(Colors.Red.Lighten3).Padding(5).AlignRight().Text("Amount").Bold();
                                });

                                // Tax
                                if (payslip.TaxAmount > 0)
                                {
                                    table.Cell().Padding(3).Text("Income Tax");
                                    table.Cell().Padding(3).AlignRight().Text($"₹{payslip.TaxAmount:N2}");
                                }

                                // Deductions
                                foreach (var deduction in payslip.PayslipDeductions)
                                {
                                    table.Cell().Padding(3).Text(deduction.DeductionType.Name);
                                    table.Cell().Padding(3).AlignRight().Text($"₹{deduction.Amount:N2}");
                                }

                                // Total Deductions
                                table.Cell().Background(Colors.Red.Lighten4).Padding(3).Text("Total Deductions").Bold();
                                table.Cell().Background(Colors.Red.Lighten4).Padding(3).AlignRight().Text($"₹{payslip.TotalDeductions:N2}").Bold();
                            });
                        });
                    });

                    column.Item().PaddingTop(20);

                    // Net Salary
                    column.Item().Background(Colors.Blue.Lighten4).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Text("Net Salary").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                        row.ConstantItem(120).AlignRight().Text($"₹{payslip.NetSalary:N2}").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                    });

                    // Notes
                    if (!string.IsNullOrEmpty(payslip.Notes))
                    {
                        column.Item().PaddingTop(20);
                        column.Item().Text("Notes").FontSize(12).Bold().FontColor(Colors.Grey.Darken1);
                        column.Item().PaddingTop(5).Text(payslip.Notes).FontSize(9).FontColor(Colors.Grey.Darken1);
                    }
                });

            page.Footer()
                .Height(50)
                .Background(Colors.Grey.Lighten3)
                .Padding(10)
                .Row(row =>
                {
                    row.RelativeItem().Text("This is a computer generated payslip and does not require signature.")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);
                    
                    row.ConstantItem(120).AlignRight().Text($"Generated on {DateTime.Now:MMM dd, yyyy}")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);
                });
        }

        private byte[] GenerateSalaryStatementPdf(Employee employee, List<Payslip> payslips, DateTime fromDate, DateTime toDate)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // Header
                    page.Header().Height(80).Background(Colors.Blue.Lighten3).Padding(15).Column(column =>
                    {
                        column.Item().Text($"Salary Statement - {employee.FirstName} {employee.LastName}")
                            .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                        column.Item().Text($"Period: {fromDate:MMM dd, yyyy} to {toDate:MMM dd, yyyy}")
                            .FontSize(12).FontColor(Colors.Blue.Darken1);
                        column.Item().Text($"Employee Code: {employee.EmployeeCode} | Department: {employee.Department.Name}")
                            .FontSize(10).FontColor(Colors.Grey.Darken1);
                    });

                    // Content
                    page.Content().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);  // Month
                            columns.ConstantColumn(80);  // Gross
                            columns.ConstantColumn(80);  // Tax
                            columns.ConstantColumn(80);  // Deductions
                            columns.ConstantColumn(80);  // Net
                            columns.RelativeColumn();    // Status
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Month").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Gross").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Tax").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Deductions").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Net Salary").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Status").Bold();
                        });

                        foreach (var payslip in payslips)
                        {
                            table.Cell().Padding(5).Text(payslip.PayPeriodStart.ToString("MMM yyyy"));
                            table.Cell().Padding(5).AlignRight().Text($"₹{payslip.GrossSalary:N0}");
                            table.Cell().Padding(5).AlignRight().Text($"₹{payslip.TaxAmount:N0}");
                            table.Cell().Padding(5).AlignRight().Text($"₹{payslip.TotalDeductions:N0}");
                            table.Cell().Padding(5).AlignRight().Text($"₹{payslip.NetSalary:N0}");
                            table.Cell().Padding(5).Text(payslip.Status.ToString()).FontColor(GetStatusColor(payslip.Status));
                        }

                        // Totals
                        var totalGross = payslips.Sum(p => p.GrossSalary);
                        var totalTax = payslips.Sum(p => p.TaxAmount);
                        var totalDeductions = payslips.Sum(p => p.TotalDeductions);
                        var totalNet = payslips.Sum(p => p.NetSalary);

                        table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("TOTAL").Bold();
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"₹{totalGross:N0}").Bold();
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"₹{totalTax:N0}").Bold();
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"₹{totalDeductions:N0}").Bold();
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"₹{totalNet:N0}").Bold();
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text($"{payslips.Count} months");
                    });

                    // Footer
                    page.Footer().Height(30).Padding(10).AlignCenter().Text($"Generated on {DateTime.Now:MMM dd, yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            }).GeneratePdf();
        }

        private byte[] GeneratePayrollSummaryPdf(List<Payslip> payslips, DateTime fromDate, DateTime toDate)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    // Header
                    page.Header().Height(60).Background(Colors.Green.Lighten3).Padding(15).Column(column =>
                    {
                        column.Item().Text("Payroll Summary Report")
                            .FontSize(16).Bold().FontColor(Colors.Green.Darken2);
                        column.Item().Text($"Period: {fromDate:MMM dd, yyyy} to {toDate:MMM dd, yyyy}")
                            .FontSize(12).FontColor(Colors.Green.Darken1);
                    });

                    // Content
                    page.Content().PaddingTop(15).Column(column =>
                    {
                        // Summary Statistics
                        column.Item().PaddingBottom(15).Row(row =>
                        {
                            var totalEmployees = payslips.Select(p => p.EmployeeId).Distinct().Count();
                            var totalGross = payslips.Sum(p => p.GrossSalary);
                            var totalNet = payslips.Sum(p => p.NetSalary);
                            var totalTax = payslips.Sum(p => p.TaxAmount);

                            row.RelativeItem().Background(Colors.Blue.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Employees").Bold();
                                col.Item().Text(totalEmployees.ToString()).FontSize(14).FontColor(Colors.Blue.Darken2);
                            });

                            row.RelativeItem().Background(Colors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Gross").Bold();
                                col.Item().Text($"₹{totalGross:N0}").FontSize(14).FontColor(Colors.Green.Darken2);
                            });

                            row.RelativeItem().Background(Colors.Orange.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Tax").Bold();
                                col.Item().Text($"₹{totalTax:N0}").FontSize(14).FontColor(Colors.Orange.Darken2);
                            });

                            row.RelativeItem().Background(Colors.Purple.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Net Pay").Bold();
                                col.Item().Text($"₹{totalNet:N0}").FontSize(14).FontColor(Colors.Purple.Darken2);
                            });
                        });

                        // Detailed Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60);   // Emp Code
                                columns.RelativeColumn(2);    // Name
                                columns.RelativeColumn(1.5f); // Department
                                columns.ConstantColumn(70);   // Gross
                                columns.ConstantColumn(60);   // Tax
                                columns.ConstantColumn(70);   // Net
                                columns.ConstantColumn(60);   // Status
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten1).Padding(3).Text("Emp Code").Bold();
                                header.Cell().Background(Colors.Grey.Lighten1).Padding(3).Text("Employee Name").Bold();
                                header.Cell().Background(Colors.Grey.Lighten1).Padding(3).Text("Department").Bold();
                                header.Cell().Background(Colors.Grey.Lighten1).Padding(3).AlignRight().Text("Gross").Bold();
                                header.Cell().Background(Colors.Grey.Lighten1).Padding(3).AlignRight().Text("Tax").Bold();
                                header.Cell().Background(Colors.Grey.Lighten1).Padding(3).AlignRight().Text("Net Pay").Bold();
                                header.Cell().Background(Colors.Grey.Lighten1).Padding(3).Text("Status").Bold();
                            });

                            foreach (var payslip in payslips.OrderBy(p => p.Employee.EmployeeCode))
                            {
                                table.Cell().Padding(3).Text(payslip.Employee.EmployeeCode);
                                table.Cell().Padding(3).Text($"{payslip.Employee.FirstName} {payslip.Employee.LastName}");
                                table.Cell().Padding(3).Text(payslip.Employee.Department.Name);
                                table.Cell().Padding(3).AlignRight().Text($"₹{payslip.GrossSalary:N0}");
                                table.Cell().Padding(3).AlignRight().Text($"₹{payslip.TaxAmount:N0}");
                                table.Cell().Padding(3).AlignRight().Text($"₹{payslip.NetSalary:N0}");
                                table.Cell().Padding(3).Text(payslip.Status.ToString()).FontColor(GetStatusColor(payslip.Status));
                            }
                        });
                    });

                    // Footer
                    page.Footer().Height(30).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Text("Confidential Document").FontSize(8).FontColor(Colors.Grey.Darken1);
                        row.ConstantItem(150).AlignRight().Text($"Generated on {DateTime.Now:MMM dd, yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            }).GeneratePdf();
        }

        private string GetStatusColor(Models.Enums.PayslipStatus status)
        {
            return status switch
            {
                Models.Enums.PayslipStatus.Draft => Colors.Orange.Medium,
                Models.Enums.PayslipStatus.Generated => Colors.Blue.Medium,
                Models.Enums.PayslipStatus.Approved => Colors.Green.Medium,
                Models.Enums.PayslipStatus.Sent => Colors.Purple.Medium,
                Models.Enums.PayslipStatus.Paid => Colors.Green.Darken1,
                _ => Colors.Grey.Medium
            };
        }
    }
}