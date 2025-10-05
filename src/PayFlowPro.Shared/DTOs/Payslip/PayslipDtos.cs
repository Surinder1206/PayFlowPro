using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Shared.DTOs.Payslip
{
    public class PayslipDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;

        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }
        public DateTime PayDate { get; set; }

        public decimal BaseSalary { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal OvertimeRate { get; set; }

        public decimal GrossPay { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPay { get; set; }

        public List<PayslipAllowanceDto> Allowances { get; set; } = new();
        public List<PayslipDeductionDto> Deductions { get; set; } = new();

        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class PayslipAllowanceDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsTaxable { get; set; }
    }

    public class PayslipDeductionDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsPreTax { get; set; }
    }

    public class CreatePayslipDto
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public DateTime PayPeriodStart { get; set; }

        [Required]
        public DateTime PayPeriodEnd { get; set; }

        public DateTime? PayDate { get; set; }
        public decimal HoursWorked { get; set; } = 160;
        public decimal OvertimeHours { get; set; } = 0;
        public string? Notes { get; set; }

        public List<CreatePayslipAllowanceDto> Allowances { get; set; } = new();
        public List<CreatePayslipDeductionDto> Deductions { get; set; } = new();
    }

    public class CreatePayslipAllowanceDto
    {
        [Required]
        public int AllowanceTypeId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
    }

    public class CreatePayslipDeductionDto
    {
        [Required]
        public int DeductionTypeId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
    }

    public class GeneratePayslipRequestDto
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public DateTime PayPeriodStart { get; set; }

        [Required]
        public DateTime PayPeriodEnd { get; set; }

        public decimal HoursWorked { get; set; } = 160;
        public decimal OvertimeHours { get; set; } = 0;
        public string? Notes { get; set; }
    }

    public class PayslipFilterDto
    {
        public int? EmployeeId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string? Department { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;
    }

    public class PayslipSummaryDto
    {
        public int TotalPayslips { get; set; }
        public decimal TotalGrossPay { get; set; }
        public decimal TotalNetPay { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal AverageNetPay { get; set; }
        public List<PayslipDto> RecentPayslips { get; set; } = new();
    }

    public class PayslipCalculationResult
    {
        public decimal GrossPay { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal IncomeTax { get; set; }
        public decimal NationalInsurance { get; set; }
        public decimal TotalTax => IncomeTax + NationalInsurance;
        public decimal NetPay { get; set; }
        public List<PayslipAllowanceDto> Allowances { get; set; } = new();
        public List<PayslipDeductionDto> Deductions { get; set; } = new();
        public Dictionary<string, decimal> TaxBreakdown { get; set; } = new();
    }
}