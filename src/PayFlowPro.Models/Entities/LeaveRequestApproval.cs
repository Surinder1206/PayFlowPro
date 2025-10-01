using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Models.Entities
{
    public class LeaveRequestApproval
    {
        public int Id { get; set; }

        [Required]
        public int LeaveRequestId { get; set; }

        // Approver level (1st level manager, 2nd level, HR, etc.)
        public int ApprovalLevel { get; set; }

        // User ID of the approver
        [Required]
        [MaxLength(450)]
        public string ApproverId { get; set; } = string.Empty;

        // Name of the approver (for display)
        [Required]
        [MaxLength(200)]
        public string ApproverName { get; set; } = string.Empty;

        // Email of the approver
        [Required]
        [MaxLength(200)]
        public string ApproverEmail { get; set; } = string.Empty;

        // Approval status
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        // Comments from approver
        [MaxLength(500)]
        public string Comments { get; set; } = string.Empty;

        // Date when approval was given/rejected
        public DateTime? ApprovedAt { get; set; }

        // Whether this approval is required or optional
        public bool IsRequired { get; set; } = true;

        // Whether this approver was notified
        public bool IsNotified { get; set; }

        // Date when approver was notified
        public DateTime? NotifiedAt { get; set; }

        // Approval deadline
        public DateTime? DeadlineAt { get; set; }

        // Whether approver can delegate this approval
        public bool CanDelegate { get; set; }

        // Delegated to user ID (if delegated)
        [MaxLength(450)]
        public string DelegatedTo { get; set; } = string.Empty;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual LeaveRequest LeaveRequest { get; set; } = null!;
    }

    public enum ApprovalStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Delegated = 4,
        Expired = 5
    }
}