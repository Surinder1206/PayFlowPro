using PayFlowPro.Models.Entities;
using PayFlowPro.Shared.DTOs.Employee;

namespace PayFlowPro.Core.Interfaces;

public interface IEmployeeService
{
    Task<Employee> CreateEmployeeAsync(CreateEmployeeDto employeeDto, string createdByUserId);
    Task<Employee> UpdateEmployeeAsync(int employeeId, UpdateEmployeeDto employeeDto, string updatedByUserId);
    Task<Employee?> GetEmployeeByIdAsync(int employeeId);
    Task<Employee?> GetEmployeeByEmailAsync(string email);
    Task<Employee?> GetEmployeeByUserIdAsync(string userId);
    Task<IEnumerable<Employee>> GetAllEmployeesAsync();
    Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(int departmentId);
    Task<IEnumerable<Employee>> GetActiveEmployeesAsync();
    Task<bool> DeleteEmployeeAsync(int employeeId, string deletedByUserId);
    Task<bool> IsEmployeeCodeAvailableAsync(string employeeCode);
    Task<bool> IsEmailAvailableAsync(string email);
    Task<string> GenerateEmployeeCodeAsync();
}