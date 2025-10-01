using PayFlowPro.Models.Entities;
using PayFlowPro.Models.Enums;

namespace PayFlowPro.Core.Interfaces;

public interface IPayslipService
{
    Task<Payslip> GeneratePayslipAsync(int employeeId, DateTime payPeriodStart, DateTime payPeriodEnd);
    Task<Payslip?> GetPayslipByIdAsync(int payslipId);
    Task<List<Payslip>> GetPayslipsByEmployeeAsync(int employeeId, int? year = null, int? month = null);
    Task<List<Payslip>> GetPayslipsForPeriodAsync(DateTime startDate, DateTime endDate);
    Task<Payslip> ApprovePayslipAsync(int payslipId, string approvedBy);
    Task<Payslip> UpdatePayslipStatusAsync(int payslipId, PayslipStatus status);
    Task<bool> DeletePayslipAsync(int payslipId);
    Task<string> GeneratePayslipNumberAsync(int employeeId, DateTime payPeriod);
}