using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PayFlowPro.Core.Interfaces;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.Entities;
using PayFlowPro.Shared.DTOs.Employee;

namespace PayFlowPro.Core.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService,
        ILogger<EmployeeService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _userManager = userManager;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Employee> CreateEmployeeAsync(CreateEmployeeDto employeeDto, string createdByUserId)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            // Generate employee code if not provided
            var employeeCode = await GenerateEmployeeCodeAsync();

            // Create user account if requested
            ApplicationUser? user = null;
            if (employeeDto.CreateUserAccount)
            {
                user = new ApplicationUser
                {
                    UserName = employeeDto.Email,
                    Email = employeeDto.Email,
                    FirstName = employeeDto.FirstName,
                    LastName = employeeDto.LastName,
                    EmailConfirmed = true,
                    IsActive = true
                };

                var password = !string.IsNullOrWhiteSpace(employeeDto.TemporaryPassword) 
                    ? employeeDto.TemporaryPassword 
                    : GenerateTemporaryPassword();

                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create user account: {errors}");
                }

                // Assign role
                var role = !string.IsNullOrWhiteSpace(employeeDto.UserRole) ? employeeDto.UserRole : "Employee";
                await _userManager.AddToRoleAsync(user, role);

                _logger.LogInformation("Created user account for employee: {Email} with role: {Role}", employeeDto.Email, role);
            }

            // Create employee record
            var employee = new Employee
            {
                EmployeeCode = employeeCode,
                FirstName = employeeDto.FirstName,
                LastName = employeeDto.LastName,
                Email = employeeDto.Email,
                PhoneNumber = employeeDto.PhoneNumber,
                DateOfBirth = employeeDto.DateOfBirth,
                DateOfJoining = employeeDto.DateOfJoining,
                Address = employeeDto.Address,
                NationalId = employeeDto.NationalId,
                TaxId = employeeDto.TaxId,
                BankAccountNumber = employeeDto.BankAccountNumber,
                BankName = employeeDto.BankName,
                Gender = employeeDto.Gender,
                MaritalStatus = employeeDto.MaritalStatus,
                Status = Models.Enums.EmploymentStatus.Active,
                JobTitle = employeeDto.JobTitle,
                BasicSalary = employeeDto.BasicSalary,
                CompanyId = employeeDto.CompanyId,
                DepartmentId = employeeDto.DepartmentId,
                UserId = user?.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Employees.Add(employee);
            await dbContext.SaveChangesAsync();

            // Log audit trail
            await _auditService.LogActivityAsync(
                "Create",
                "Employee", 
                employee.Id.ToString(),
                null,
                new
                {
                    employee.EmployeeCode,
                    employee.FirstName,
                    employee.LastName,
                    employee.Email,
                    employee.JobTitle,
                    employee.DepartmentId,
                    employee.BasicSalary,
                    UserAccountCreated = user != null
                },
                $"Created employee: {employee.FullName} ({employee.EmployeeCode})"
            );

            await transaction.CommitAsync();

            _logger.LogInformation("Successfully created employee: {EmployeeCode} - {FullName}", employee.EmployeeCode, employee.FullName);

            return employee;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create employee: {Email}", employeeDto.Email);
            
            await _auditService.LogActivityAsync(
                "Create",
                "Employee",
                "0",
                null,
                null,
                $"Failed to create employee: {employeeDto.FirstName} {employeeDto.LastName} - {ex.Message}"
            );
            
            throw;
        }
    }

    public async Task<Employee> UpdateEmployeeAsync(int employeeId, UpdateEmployeeDto employeeDto, string updatedByUserId)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        var employee = await dbContext.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            throw new ArgumentException("Employee not found", nameof(employeeId));
        }

        // Store old values for audit
        var oldValues = System.Text.Json.JsonSerializer.Serialize(new
        {
            employee.FirstName,
            employee.LastName,
            employee.Email,
            employee.JobTitle,
            employee.BasicSalary,
            employee.DepartmentId,
            employee.Status
        });

        // Update employee properties
        employee.FirstName = employeeDto.FirstName;
        employee.LastName = employeeDto.LastName;
        employee.Email = employeeDto.Email;
        employee.PhoneNumber = employeeDto.PhoneNumber;
        employee.DateOfBirth = employeeDto.DateOfBirth;
        employee.DateOfJoining = employeeDto.DateOfJoining;
        employee.DateOfLeaving = employeeDto.DateOfLeaving;
        employee.Address = employeeDto.Address;
        employee.NationalId = employeeDto.NationalId;
        employee.TaxId = employeeDto.TaxId;
        employee.BankAccountNumber = employeeDto.BankAccountNumber;
        employee.BankName = employeeDto.BankName;
        employee.Gender = employeeDto.Gender;
        employee.MaritalStatus = employeeDto.MaritalStatus;
        employee.Status = employeeDto.Status;
        employee.JobTitle = employeeDto.JobTitle;
        employee.BasicSalary = employeeDto.BasicSalary;
        employee.DepartmentId = employeeDto.DepartmentId;
        employee.CompanyId = employeeDto.CompanyId;
        employee.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        // Log audit trail
        var newValues = System.Text.Json.JsonSerializer.Serialize(new
        {
            employee.FirstName,
            employee.LastName,
            employee.Email,
            employee.JobTitle,
            employee.BasicSalary,
            employee.DepartmentId,
            employee.Status
        });

        await _auditService.LogActivityAsync(
            "Update",
            "Employee",
            employee.Id.ToString(),
            oldValues,
            newValues,
            $"Updated employee: {employee.FullName} ({employee.EmployeeCode})"
        );

        _logger.LogInformation("Successfully updated employee: {EmployeeCode} - {FullName}", employee.EmployeeCode, employee.FullName);

        return employee;
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int employeeId)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        return await dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Company)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == employeeId);
    }

    public async Task<Employee?> GetEmployeeByEmailAsync(string email)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        return await dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Company)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Email == email);
    }

    public async Task<Employee?> GetEmployeeByUserIdAsync(string userId)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        return await dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Company)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.UserId == userId);
    }

    public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        return await dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Company)
            .Include(e => e.User)
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync();
    }

    public async Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(int departmentId)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        return await dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Company)
            .Include(e => e.User)
            .Where(e => e.DepartmentId == departmentId)
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync();
    }

    public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync()
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        return await dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Company)
            .Include(e => e.User)
            .Where(e => e.Status == Models.Enums.EmploymentStatus.Active)
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync();
    }

    public async Task<bool> DeleteEmployeeAsync(int employeeId, string deletedByUserId)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        var employee = await dbContext.Employees.FindAsync(employeeId);
        if (employee == null)
        {
            return false;
        }

        var oldValues = System.Text.Json.JsonSerializer.Serialize(new
        {
            employee.EmployeeCode,
            employee.FirstName,
            employee.LastName,
            employee.Email,
            employee.Status
        });

        dbContext.Employees.Remove(employee);
        await dbContext.SaveChangesAsync();

        // Log audit trail
        await _auditService.LogActivityAsync(
            "Delete",
            "Employee",
            employee.Id.ToString(),
            oldValues,
            null,
            $"Deleted employee: {employee.FullName} ({employee.EmployeeCode})"
        );

        _logger.LogInformation("Successfully deleted employee: {EmployeeCode} - {FullName}", employee.EmployeeCode, employee.FullName);

        return true;
    }

    public async Task<bool> IsEmployeeCodeAvailableAsync(string employeeCode)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        return !await dbContext.Employees.AnyAsync(e => e.EmployeeCode == employeeCode);
    }

    public async Task<bool> IsEmailAvailableAsync(string email)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        return !await dbContext.Employees.AnyAsync(e => e.Email == email);
    }

    public async Task<string> GenerateEmployeeCodeAsync()
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        // Get the highest employee code number
        var lastEmployee = await dbContext.Employees
            .Where(e => e.EmployeeCode.StartsWith("EMP"))
            .OrderByDescending(e => e.EmployeeCode)
            .FirstOrDefaultAsync();

        if (lastEmployee == null)
        {
            return "EMP001";
        }

        // Extract number from employee code (EMP001 -> 001)
        var codeNumber = lastEmployee.EmployeeCode.Substring(3);
        if (int.TryParse(codeNumber, out int number))
        {
            return $"EMP{(number + 1):D3}";
        }

        // Fallback if parsing fails
        var count = await dbContext.Employees.CountAsync();
        return $"EMP{(count + 1):D3}";
    }

    private static string GenerateTemporaryPassword()
    {
        // Generate a secure temporary password
        var random = new Random();
        var chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789";
        var result = new char[12];
        
        for (int i = 0; i < 12; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }
        
        return new string(result) + "!";
    }
}