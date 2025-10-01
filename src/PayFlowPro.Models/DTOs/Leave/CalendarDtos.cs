namespace PayFlowPro.Models.DTOs.Leave
{
    public class CalendarLeaveRequestDto
    {
        public int Id { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public string LeaveTypeCode { get; set; } = string.Empty;
        public string LeaveTypeColor { get; set; } = "#007bff";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysRequested { get; set; }
        public bool IsHalfDay { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? ApproverComments { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedBy { get; set; }
        public string? ReviewerName { get; set; }
    }

    public class CalendarLeaveFilterDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? LeaveTypeId { get; set; }
        public string? Status { get; set; }
        public int? DepartmentId { get; set; }
        public int? EmployeeId { get; set; }
    }

    public class CalendarTeamAvailabilityDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalEmployees { get; set; }
        public int AvailableEmployees { get; set; }
        public int OnLeaveEmployees { get; set; }
        public decimal AvailabilityPercentage { get; set; }
        public List<CalendarLeaveRequestDto> CurrentLeaves { get; set; } = new();
    }
}