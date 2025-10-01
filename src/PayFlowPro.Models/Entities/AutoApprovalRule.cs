using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayFlowPro.Models.Entities
{
    public class AutoApprovalRule
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string RuleName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        // Rule execution order (lower number = higher priority)
        public int Priority { get; set; } = 1;

        // Whether this rule is active
        public bool IsActive { get; set; } = true;

        // Conditions for auto-approval

        // Maximum number of days that can be auto-approved
        [Column(TypeName = "decimal(10,2)")]
        public decimal? MaxDaysAllowed { get; set; }

        // Minimum notice period required (in days)
        public int? MinNoticeRequiredDays { get; set; }

        // Maximum consecutive days allowed for auto-approval
        [Column(TypeName = "decimal(10,2)")]
        public decimal? MaxConsecutiveDays { get; set; }

        // Minimum leave balance required for auto-approval
        [Column(TypeName = "decimal(10,2)")]
        public decimal? MinBalanceRequired { get; set; }

        // Leave types that this rule applies to (comma-separated IDs)
        [MaxLength(200)]
        public string? ApplicableLeaveTypeIds { get; set; }

        // Employee levels/roles this rule applies to
        public AutoApprovalEmployeeLevel? ApplicableEmployeeLevel { get; set; }

        // Department restrictions (comma-separated department IDs)
        [MaxLength(200)]
        public string? ApplicableDepartmentIds { get; set; }

        // Time-based restrictions

        // Days of week when auto-approval is allowed (comma-separated: 1=Monday, 7=Sunday)
        [MaxLength(20)]
        public string? AllowedDaysOfWeek { get; set; }

        // Months when auto-approval is allowed (comma-separated: 1=Jan, 12=Dec)
        [MaxLength(30)]
        public string? AllowedMonths { get; set; }

        // Blackout periods when auto-approval is not allowed
        public DateTime? BlackoutStartDate { get; set; }
        public DateTime? BlackoutEndDate { get; set; }

        // Advanced conditions

        // Whether to check for team conflicts (multiple people on leave same day)
        public bool CheckTeamConflicts { get; set; } = false;

        // Maximum percentage of team that can be on leave at same time
        [Column(TypeName = "decimal(5,2)")]
        public decimal? MaxTeamLeavePercentage { get; set; }

        // Whether to require supporting documents for auto-approval
        public bool RequireSupportingDocuments { get; set; } = false;

        // Whether half-day leaves are eligible for auto-approval
        public bool AllowHalfDayAutoApproval { get; set; } = true;

        // Whether emergency leaves bypass this rule
        public bool EmergencyBypassRule { get; set; } = true;

        // Approval actions

        // Whether to send notification when auto-approved
        public bool SendNotificationOnApproval { get; set; } = true;

        // Whether to log auto-approval action
        public bool LogAutoApprovalAction { get; set; } = true;

        // Custom conditions (JSON format for extensibility)
        [Column(TypeName = "nvarchar(max)")]
        public string? CustomConditions { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        [MaxLength(450)]
        public string UpdatedBy { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<AutoApprovalRuleLog> ApprovalLogs { get; set; } = new List<AutoApprovalRuleLog>();
    }

    public enum AutoApprovalEmployeeLevel
    {
        All = 0,
        Junior = 1,
        Senior = 2,
        Manager = 3,
        Director = 4,
        Executive = 5
    }

    // Log table for tracking auto-approval actions
    public class AutoApprovalRuleLog
    {
        public int Id { get; set; }

        [Required]
        public int AutoApprovalRuleId { get; set; }

        [Required]
        public int LeaveRequestId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        public AutoApprovalResult Result { get; set; }

        [MaxLength(500)]
        public string ResultReason { get; set; } = string.Empty;

        // Conditions that were evaluated
        [Column(TypeName = "nvarchar(max)")]
        public string EvaluationDetails { get; set; } = string.Empty;

        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string ProcessingTimeMs { get; set; } = string.Empty;

        // Navigation properties
        public virtual AutoApprovalRule AutoApprovalRule { get; set; } = null!;
        public virtual LeaveRequest LeaveRequest { get; set; } = null!;
        public virtual Employee Employee { get; set; } = null!;
    }

    public enum AutoApprovalResult
    {
        Approved = 1,
        Rejected = 2,
        RequiresManualApproval = 3,
        RuleNotApplicable = 4,
        Error = 5
    }
}