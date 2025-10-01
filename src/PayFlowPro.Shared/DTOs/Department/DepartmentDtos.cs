using System.ComponentModel.DataAnnotations;

namespace PayFlowPro.Shared.DTOs.Department
{
    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ManagerName { get; set; }
        public int? ManagerId { get; set; }
        public bool IsActive { get; set; }
        public int EmployeeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateDepartmentDto
    {
        [Required(ErrorMessage = "Department name is required")]
        [StringLength(100, ErrorMessage = "Department name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public int? ManagerId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateDepartmentDto
    {
        [Required(ErrorMessage = "Department name is required")]
        [StringLength(100, ErrorMessage = "Department name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public int? ManagerId { get; set; }
        public bool IsActive { get; set; }
    }

    public class DepartmentFilterDto
    {
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
    }

    public class DepartmentSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public int ActiveEmployeeCount { get; set; }
        public decimal TotalSalaryExpense { get; set; }
        public decimal AverageSalary { get; set; }
        public string? ManagerName { get; set; }
    }
}