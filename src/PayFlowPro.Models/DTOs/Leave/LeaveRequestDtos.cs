using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Models.DTOs.Leave
{
    // Summary DTOs for dashboard display
    public class LeaveSummaryDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public int FinancialYear { get; set; }
        public decimal TotalAllocated { get; set; }
        public decimal TotalUsed { get; set; }
        public decimal TotalAvailable { get; set; }
        public decimal TotalPending { get; set; }
        public decimal TotalCarriedOver { get; set; }
        public List<LeaveTypeBalanceDto> LeaveTypeBalances { get; set; } = new();
    }

    public class LeaveTypeBalanceDto
    {
        public int Id { get; set; }
        public int LeaveTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LeaveTypeName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string LeaveTypeCode { get; set; } = string.Empty;
        public string ColorCode { get; set; } = "#007bff";
        public decimal AllocatedDays { get; set; }
        public decimal UsedDays { get; set; }
        public decimal AvailableDays { get; set; }
        public decimal PendingDays { get; set; }
        public decimal CarriedOverDays { get; set; }
        public decimal ExpiringDays { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool CanCarryOver { get; set; }
        public decimal MaxCarryOverDays { get; set; }
        public bool RequiresDocuments { get; set; }
    }

    // Leave Request DTOs
    public class LeaveRequestDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        
        public decimal DaysRequested { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string EmergencyContact { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string AddressDuringLeave { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string PhoneDuringLeave { get; set; } = string.Empty;
        
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public string SubmittedBy { get; set; } = string.Empty;
        public string ApproverComments { get; set; } = string.Empty;
        public DateTime? ReviewedAt { get; set; }
        public string ReviewedBy { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public bool IsHalfDay { get; set; }
        public string HalfDaySession { get; set; } = string.Empty;
        public bool HasSupportingDocuments { get; set; }
        public int FinancialYear { get; set; }
        
        // Related information
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public string LeaveTypeColor { get; set; } = "#007bff";
        public DateTime? ApprovedAt { get; set; }
        public string ApproverName { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
        public List<LeaveRequestApprovalDto> Approvals { get; set; } = new();
        
        // Navigation properties for email notifications
        public EmployeeBasicDto? Employee { get; set; }
        public LeaveTypeDto? LeaveType { get; set; }
    }

    public class CreateLeaveRequestDto
    {
        public int EmployeeId { get; set; }
        
        [Required]
        public int LeaveTypeId { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string EmergencyContact { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string AddressDuringLeave { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string PhoneDuringLeave { get; set; } = string.Empty;
        
        public string Priority { get; set; } = "Normal";
        public string Status { get; set; } = "Pending";
        public bool IsHalfDay { get; set; }
        public string HalfDaySession { get; set; } = string.Empty;
        public bool HasSupportingDocuments { get; set; }
        public string? Comments { get; set; }
        public decimal DaysRequested { get; set; }
    }

    public class UpdateLeaveRequestDto
    {
        [Required]
        public int Id { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string EmergencyContact { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string AddressDuringLeave { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string PhoneDuringLeave { get; set; } = string.Empty;
        
        public string Priority { get; set; } = "Normal";
        public bool IsHalfDay { get; set; }
        public string HalfDaySession { get; set; } = string.Empty;
        public bool HasSupportingDocuments { get; set; }
    }

    // Leave Approval DTOs
    public class LeaveRequestApprovalDto
    {
        public int Id { get; set; }
        public int LeaveRequestId { get; set; }
        public int ApprovalLevel { get; set; }
        public string ApproverId { get; set; } = string.Empty;
        public string ApproverName { get; set; } = string.Empty;
        public string ApproverEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public DateTime? ApprovedAt { get; set; }
        public bool IsRequired { get; set; }
        public bool IsNotified { get; set; }
        public DateTime? NotifiedAt { get; set; }
        public DateTime? DeadlineAt { get; set; }
        public bool CanDelegate { get; set; }
        public string DelegatedTo { get; set; } = string.Empty;
    }

    public class ApproveLeaveRequestDto
    {
        [Required]
        public int LeaveRequestId { get; set; }
        
        [Required]
        public int ApprovalLevel { get; set; }
        
        [MaxLength(500)]
        public string Comments { get; set; } = string.Empty;
        
        [Required]
        public bool IsApproved { get; set; }
    }

    // Leave Type DTOs
    public class LeaveTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal AnnualAllocation { get; set; }
        public bool CanCarryOver { get; set; }
        public decimal MaxCarryOverDays { get; set; }
        public bool RequiresApproval { get; set; }
        public int MinimumNoticeRequiredDays { get; set; }
        public decimal MaxConsecutiveDays { get; set; }
        public bool IsPaid { get; set; }
        public bool IsActive { get; set; }
        public string AccrualFrequency { get; set; } = string.Empty;
        public string GenderRestriction { get; set; } = string.Empty;
        public string ColorCode { get; set; } = "#007bff";
    }

    public class CreateLeaveTypeDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Range(0, 365)]
        public decimal AnnualAllocation { get; set; }
        
        public bool CanCarryOver { get; set; }
        
        [Range(0, 365)]
        public decimal MaxCarryOverDays { get; set; }
        
        public bool RequiresApproval { get; set; } = true;
        
        [Range(0, 365)]
        public int MinimumNoticeRequiredDays { get; set; } = 1;
        
        [Range(0, 365)]
        public decimal MaxConsecutiveDays { get; set; } = 365;
        
        public bool IsPaid { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public string AccrualFrequency { get; set; } = "Annually";
        public string GenderRestriction { get; set; } = string.Empty;
        
        [MaxLength(7)]
        public string ColorCode { get; set; } = "#007bff";
    }

    // Basic DTOs for email notifications and simplified data transfer
    public class EmployeeBasicDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    // Auto-Approval Rule DTOs
    public class CreateAutoApprovalRuleDto
    {
        [Required]
        [StringLength(100)]
        public string RuleName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int? DepartmentId { get; set; }

        public int? LeaveTypeId { get; set; }

        [StringLength(50)]
        public string? EmployeeLevel { get; set; }

        [Range(0.5, 365)]
        public decimal? MaxDaysAllowed { get; set; }

        [Range(1, 365)]
        public int? MinNoticeRequiredDays { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(1, 100)]
        public int Priority { get; set; } = 1;

        public string? CustomConditionsJson { get; set; }
    }

    public class UpdateAutoApprovalRuleDto : CreateAutoApprovalRuleDto
    {
        public int Id { get; set; }
    }
}