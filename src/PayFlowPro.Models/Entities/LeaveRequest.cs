using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayFlowPro.Models.Entities
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int LeaveTypeId { get; set; }

        // Leave request number for tracking
        [Required]
        [MaxLength(20)]
        public string RequestNumber { get; set; } = string.Empty;

        // Start date of leave
        [Required]
        public DateTime StartDate { get; set; }

        // End date of leave
        [Required]
        public DateTime EndDate { get; set; }

        // Number of leave days requested
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal DaysRequested { get; set; }

        // Reason for leave
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        // Emergency contact during leave
        [MaxLength(200)]
        public string EmergencyContact { get; set; } = string.Empty;

        // Address during leave
        [MaxLength(500)]
        public string AddressDuringLeave { get; set; } = string.Empty;

        // Phone number during leave
        [MaxLength(20)]
        public string PhoneDuringLeave { get; set; } = string.Empty;

        // Leave request status
        public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

        // Date when request was submitted
        public DateTime SubmittedAt { get; set; }

        // User ID who submitted the request
        [MaxLength(450)]
        public string SubmittedBy { get; set; } = string.Empty;

        // Manager/Approver comments
        [MaxLength(500)]
        public string ApproverComments { get; set; } = string.Empty;

        // Date when request was reviewed
        public DateTime? ReviewedAt { get; set; }

        // User ID who reviewed the request
        [MaxLength(450)]
        public string ReviewedBy { get; set; } = string.Empty;

        // Priority of the leave request
        public LeaveRequestPriority Priority { get; set; } = LeaveRequestPriority.Normal;

        // Whether this is a half-day leave
        public bool IsHalfDay { get; set; }

        // Half-day session (Morning/Afternoon) if applicable
        public HalfDaySession? HalfDaySession { get; set; }

        // Supporting documents attached
        public bool HasSupportingDocuments { get; set; }

        // Auto-approval eligible
        public bool IsAutoApprovalEligible { get; set; }

        // Financial year when leave was/will be taken
        public int FinancialYear { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Employee Employee { get; set; } = null!;
        public virtual LeaveType LeaveType { get; set; } = null!;
        public virtual ICollection<LeaveRequestApproval> Approvals { get; set; } = new List<LeaveRequestApproval>();
    }

    public enum LeaveRequestStatus
    {
        Draft = 1,
        Pending = 2,
        Approved = 3,
        Rejected = 4,
        Cancelled = 5,
        InProgress = 6,
        Completed = 7
    }

    public enum LeaveRequestPriority
    {
        Low = 1,
        Normal = 2,
        High = 3,
        Emergency = 4
    }

    public enum HalfDaySession
    {
        Morning = 1,
        Afternoon = 2
    }
}