using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.Entities;
using System.Text;

namespace PayFlowPro.Blazor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for PDF operations
    public class PdfController : ControllerBase
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly ILogger<PdfController> _logger;

        public PdfController(
            IDbContextFactory<ApplicationDbContext> dbContextFactory,
            ILogger<PdfController> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Download payslip as HTML report that can be saved as PDF
        /// </summary>
        [HttpGet("payslip/{payslipId}")]
        [Authorize] // Any authenticated user can download payslips
        public async Task<IActionResult> DownloadPayslip(int payslipId)
        {
            try
            {
                _logger.LogInformation("Download request for payslip {PayslipId} from user {User}", payslipId, User?.Identity?.Name ?? "Unknown");
                _logger.LogInformation("User authenticated: {IsAuthenticated}, Claims: {Claims}", User?.Identity?.IsAuthenticated, string.Join(", ", User?.Claims?.Select(c => $"{c.Type}:{c.Value}") ?? new[] {"No claims"}));
                
                using var context = _dbContextFactory.CreateDbContext();
                var payslip = await context.Payslips
                    .Include(p => p.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(p => p.Employee)
                        .ThenInclude(e => e.Company)
                    .FirstOrDefaultAsync(p => p.Id == payslipId);
                
                if (payslip == null)
                {
                    _logger.LogWarning("Payslip {PayslipId} not found", payslipId);
                    return NotFound("Payslip not found");
                }

                var htmlContent = GeneratePayslipHtml(payslip);
                var htmlBytes = Encoding.UTF8.GetBytes(htmlContent);
                
                var fileName = $"Payslip_{payslip.Employee.EmployeeCode}_{payslip.PayPeriodStart:yyyy_MM}.html";
                
                _logger.LogInformation("Generated payslip HTML file {FileName} with size {Size} bytes", fileName, htmlBytes.Length);
                
                // Set headers for file download
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                Response.Headers.Add("Content-Length", htmlBytes.Length.ToString());
                
                return File(htmlBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for payslip {PayslipId}", payslipId);
                return StatusCode(500, "Error generating PDF");
            }
        }


        // TODO: Implement batch PDF download functionality when needed
        /*
        /// <summary>
        /// Download multiple payslips as a single PDF
        /// </summary>
        [HttpPost("payslips/batch")]
        public async Task<IActionResult> DownloadBatchPayslips([FromBody] int[] payslipIds)
        {
            // Implementation pending - requires batch PDF generation
            return StatusCode(501, "Batch PDF download not yet implemented");
        }
        */

        // TODO: Implement salary statement functionality when needed
        /*
        /// <summary>
        /// Download salary statement for an employee
        /// </summary>
        [HttpGet("salary-statement/{employeeId}")]
        public async Task<IActionResult> DownloadSalaryStatement(
            int employeeId, 
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate)
        {
            // Implementation pending - requires salary statement generation
            return StatusCode(501, "Salary statement download not yet implemented");
        }
        */

        // TODO: Implement payroll summary functionality when needed
        /*
        /// <summary>
        /// Download payroll summary report
        /// </summary>
        [HttpGet("payroll-summary")]
        public async Task<IActionResult> DownloadPayrollSummary(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate)
        {
            // Implementation pending - requires payroll summary generation
            return StatusCode(501, "Payroll summary download not yet implemented");
        }
        */

        /// <summary>
        /// Preview payslip as HTML report in browser
        /// </summary>
        [HttpGet("payslip/{payslipId}/preview")]
        [Authorize] // Any authenticated user can preview payslips
        public async Task<IActionResult> PreviewPayslip(int payslipId)
        {
            try
            {
                _logger.LogInformation("Preview request for payslip {PayslipId} from user {User}", payslipId, User?.Identity?.Name ?? "Unknown");
                using var context = _dbContextFactory.CreateDbContext();
                var payslip = await context.Payslips
                    .Include(p => p.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(p => p.Employee)
                        .ThenInclude(e => e.Company)
                    .FirstOrDefaultAsync(p => p.Id == payslipId);
                
                if (payslip == null)
                {
                    return NotFound("Payslip not found");
                }

                var htmlContent = GeneratePayslipHtml(payslip, true); // true for preview mode
                
                return Content(htmlContent, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing PDF for payslip {PayslipId}", payslipId);
                return StatusCode(500, "Error previewing PDF");
            }
        }

        private string GeneratePayslipHtml(Payslip payslip, bool isPreview = false)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Payslip - {payslip.Employee.FirstName} {payslip.Employee.LastName}</title>
    <meta charset='utf-8'>
    <style>
        body {{ 
            font-family: Arial, sans-serif; 
            margin: 0; 
            padding: 20px; 
            background-color: white;
        }}
        .payslip-container {{ 
            max-width: 800px; 
            margin: 0 auto; 
            background: white; 
            box-shadow: 0 0 10px rgba(0,0,0,0.1);
            padding: 30px;
        }}
        .header {{ 
            text-align: center; 
            border-bottom: 2px solid #333; 
            padding-bottom: 20px; 
            margin-bottom: 30px; 
        }}
        .company-name {{ 
            font-size: 24px; 
            font-weight: bold; 
            color: #333; 
            margin-bottom: 5px; 
        }}
        .payslip-title {{ 
            font-size: 18px; 
            color: #666; 
        }}
        .employee-info {{ 
            display: flex; 
            justify-content: space-between; 
            margin-bottom: 30px; 
        }}
        .info-section {{ 
            flex: 1; 
        }}
        .info-row {{ 
            margin: 5px 0; 
        }}
        .label {{ 
            font-weight: bold; 
            display: inline-block; 
            width: 140px; 
        }}
        .salary-table {{ 
            width: 100%; 
            border-collapse: collapse; 
            margin: 20px 0; 
        }}
        .salary-table th, .salary-table td {{ 
            border: 1px solid #ddd; 
            padding: 12px; 
            text-align: left; 
        }}
        .salary-table th {{ 
            background-color: #f8f9fa; 
            font-weight: bold; 
        }}
        .total-row {{ 
            background-color: #e9ecef; 
            font-weight: bold; 
        }}
        .net-salary {{ 
            background-color: #d4edda; 
            font-size: 18px; 
            font-weight: bold; 
        }}
        .footer {{ 
            margin-top: 40px; 
            border-top: 1px solid #ddd; 
            padding-top: 20px; 
            text-align: center; 
            color: #666; 
            font-size: 12px; 
        }}
        @media print {{
            body {{ padding: 0; }}
            .payslip-container {{ box-shadow: none; }}
        }}
    </style>
    {(isPreview ? @"<script>
        function printPayslip() {
            window.print();
        }
        function downloadPayslip() {
            window.location.href = '/api/pdf/payslip/" + payslip.Id + @"';
        }
    </script>" : "")}
</head>
<body>
    <div class='payslip-container'>
        {(isPreview ? @"
        <div style='text-align: center; margin-bottom: 20px; print:hidden;'>
            <button onclick='printPayslip()' style='margin-right: 10px; padding: 10px 20px; background: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer;'>Print</button>
            <button onclick='downloadPayslip()' style='padding: 10px 20px; background: #28a745; color: white; border: none; border-radius: 4px; cursor: pointer;'>Download</button>
        </div>" : "")}
        
        <div class='header'>
            <div class='company-name'>{payslip.Employee.Company?.Name ?? "Company Name"}</div>
            <div class='payslip-title'>PAYSLIP</div>
        </div>

        <div class='employee-info'>
            <div class='info-section'>
                <div class='info-row'>
                    <span class='label'>Employee Name:</span>
                    <span>{payslip.Employee.FirstName} {payslip.Employee.LastName}</span>
                </div>
                <div class='info-row'>
                    <span class='label'>Employee Code:</span>
                    <span>{payslip.Employee.EmployeeCode}</span>
                </div>
                <div class='info-row'>
                    <span class='label'>Department:</span>
                    <span>{payslip.Employee.Department?.Name ?? "N/A"}</span>
                </div>
                <div class='info-row'>
                    <span class='label'>Job Title:</span>
                    <span>{payslip.Employee.JobTitle ?? "N/A"}</span>
                </div>
            </div>
            <div class='info-section'>
                <div class='info-row'>
                    <span class='label'>Payslip Number:</span>
                    <span>{payslip.PayslipNumber}</span>
                </div>
                <div class='info-row'>
                    <span class='label'>Pay Period:</span>
                    <span>{payslip.PayPeriodStart:dd/MM/yyyy} - {payslip.PayPeriodEnd:dd/MM/yyyy}</span>
                </div>
                <div class='info-row'>
                    <span class='label'>Pay Date:</span>
                    <span>{payslip.PayDate:dd/MM/yyyy}</span>
                </div>
                <div class='info-row'>
                    <span class='label'>Working Days:</span>
                    <span>{payslip.WorkingDays}/{payslip.ActualWorkingDays}</span>
                </div>
            </div>
        </div>

        <table class='salary-table'>
            <thead>
                <tr>
                    <th>Description</th>
                    <th style='text-align: right;'>Amount ($)</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>Basic Salary</td>
                    <td style='text-align: right;'>{payslip.BasicSalary:N2}</td>
                </tr>
                <tr>
                    <td>Total Allowances</td>
                    <td style='text-align: right;'>{payslip.TotalAllowances:N2}</td>
                </tr>
                <tr class='total-row'>
                    <td>Gross Salary</td>
                    <td style='text-align: right;'>{payslip.GrossSalary:N2}</td>
                </tr>
                <tr>
                    <td>Tax Deduction</td>
                    <td style='text-align: right;'>{payslip.TaxAmount:N2}</td>
                </tr>
                <tr>
                    <td>Other Deductions</td>
                    <td style='text-align: right;'>{(payslip.TotalDeductions - payslip.TaxAmount):N2}</td>
                </tr>
                <tr class='total-row'>
                    <td>Total Deductions</td>
                    <td style='text-align: right;'>{payslip.TotalDeductions:N2}</td>
                </tr>
                <tr class='net-salary'>
                    <td>NET SALARY</td>
                    <td style='text-align: right;'>{payslip.NetSalary:N2}</td>
                </tr>
            </tbody>
        </table>

        <div style='margin-top: 30px;'>
            <div class='info-row'>
                <span class='label'>Net Salary in Words:</span>
                <span>{ConvertToWords(payslip.NetSalary)}</span>
            </div>
        </div>

        {(payslip.Notes != null ? $@"
        <div style='margin-top: 20px;'>
            <div class='label'>Notes:</div>
            <div style='background: #f8f9fa; padding: 10px; border-radius: 4px; margin-top: 5px;'>
                {payslip.Notes}
            </div>
        </div>" : "")}

        <div class='footer'>
            <p>This is a computer-generated payslip and does not require a signature.</p>
            <p>Generated on: {DateTime.Now:dd/MM/yyyy HH:mm}</p>
        </div>
    </div>
</body>
</html>";

            return html;
        }

        private string ConvertToWords(decimal amount)
        {
            // Simple implementation - in production, use a proper number-to-words converter
            return $"{amount:N2} Dollars Only";
        }
    }
}