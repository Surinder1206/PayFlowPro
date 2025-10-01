using PayFlowPro.Models.DTOs.Leave;
using PayFlowPro.Shared.DTOs;

namespace PayFlowPro.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendLeaveApprovalNotificationAsync(LeaveRequestDto leaveRequest, string approverName, string approverComments);
        Task SendLeaveRejectionNotificationAsync(LeaveRequestDto leaveRequest, string approverName, string approverComments);
        Task SendLeaveRequestNotificationAsync(LeaveRequestDto leaveRequest, List<string> approverEmails);
        Task SendLeaveBalanceExpiryNotificationAsync(string employeeEmail, string employeeName, int expiringDays, DateTime expiryDate, string leaveTypeName);
        Task SendLeaveRequestReminderAsync(string approverEmail, List<LeaveRequestDto> pendingRequests);
    }

    public class EmailTemplate
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public List<string> ToEmails { get; set; } = new();
        public List<string>? CcEmails { get; set; }
        public Dictionary<string, string>? Attachments { get; set; }
    }
}