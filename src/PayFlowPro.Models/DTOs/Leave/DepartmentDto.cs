namespace PayFlowPro.Models.DTOs.Leave
{
    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int EmployeeCount { get; set; }
        public bool IsActive { get; set; } = true;
    }
}