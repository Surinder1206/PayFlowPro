using PayFlowPro.Models.Entities;

namespace PayFlowPro.Core.Interfaces
{
    /// <summary>
    /// Interface for PDF generation services
    /// </summary>
    public interface IPdfService
    {
        /// <summary>
        /// Generate a PDF document for a payslip
        /// </summary>
        /// <param name="payslip">The payslip to generate PDF for</param>
        /// <returns>PDF document as byte array</returns>
        Task<byte[]> GeneratePayslipPdfAsync(Payslip payslip);

        /// <summary>
        /// Generate a PDF document for multiple payslips
        /// </summary>
        /// <param name="payslips">List of payslips to generate PDF for</param>
        /// <returns>PDF document as byte array</returns>
        Task<byte[]> GenerateBatchPayslipsPdfAsync(IEnumerable<Payslip> payslips);

        /// <summary>
        /// Generate a salary statement PDF for an employee
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="fromDate">Start date for the statement</param>
        /// <param name="toDate">End date for the statement</param>
        /// <returns>PDF document as byte array</returns>
        Task<byte[]> GenerateSalaryStatementPdfAsync(int employeeId, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Generate a payroll summary PDF for a specific period
        /// </summary>
        /// <param name="fromDate">Start date for the summary</param>
        /// <param name="toDate">End date for the summary</param>
        /// <returns>PDF document as byte array</returns>
        Task<byte[]> GeneratePayrollSummaryPdfAsync(DateTime fromDate, DateTime toDate);
    }
}