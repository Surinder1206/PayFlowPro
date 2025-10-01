using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayFlowPro.Core.Interfaces;
using PayFlowPro.Models.DTOs.Leave;
using PayFlowPro.Shared.DTOs;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace PayFlowPro.Core.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly SmtpClient _smtpClient;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _smtpClient = ConfigureSmtpClient();
        }

        private SmtpClient ConfigureSmtpClient()
        {
            var smtpHost = _configuration["Email:SmtpHost"] ?? "localhost";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];

            var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            return client;
        }

        public async Task SendLeaveApprovalNotificationAsync(LeaveRequestDto leaveRequest, string approverName, string approverComments)
        {
            try
            {
                var template = GenerateApprovalEmailTemplate(leaveRequest, approverName, approverComments);
                await SendEmailAsync(template);
                
                _logger.LogInformation("Leave approval notification sent to {EmployeeEmail} for request {RequestId}", 
                    leaveRequest.Employee?.Email, leaveRequest.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send leave approval notification for request {RequestId}", leaveRequest.Id);
            }
        }

        public async Task SendLeaveRejectionNotificationAsync(LeaveRequestDto leaveRequest, string approverName, string approverComments)
        {
            try
            {
                var template = GenerateRejectionEmailTemplate(leaveRequest, approverName, approverComments);
                await SendEmailAsync(template);
                
                _logger.LogInformation("Leave rejection notification sent to {EmployeeEmail} for request {RequestId}", 
                    leaveRequest.Employee?.Email, leaveRequest.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send leave rejection notification for request {RequestId}", leaveRequest.Id);
            }
        }

        public async Task SendLeaveRequestNotificationAsync(LeaveRequestDto leaveRequest, List<string> approverEmails)
        {
            try
            {
                var template = GenerateRequestNotificationTemplate(leaveRequest, approverEmails);
                await SendEmailAsync(template);
                
                _logger.LogInformation("Leave request notification sent to approvers for request {RequestId}", leaveRequest.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send leave request notification for request {RequestId}", leaveRequest.Id);
            }
        }

        public async Task SendLeaveBalanceExpiryNotificationAsync(string employeeEmail, string employeeName, int expiringDays, DateTime expiryDate, string leaveTypeName)
        {
            try
            {
                var template = GenerateBalanceExpiryTemplate(employeeEmail, employeeName, expiringDays, expiryDate, leaveTypeName);
                await SendEmailAsync(template);
                
                _logger.LogInformation("Leave balance expiry notification sent to {EmployeeEmail}", employeeEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send leave balance expiry notification to {EmployeeEmail}", employeeEmail);
            }
        }

        public async Task SendLeaveRequestReminderAsync(string approverEmail, List<LeaveRequestDto> pendingRequests)
        {
            try
            {
                var template = GenerateReminderTemplate(approverEmail, pendingRequests);
                await SendEmailAsync(template);
                
                _logger.LogInformation("Leave request reminder sent to {ApproverEmail} for {Count} pending requests", 
                    approverEmail, pendingRequests.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send leave request reminder to {ApproverEmail}", approverEmail);
            }
        }

        private async Task SendEmailAsync(EmailTemplate template)
        {
            var fromEmail = _configuration["Email:FromAddress"] ?? "noreply@payflowpro.com";
            var fromName = _configuration["Email:FromName"] ?? "PayFlow Pro";

            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail, fromName);
            
            foreach (var email in template.ToEmails)
            {
                message.To.Add(email);
            }

            if (template.CcEmails != null)
            {
                foreach (var email in template.CcEmails)
                {
                    message.CC.Add(email);
                }
            }

            message.Subject = template.Subject;
            message.Body = template.Body;
            message.IsBodyHtml = true;

            await _smtpClient.SendMailAsync(message);
        }

        private EmailTemplate GenerateApprovalEmailTemplate(LeaveRequestDto leaveRequest, string approverName, string approverComments)
        {
            var subject = $"Leave Request Approved - {leaveRequest.LeaveType?.Name} ({leaveRequest.RequestNumber})";
            
            var body = new StringBuilder();
            body.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            body.AppendLine($"<h2 style='color: #28a745;'>Leave Request Approved ✅</h2>");
            body.AppendLine($"<p>Dear {leaveRequest.Employee?.FirstName} {leaveRequest.Employee?.LastName},</p>");
            body.AppendLine("<p>Great news! Your leave request has been <strong>approved</strong>.</p>");
            
            body.AppendLine("<div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>");
            body.AppendLine("<h3>Request Details:</h3>");
            body.AppendLine($"<p><strong>Request Number:</strong> {leaveRequest.RequestNumber}</p>");
            body.AppendLine($"<p><strong>Leave Type:</strong> {leaveRequest.LeaveType?.Name}</p>");
            body.AppendLine($"<p><strong>Start Date:</strong> {leaveRequest.StartDate:MMM dd, yyyy}</p>");
            body.AppendLine($"<p><strong>End Date:</strong> {leaveRequest.EndDate:MMM dd, yyyy}</p>");
            body.AppendLine($"<p><strong>Duration:</strong> {leaveRequest.DaysRequested} day(s)</p>");
            body.AppendLine($"<p><strong>Approved By:</strong> {approverName}</p>");
            
            if (!string.IsNullOrEmpty(approverComments))
            {
                body.AppendLine($"<p><strong>Approver Comments:</strong> {approverComments}</p>");
            }
            body.AppendLine("</div>");
            
            body.AppendLine("<p>Please ensure proper handover of your responsibilities before your leave begins.</p>");
            body.AppendLine("<p>Have a great time off!</p>");
            body.AppendLine("<br/><p>Best regards,<br/>PayFlow Pro</p>");
            body.AppendLine("</body></html>");

            return new EmailTemplate
            {
                Subject = subject,
                Body = body.ToString(),
                ToEmails = new List<string> { leaveRequest.Employee?.Email ?? "" }
            };
        }

        private EmailTemplate GenerateRejectionEmailTemplate(LeaveRequestDto leaveRequest, string approverName, string approverComments)
        {
            var subject = $"Leave Request Rejected - {leaveRequest.LeaveType?.Name} ({leaveRequest.RequestNumber})";
            
            var body = new StringBuilder();
            body.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            body.AppendLine($"<h2 style='color: #dc3545;'>Leave Request Rejected ❌</h2>");
            body.AppendLine($"<p>Dear {leaveRequest.Employee?.FirstName} {leaveRequest.Employee?.LastName},</p>");
            body.AppendLine("<p>We regret to inform you that your leave request has been <strong>rejected</strong>.</p>");
            
            body.AppendLine("<div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>");
            body.AppendLine("<h3>Request Details:</h3>");
            body.AppendLine($"<p><strong>Request Number:</strong> {leaveRequest.RequestNumber}</p>");
            body.AppendLine($"<p><strong>Leave Type:</strong> {leaveRequest.LeaveType?.Name}</p>");
            body.AppendLine($"<p><strong>Start Date:</strong> {leaveRequest.StartDate:MMM dd, yyyy}</p>");
            body.AppendLine($"<p><strong>End Date:</strong> {leaveRequest.EndDate:MMM dd, yyyy}</p>");
            body.AppendLine($"<p><strong>Duration:</strong> {leaveRequest.DaysRequested} day(s)</p>");
            body.AppendLine($"<p><strong>Rejected By:</strong> {approverName}</p>");
            
            if (!string.IsNullOrEmpty(approverComments))
            {
                body.AppendLine($"<p><strong>Reason for Rejection:</strong> {approverComments}</p>");
            }
            body.AppendLine("</div>");
            
            body.AppendLine("<p>If you have any questions or would like to discuss this decision, please contact your manager or HR.</p>");
            body.AppendLine("<p>You can also submit a new request with different dates if needed.</p>");
            body.AppendLine("<br/><p>Best regards,<br/>PayFlow Pro</p>");
            body.AppendLine("</body></html>");

            return new EmailTemplate
            {
                Subject = subject,
                Body = body.ToString(),
                ToEmails = new List<string> { leaveRequest.Employee?.Email ?? "" }
            };
        }

        private EmailTemplate GenerateRequestNotificationTemplate(LeaveRequestDto leaveRequest, List<string> approverEmails)
        {
            var subject = $"New Leave Request - {leaveRequest.Employee?.FirstName} {leaveRequest.Employee?.LastName} ({leaveRequest.RequestNumber})";
            
            var body = new StringBuilder();
            body.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            body.AppendLine($"<h2 style='color: #007bff;'>New Leave Request Pending Approval 📋</h2>");
            body.AppendLine("<p>Dear Approver,</p>");
            body.AppendLine("<p>A new leave request has been submitted and requires your approval.</p>");
            
            body.AppendLine("<div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>");
            body.AppendLine("<h3>Request Details:</h3>");
            body.AppendLine($"<p><strong>Employee:</strong> {leaveRequest.Employee?.FirstName} {leaveRequest.Employee?.LastName}</p>");
            body.AppendLine($"<p><strong>Request Number:</strong> {leaveRequest.RequestNumber}</p>");
            body.AppendLine($"<p><strong>Leave Type:</strong> {leaveRequest.LeaveType?.Name}</p>");
            body.AppendLine($"<p><strong>Start Date:</strong> {leaveRequest.StartDate:MMM dd, yyyy}</p>");
            body.AppendLine($"<p><strong>End Date:</strong> {leaveRequest.EndDate:MMM dd, yyyy}</p>");
            body.AppendLine($"<p><strong>Duration:</strong> {leaveRequest.DaysRequested} day(s)</p>");
            body.AppendLine($"<p><strong>Submitted:</strong> {leaveRequest.SubmittedAt:MMM dd, yyyy HH:mm}</p>");
            
            if (!string.IsNullOrEmpty(leaveRequest.Reason))
            {
                body.AppendLine($"<p><strong>Reason:</strong> {leaveRequest.Reason}</p>");
            }
            body.AppendLine("</div>");
            
            body.AppendLine("<p>Please log in to the system to review and process this request.</p>");
            body.AppendLine($"<p><a href='#' style='background-color: #007bff; color: white; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>Review Request</a></p>");
            body.AppendLine("<br/><p>Best regards,<br/>PayFlow Pro</p>");
            body.AppendLine("</body></html>");

            return new EmailTemplate
            {
                Subject = subject,
                Body = body.ToString(),
                ToEmails = approverEmails
            };
        }

        private EmailTemplate GenerateBalanceExpiryTemplate(string employeeEmail, string employeeName, int expiringDays, DateTime expiryDate, string leaveTypeName)
        {
            var subject = $"Leave Balance Expiry Reminder - {leaveTypeName}";
            
            var body = new StringBuilder();
            body.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            body.AppendLine($"<h2 style='color: #ffc107;'>Leave Balance Expiry Reminder ⚠️</h2>");
            body.AppendLine($"<p>Dear {employeeName},</p>");
            body.AppendLine("<p>This is a reminder that some of your leave balance is about to expire.</p>");
            
            body.AppendLine("<div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #ffc107;'>");
            body.AppendLine("<h3>Expiry Details:</h3>");
            body.AppendLine($"<p><strong>Leave Type:</strong> {leaveTypeName}</p>");
            body.AppendLine($"<p><strong>Expiring Days:</strong> {expiringDays} day(s)</p>");
            body.AppendLine($"<p><strong>Expiry Date:</strong> {expiryDate:MMM dd, yyyy}</p>");
            body.AppendLine("</div>");
            
            body.AppendLine("<p>Please plan your leave accordingly to utilize your available balance before it expires.</p>");
            body.AppendLine("<p>Contact HR if you have any questions about your leave balance.</p>");
            body.AppendLine("<br/><p>Best regards,<br/>PayFlow Pro</p>");
            body.AppendLine("</body></html>");

            return new EmailTemplate
            {
                Subject = subject,
                Body = body.ToString(),
                ToEmails = new List<string> { employeeEmail }
            };
        }

        private EmailTemplate GenerateReminderTemplate(string approverEmail, List<LeaveRequestDto> pendingRequests)
        {
            var subject = $"Leave Request Reminder - {pendingRequests.Count} Pending Approval(s)";
            
            var body = new StringBuilder();
            body.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            body.AppendLine($"<h2 style='color: #fd7e14;'>Pending Leave Requests Reminder 🔔</h2>");
            body.AppendLine("<p>Dear Approver,</p>");
            body.AppendLine($"<p>You have <strong>{pendingRequests.Count}</strong> leave request(s) pending your approval.</p>");
            
            body.AppendLine("<div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>");
            body.AppendLine("<h3>Pending Requests:</h3>");
            
            foreach (var request in pendingRequests.Take(5)) // Show max 5 requests
            {
                body.AppendLine("<div style='border-bottom: 1px solid #dee2e6; padding: 10px 0;'>");
                body.AppendLine($"<p><strong>{request.Employee?.FirstName} {request.Employee?.LastName}</strong> - {request.LeaveType?.Name}</p>");
                body.AppendLine($"<p>📅 {request.StartDate:MMM dd} to {request.EndDate:MMM dd, yyyy} ({request.DaysRequested} days)</p>");
                body.AppendLine($"<p>📝 Request: {request.RequestNumber}</p>");
                body.AppendLine("</div>");
            }
            
            if (pendingRequests.Count > 5)
            {
                body.AppendLine($"<p><em>... and {pendingRequests.Count - 5} more request(s)</em></p>");
            }
            
            body.AppendLine("</div>");
            
            body.AppendLine("<p>Please log in to the system to review and process these requests promptly.</p>");
            body.AppendLine($"<p><a href='#' style='background-color: #fd7e14; color: white; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>Review Requests</a></p>");
            body.AppendLine("<br/><p>Best regards,<br/>PayFlow Pro</p>");
            body.AppendLine("</body></html>");

            return new EmailTemplate
            {
                Subject = subject,
                Body = body.ToString(),
                ToEmails = new List<string> { approverEmail }
            };
        }

        public void Dispose()
        {
            _smtpClient?.Dispose();
        }
    }
}