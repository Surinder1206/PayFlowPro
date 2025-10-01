using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Models.Entities
{
    public class LeaveType
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        // Annual allocation for this leave type
        public decimal AnnualAllocation { get; set; }

        // Whether this leave type can be carried over to next year
        public bool CanCarryOver { get; set; }

        // Maximum days that can be carried over
        public decimal MaxCarryOverDays { get; set; }

        // Whether this leave type requires approval
        public bool RequiresApproval { get; set; }

        // Minimum advance notice required in days
        public int MinimumNoticeRequiredDays { get; set; }

        // Maximum consecutive days allowed
        public decimal MaxConsecutiveDays { get; set; }

        // Whether this leave type is paid
        public bool IsPaid { get; set; }

        // Whether this leave type is active
        public bool IsActive { get; set; }

        // Leave accrual frequency (Monthly, Quarterly, Annually)
        public LeaveAccrualFrequency AccrualFrequency { get; set; }

        // Gender restriction (if any)
        public LeaveGenderRestriction? GenderRestriction { get; set; }

        // Color code for UI display
        [MaxLength(7)]
        public string ColorCode { get; set; } = "#007bff";

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }

    public enum LeaveAccrualFrequency
    {
        Monthly = 1,
        Quarterly = 2,
        Annually = 3
    }

    public enum LeaveGenderRestriction
    {
        Male = 1,
        Female = 2
    }
}