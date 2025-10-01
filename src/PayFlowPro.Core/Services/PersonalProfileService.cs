using Microsoft.EntityFrameworkCore;
using PayFlowPro.Data.Context;
using PayFlowPro.Shared.DTOs.Employee;
using PayFlowPro.Models.Entities;
using PayFlowPro.Models.Enums;
using PayFlowPro.Shared.DTOs;

namespace PayFlowPro.Core.Services;

/// <summary>
/// Service for managing employee personal profiles
/// </summary>
public interface IPersonalProfileService
{
    Task<PersonalProfileDto?> GetPersonalProfileAsync(string userId);
    Task<ProfileUpdateResponseDto> UpdatePersonalProfileAsync(string userId, UpdatePersonalProfileDto updateDto);
}

/// <summary>
/// Implementation of personal profile management service
/// </summary>
public class PersonalProfileService : IPersonalProfileService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public PersonalProfileService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<PersonalProfileDto?> GetPersonalProfileAsync(string userId)
    {
        using var context = _contextFactory.CreateDbContext();

        var employee = await context.Employees
            .Include(e => e.Department)
            .Include(e => e.Company)
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (employee == null)
            return null;

        return new PersonalProfileDto
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            Address = employee.Address,
            DateOfBirth = employee.DateOfBirth,
            Gender = employee.Gender.ToString(),
            MaritalStatus = employee.MaritalStatus.ToString(),
            NationalId = employee.NationalId,
            TaxId = employee.TaxId,
            JobTitle = employee.JobTitle ?? "Not Specified",
            DepartmentName = employee.Department?.Name ?? "No Department",
            CompanyName = employee.Company?.Name ?? "No Company",
            DateOfJoining = employee.DateOfJoining,
            ProfileImageUrl = employee.ProfileImageUrl,
            BasicSalary = employee.BasicSalary,
            BankName = employee.BankName,
            BankAccountNumber = employee.BankAccountNumber,
            EmergencyContacts = new List<EmergencyContactDto>() // Will implement later
        };
    }

    public async Task<ProfileUpdateResponseDto> UpdatePersonalProfileAsync(string userId, UpdatePersonalProfileDto updateDto)
    {
        using var context = _contextFactory.CreateDbContext();

        var employee = await context.Employees
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (employee == null)
        {
            return new ProfileUpdateResponseDto
            {
                Success = false,
                Message = "Employee profile not found."
            };
        }

        var updatedFields = new List<string>();

        try
        {
            // Update allowed fields (fields that employees can modify directly)
            if (!string.IsNullOrEmpty(updateDto.PhoneNumber) && updateDto.PhoneNumber != employee.PhoneNumber)
            {
                employee.PhoneNumber = updateDto.PhoneNumber;
                updatedFields.Add("Phone Number");
            }

            if (!string.IsNullOrEmpty(updateDto.Address) && updateDto.Address != employee.Address)
            {
                employee.Address = updateDto.Address;
                updatedFields.Add("Address");
            }

            if (!string.IsNullOrEmpty(updateDto.Gender) && Enum.TryParse<Gender>(updateDto.Gender, out var gender) && gender != employee.Gender)
            {
                employee.Gender = gender;
                updatedFields.Add("Gender");
            }

            if (!string.IsNullOrEmpty(updateDto.MaritalStatus) && Enum.TryParse<MaritalStatus>(updateDto.MaritalStatus, out var maritalStatus) && maritalStatus != employee.MaritalStatus)
            {
                employee.MaritalStatus = maritalStatus;
                updatedFields.Add("Marital Status");
            }

            if (!string.IsNullOrEmpty(updateDto.BankName) && updateDto.BankName != employee.BankName)
            {
                employee.BankName = updateDto.BankName;
                updatedFields.Add("Bank Name");
            }

            if (!string.IsNullOrEmpty(updateDto.BankAccountNumber) && updateDto.BankAccountNumber != employee.BankAccountNumber)
            {
                employee.BankAccountNumber = updateDto.BankAccountNumber;
                updatedFields.Add("Bank Account Number");
            }

            employee.UpdatedAt = DateTime.UtcNow;

            if (updatedFields.Any())
            {
                await context.SaveChangesAsync();
            }

            return new ProfileUpdateResponseDto
            {
                Success = true,
                Message = updatedFields.Any() 
                    ? $"Successfully updated: {string.Join(", ", updatedFields)}"
                    : "No changes were made.",
                UpdatedFields = updatedFields,
                PendingApprovalFields = new List<string>()
            };
        }
        catch (Exception ex)
        {
            return new ProfileUpdateResponseDto
            {
                Success = false,
                Message = $"Failed to update profile: {ex.Message}"
            };
        }
    }
}