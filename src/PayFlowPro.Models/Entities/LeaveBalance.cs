using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayFlowPro.Models.Entities
{
    public class LeaveBalance
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int LeaveTypeId { get; set; }

        // Financial year for this leave balance
        [Required]
        public int FinancialYear { get; set; }

        // Total allocated leave days for the year
        [Column(TypeName = "decimal(10,2)")]
        public decimal AllocatedDays { get; set; }

        // Leave days used/consumed
        [Column(TypeName = "decimal(10,2)")]
        public decimal UsedDays { get; set; }

        // Leave days carried over from previous year
        [Column(TypeName = "decimal(10,2)")]
        public decimal CarriedOverDays { get; set; }

        // Leave days that will expire if not used
        [Column(TypeName = "decimal(10,2)")]
        public decimal ExpiringDays { get; set; }

        // Date when expiring days will expire
        public DateTime? ExpiryDate { get; set; }

        // Accrued leave days (for monthly/quarterly accrual)
        [Column(TypeName = "decimal(10,2)")]
        public decimal AccruedDays { get; set; }

        // Pending leave days (from submitted but not approved requests)
        [Column(TypeName = "decimal(10,2)")]
        public decimal PendingDays { get; set; }

        // Available leave days (calculated field)
        [NotMapped]
        public decimal AvailableDays => AllocatedDays + CarriedOverDays + AccruedDays - UsedDays - PendingDays;

        // Last accrual processing date
        public DateTime? LastAccrualProcessed { get; set; }

        // Comments or notes
        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Employee Employee { get; set; } = null!;
        public virtual LeaveType LeaveType { get; set; } = null!;
    }
}