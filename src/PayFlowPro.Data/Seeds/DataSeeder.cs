using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.Entities;
using PayFlowPro.Models.Enums;

namespace PayFlowPro.Data.Seeds;

public static class DataSeeder
{
    public static async Task SeedDataAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        Console.WriteLine("DataSeeder: Starting seed process...");
        try
        {
            // Seed Roles
            Console.WriteLine("DataSeeder: Seeding roles...");
            await SeedRolesAsync(roleManager);

            // Seed Company and Departments (in one transaction)
            await SeedCompanyAsync(context);

            // Seed Allowance and Deduction Types
            await SeedAllowanceTypesAsync(context);
            await context.SaveChangesAsync();

            await SeedDeductionTypesAsync(context);
            await context.SaveChangesAsync();

            // Seed Leave Types
            await SeedLeaveTypesAsync(context);
            await context.SaveChangesAsync();

            // Seed Users and Employees (after departments are committed)
            Console.WriteLine("DataSeeder: Starting user and employee seeding...");
            await SeedUsersAndEmployeesAsync(context, userManager);
            Console.WriteLine("DataSeeder: Completed user and employee seeding...");

            await context.SaveChangesAsync();

            // Seed Leave Balances (after employees are created)
            await SeedLeaveBalancesAsync(context);
            await context.SaveChangesAsync();

            // Seed System Settings (currency configuration)
            await SeedSystemSettingsAsync(context);
            await context.SaveChangesAsync();

            // Fix any existing AccruedDays data issues (one-time fix)
            await FixAccruedDaysAsync(context);
        }
        catch (Exception ex)
        {
            // Log the error (in production, use proper logging)
            Console.WriteLine($"Error seeding data: {ex.Message}");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Admin", "HR", "Manager", "Employee" };

        foreach (string role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedCompanyAsync(ApplicationDbContext context)
    {
        Company company;

        if (!context.Companies.Any())
        {
            company = new Company
            {
                Name = "SITA Information Networking Computing UK",
                Address = "Level 5, Block A-C, Apex, Forbury Road, Reading, England RG1 1AX",
                PhoneNumber = "+44 8000260142",
                Email = "support@sita.aero",
                RegistrationNumber = "03995063",
                IsActive = true
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync();
        }
        else
        {
            // Update existing company with SITA information
            company = await context.Companies.FirstAsync();
            company.Name = "SITA Information Networking Computing UK";
            company.Address = "Level 5, Block A-C, Apex, Forbury Road, Reading, England RG1 1AX";
            company.PhoneNumber = "+44 8000260142";
            company.Email = "support@sita.aero";
            company.RegistrationNumber = "03995063";
            company.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }

        // Always check and seed departments
        if (!context.Departments.Any())
        {
            var departments = new[]
            {
                new Department { Name = "Human Resources", Code = "HR", CompanyId = company.Id, Description = "Human Resources Department" },
                new Department { Name = "Information Technology", Code = "IT", CompanyId = company.Id, Description = "IT Department" },
                new Department { Name = "Finance", Code = "FIN", CompanyId = company.Id, Description = "Finance Department" },
                new Department { Name = "Marketing", Code = "MKT", CompanyId = company.Id, Description = "Marketing Department" }
            };

            context.Departments.AddRange(departments);
            await context.SaveChangesAsync();
        }
    }

    private static Task SeedAllowanceTypesAsync(ApplicationDbContext context)
    {
        if (!context.AllowanceTypes.Any())
        {
            var allowanceTypes = new[]
            {
                new Models.Entities.AllowanceType { Name = "House Rent Allowance", Code = "HRA", Type = Models.Enums.AllowanceType.Percentage, DefaultPercentage = 40, IsTaxable = true },
                new Models.Entities.AllowanceType { Name = "Medical Allowance", Code = "MED", Type = Models.Enums.AllowanceType.Fixed, DefaultAmount = 500, IsTaxable = false },
                new Models.Entities.AllowanceType { Name = "Transport Allowance", Code = "TRANS", Type = Models.Enums.AllowanceType.Fixed, DefaultAmount = 300, IsTaxable = true },
                new Models.Entities.AllowanceType { Name = "Performance Bonus", Code = "PERF", Type = Models.Enums.AllowanceType.Fixed, DefaultAmount = 0, IsTaxable = true },
                new Models.Entities.AllowanceType { Name = "Overtime Allowance", Code = "OT", Type = Models.Enums.AllowanceType.Fixed, DefaultAmount = 0, IsTaxable = true }
            };

            context.AllowanceTypes.AddRange(allowanceTypes);
        }

        return Task.CompletedTask;
    }

    private static Task SeedDeductionTypesAsync(ApplicationDbContext context)
    {
        if (!context.DeductionTypes.Any())
        {
            var deductionTypes = new[]
            {
                new Models.Entities.DeductionType { Name = "Income Tax", Code = "IT", Type = Models.Enums.DeductionType.Tax, DefaultPercentage = 10 },
                new Models.Entities.DeductionType { Name = "Provident Fund", Code = "PF", Type = Models.Enums.DeductionType.Percentage, DefaultPercentage = 12 },
                new Models.Entities.DeductionType { Name = "Health Insurance", Code = "HI", Type = Models.Enums.DeductionType.Insurance, DefaultAmount = 200 },
                new Models.Entities.DeductionType { Name = "Loan Deduction", Code = "LOAN", Type = Models.Enums.DeductionType.Fixed, DefaultAmount = 0 },
                new Models.Entities.DeductionType { Name = "Late Coming Fine", Code = "LATE", Type = Models.Enums.DeductionType.Fixed, DefaultAmount = 0 }
            };

            context.DeductionTypes.AddRange(deductionTypes);
        }

        return Task.CompletedTask;
    }

    private static async Task SeedUsersAndEmployeesAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        if (!context.Employees.Any())
        {
            var company = await context.Companies.FirstOrDefaultAsync();
            if (company == null)
            {
                throw new InvalidOperationException("Company not found. Ensure company seeding completed.");
            }

            var hrDept = await context.Departments.FirstOrDefaultAsync(d => d.Code == "HR");
            var itDept = await context.Departments.FirstOrDefaultAsync(d => d.Code == "IT");

            if (hrDept == null || itDept == null)
            {
                // Try to find any departments for debugging
                var allDepts = await context.Departments.ToListAsync();
                var deptCodes = string.Join(", ", allDepts.Select(d => d.Code));
                throw new InvalidOperationException($"Required departments not found. Available departments: [{deptCodes}]. HR={hrDept != null}, IT={itDept != null}");
            }

            // Create Admin User
            var adminUser = new ApplicationUser
            {
                UserName = "admin@democompany.com",
                Email = "admin@democompany.com",
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                IsActive = true
            };

            if (await userManager.FindByEmailAsync(adminUser.Email) == null)
            {
                var createResult = await userManager.CreateAsync(adminUser, "Admin@123");
                if (!createResult.Succeeded)
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }

                var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                if (!roleResult.Succeeded)
                {
                    throw new Exception($"Failed to add Admin role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }

                var adminEmployee = new Employee
                {
                    EmployeeCode = "EMP001",
                    FirstName = "System",
                    LastName = "Administrator",
                    Email = adminUser.Email,
                    DateOfBirth = new DateTime(1985, 1, 1),
                    DateOfJoining = DateTime.Today.AddYears(-2),
                    Gender = Gender.Male,
                    MaritalStatus = MaritalStatus.Single,
                    Status = EmploymentStatus.Active,
                    JobTitle = "System Administrator",
                    BasicSalary = 80000,
                    CompanyId = company.Id,
                    DepartmentId = itDept.Id,
                    UserId = adminUser.Id
                };

                context.Employees.Add(adminEmployee);
            }

            // Create HR Manager
            var hrUser = new ApplicationUser
            {
                UserName = "hr@democompany.com",
                Email = "hr@democompany.com",
                FirstName = "Sarah",
                LastName = "Johnson",
                EmailConfirmed = true,
                IsActive = true
            };

            if (await userManager.FindByEmailAsync(hrUser.Email) == null)
            {
                var createResult = await userManager.CreateAsync(hrUser, "Hr@123");
                if (!createResult.Succeeded)
                {
                    throw new Exception($"Failed to create HR user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }

                var roleResult = await userManager.AddToRoleAsync(hrUser, "HR");
                if (!roleResult.Succeeded)
                {
                    throw new Exception($"Failed to add HR role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }

                var hrEmployee = new Employee
                {
                    EmployeeCode = "EMP002",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    Email = hrUser.Email,
                    DateOfBirth = new DateTime(1988, 5, 15),
                    DateOfJoining = DateTime.Today.AddYears(-1),
                    Gender = Gender.Female,
                    MaritalStatus = MaritalStatus.Married,
                    Status = EmploymentStatus.Active,
                    JobTitle = "HR Manager",
                    BasicSalary = 60000,
                    CompanyId = company.Id,
                    DepartmentId = hrDept.Id,
                    UserId = hrUser.Id
                };

                context.Employees.Add(hrEmployee);
            }

            // Create Sample Employee
            var empUser = new ApplicationUser
            {
                UserName = "john.doe@democompany.com",
                Email = "john.doe@democompany.com",
                FirstName = "John",
                LastName = "Doe",
                EmailConfirmed = true,
                IsActive = true
            };

            var existingEmpUser = await userManager.FindByEmailAsync(empUser.Email);
            Console.WriteLine($"DataSeeder: Checking employee user {empUser.Email} - Exists: {existingEmpUser != null}");

            if (existingEmpUser == null)
            {
                Console.WriteLine($"DataSeeder: Creating employee user {empUser.Email}");
                var createResult = await userManager.CreateAsync(empUser, "Employee@123");
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    Console.WriteLine($"DataSeeder: Failed to create employee user: {errors}");
                    throw new Exception($"Failed to create employee user: {errors}");
                }
                Console.WriteLine($"DataSeeder: Successfully created employee user {empUser.Email}");

                var roleResult = await userManager.AddToRoleAsync(empUser, "Employee");
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    Console.WriteLine($"DataSeeder: Failed to add Employee role: {errors}");
                    throw new Exception($"Failed to add Employee role: {errors}");
                }
                Console.WriteLine($"DataSeeder: Successfully added Employee role to {empUser.Email}");
            }
            else
            {
                Console.WriteLine($"DataSeeder: Employee user {empUser.Email} already exists, skipping creation");
            }

            // Create Employee record (regardless of whether user existed)
            var existingEmployee = await context.Employees.FirstOrDefaultAsync(e => e.Email == empUser.Email);
            if (existingEmployee == null)
            {
                var employee = new Employee
                {
                    EmployeeCode = "EMP003",
                    FirstName = "John",
                    LastName = "Doe",
                    Email = empUser.Email,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    DateOfJoining = DateTime.Today.AddMonths(-6),
                    Gender = Gender.Male,
                    MaritalStatus = MaritalStatus.Single,
                    Status = EmploymentStatus.Active,
                    JobTitle = "Software Developer",
                    BasicSalary = 45000,
                    CompanyId = company.Id,
                    DepartmentId = itDept.Id,
                    UserId = empUser.Id
                };

                context.Employees.Add(employee);
            }

            // Save employees first so they get IDs assigned
            await context.SaveChangesAsync();

            // Now update department managers with the employee IDs
            var savedHrEmployee = await context.Employees
                .FirstOrDefaultAsync(e => e.Email == "hr@democompany.com");

            if (savedHrEmployee != null)
            {
                var hrDepartment = await context.Departments.FirstOrDefaultAsync(d => d.Code == "HR");
                if (hrDepartment != null)
                {
                    hrDepartment.ManagerEmployeeId = savedHrEmployee.Id;
                    await context.SaveChangesAsync();
                }
            }

            // Add sample allowances and deductions for employees
            await SeedEmployeeAllowancesAndDeductionsAsync(context);
        }
    }

    private static async Task SeedEmployeeAllowancesAndDeductionsAsync(ApplicationDbContext context)
    {
        Console.WriteLine("DataSeeder: Starting employee allowances and deductions seeding...");

        if (!context.EmployeeAllowances.Any())
        {
            var employees = await context.Employees.ToListAsync();
            var allowanceTypes = await context.AllowanceTypes.ToListAsync();
            var deductionTypes = await context.DeductionTypes.ToListAsync();

            foreach (var employee in employees)
            {
                // Add standard allowances for all employees
                var hraType = allowanceTypes.FirstOrDefault(a => a.Code == "HRA");
                if (hraType != null)
                {
                    context.EmployeeAllowances.Add(new EmployeeAllowance
                    {
                        EmployeeId = employee.Id,
                        AllowanceTypeId = hraType.Id,
                        Percentage = 40, // 40% of basic salary
                        EffectiveFrom = employee.DateOfJoining,
                        IsActive = true
                    });
                }

                var medicalType = allowanceTypes.FirstOrDefault(a => a.Code == "MED");
                if (medicalType != null)
                {
                    context.EmployeeAllowances.Add(new EmployeeAllowance
                    {
                        EmployeeId = employee.Id,
                        AllowanceTypeId = medicalType.Id,
                        Amount = 1500, // Fixed medical allowance
                        EffectiveFrom = employee.DateOfJoining,
                        IsActive = true
                    });
                }

                var transportType = allowanceTypes.FirstOrDefault(a => a.Code == "TRANS");
                if (transportType != null)
                {
                    context.EmployeeAllowances.Add(new EmployeeAllowance
                    {
                        EmployeeId = employee.Id,
                        AllowanceTypeId = transportType.Id,
                        Amount = 2000, // Fixed transport allowance
                        EffectiveFrom = employee.DateOfJoining,
                        IsActive = true
                    });
                }

                // Add standard deductions
                var pfType = deductionTypes.FirstOrDefault(d => d.Code == "PF");
                if (pfType != null)
                {
                    context.EmployeeDeductions.Add(new EmployeeDeduction
                    {
                        EmployeeId = employee.Id,
                        DeductionTypeId = pfType.Id,
                        Percentage = 12, // 12% of basic salary
                        EffectiveFrom = employee.DateOfJoining,
                        IsActive = true
                    });
                }

                var insuranceType = deductionTypes.FirstOrDefault(d => d.Code == "HI");
                if (insuranceType != null)
                {
                    context.EmployeeDeductions.Add(new EmployeeDeduction
                    {
                        EmployeeId = employee.Id,
                        DeductionTypeId = insuranceType.Id,
                        Amount = 500, // Fixed health insurance
                        EffectiveFrom = employee.DateOfJoining,
                        IsActive = true
                    });
                }
            }

            await context.SaveChangesAsync();
            Console.WriteLine("DataSeeder: Successfully seeded employee allowances and deductions.");
        }
    }

    private static async Task SeedLeaveTypesAsync(ApplicationDbContext context)
        {
            if (!context.LeaveTypes.Any())
            {
                Console.WriteLine("DataSeeder: Seeding leave types...");
                var leaveTypes = new[]
                {
                    new LeaveType
                    {
                        Name = "Annual Leave",
                        Code = "AL",
                        Description = "Annual vacation leave",
                        AnnualAllocation = 20,
                        CanCarryOver = true,
                        MaxCarryOverDays = 5,
                        RequiresApproval = true,
                        MinimumNoticeRequiredDays = 7,
                        MaxConsecutiveDays = 10,
                        IsPaid = true,
                        IsActive = true,
                        AccrualFrequency = LeaveAccrualFrequency.Annually,
                        ColorCode = "#007bff"
                    },
                    new LeaveType
                    {
                        Name = "Sick Leave",
                        Code = "SL",
                        Description = "Medical sick leave",
                        AnnualAllocation = 12,
                        CanCarryOver = false,
                        MaxCarryOverDays = 0,
                        RequiresApproval = false,
                        MinimumNoticeRequiredDays = 0,
                        MaxConsecutiveDays = 30,
                        IsPaid = true,
                        IsActive = true,
                        AccrualFrequency = LeaveAccrualFrequency.Annually,
                        ColorCode = "#dc3545"
                    },
                    new LeaveType
                    {
                        Name = "Emergency Leave",
                        Code = "EL",
                        Description = "Emergency and urgent personal matters",
                        AnnualAllocation = 5,
                        CanCarryOver = false,
                        MaxCarryOverDays = 0,
                        RequiresApproval = true,
                        MinimumNoticeRequiredDays = 0,
                        MaxConsecutiveDays = 3,
                        IsPaid = true,
                        IsActive = true,
                        AccrualFrequency = LeaveAccrualFrequency.Annually,
                        ColorCode = "#fd7e14"
                    },
                    new LeaveType
                    {
                        Name = "Maternity Leave",
                        Code = "ML",
                        Description = "Maternity leave for female employees",
                        AnnualAllocation = 90,
                        CanCarryOver = false,
                        MaxCarryOverDays = 0,
                        RequiresApproval = true,
                        MinimumNoticeRequiredDays = 30,
                        MaxConsecutiveDays = 90,
                        IsPaid = true,
                        IsActive = true,
                        AccrualFrequency = LeaveAccrualFrequency.Annually,
                        GenderRestriction = LeaveGenderRestriction.Female,
                        ColorCode = "#e83e8c"
                    },
                    new LeaveType
                    {
                        Name = "Paternity Leave",
                        Code = "PL",
                        Description = "Paternity leave for male employees",
                        AnnualAllocation = 14,
                        CanCarryOver = false,
                        MaxCarryOverDays = 0,
                        RequiresApproval = true,
                        MinimumNoticeRequiredDays = 14,
                        MaxConsecutiveDays = 14,
                        IsPaid = true,
                        IsActive = true,
                        AccrualFrequency = LeaveAccrualFrequency.Annually,
                        GenderRestriction = LeaveGenderRestriction.Male,
                        ColorCode = "#6f42c1"
                    },
                    new LeaveType
                    {
                        Name = "Compensatory Leave",
                        Code = "CL",
                        Description = "Compensation for overtime work",
                        AnnualAllocation = 0,
                        CanCarryOver = true,
                        MaxCarryOverDays = 10,
                        RequiresApproval = true,
                        MinimumNoticeRequiredDays = 3,
                        MaxConsecutiveDays = 5,
                        IsPaid = false,
                        IsActive = true,
                        AccrualFrequency = LeaveAccrualFrequency.Monthly,
                        ColorCode = "#20c997"
                    }
                };

                context.LeaveTypes.AddRange(leaveTypes);
                await context.SaveChangesAsync();
                Console.WriteLine("DataSeeder: Successfully seeded leave types.");
            }
        }

        private static async Task SeedLeaveBalancesAsync(ApplicationDbContext context)
        {
            if (!context.LeaveBalances.Any())
            {
                Console.WriteLine("DataSeeder: Seeding leave balances...");
                var employees = await context.Employees.ToListAsync();
                var leaveTypes = await context.LeaveTypes.ToListAsync();
                var currentYear = DateTime.Now.Year;

                var leaveBalances = new List<LeaveBalance>();

                foreach (var employee in employees)
                {
                    foreach (var leaveType in leaveTypes)
                    {
                        // Skip gender-restricted leave types if not applicable
                        if (leaveType.GenderRestriction.HasValue)
                        {
                            if (leaveType.GenderRestriction == LeaveGenderRestriction.Male && employee.Gender != Gender.Male)
                                continue;
                            if (leaveType.GenderRestriction == LeaveGenderRestriction.Female && employee.Gender != Gender.Female)
                                continue;
                        }

                        var leaveBalance = new LeaveBalance
                        {
                            EmployeeId = employee.Id,
                            LeaveTypeId = leaveType.Id,
                            FinancialYear = currentYear,
                            AllocatedDays = leaveType.AnnualAllocation,
                            UsedDays = 0,
                            CarriedOverDays = 0,
                            ExpiringDays = 0,
                            AccruedDays = leaveType.AnnualAllocation, // Full allocation for demo
                            PendingDays = 0
                        };

                        leaveBalances.Add(leaveBalance);
                    }
                }

                context.LeaveBalances.AddRange(leaveBalances);
                await context.SaveChangesAsync();
                Console.WriteLine("DataSeeder: Successfully seeded leave balances.");
            }
        }

        private static async Task FixAccruedDaysAsync(ApplicationDbContext context)
    {
        Console.WriteLine("DataSeeder: Checking for AccruedDays data issues...");

        // Find leave balances where AccruedDays equals AllocatedDays (the bug condition)
        // and they are new employees with no usage (UsedDays = 0, PendingDays = 0, CarriedOverDays = 0)
        var problematicBalances = await context.LeaveBalances
            .Where(lb => lb.AccruedDays == lb.AllocatedDays
                        && lb.UsedDays == 0
                        && lb.PendingDays == 0
                        && lb.CarriedOverDays == 0)
            .Include(lb => lb.Employee)
            .Include(lb => lb.LeaveType)
            .ToListAsync();

        if (problematicBalances.Any())
        {
            Console.WriteLine($"DataSeeder: Found {problematicBalances.Count} leave balances to fix");

            foreach (var balance in problematicBalances)
            {
                var oldAccruedDays = balance.AccruedDays;
                balance.AccruedDays = 0; // Reset to 0 as it should be for new employees

                Console.WriteLine($"DataSeeder: Fixed {balance.Employee.FirstName} {balance.Employee.LastName} - {balance.LeaveType.Name}: AccruedDays {oldAccruedDays} → 0");
            }

            var updatedCount = await context.SaveChangesAsync();
            Console.WriteLine($"DataSeeder: Successfully fixed {updatedCount} leave balance records");
        }
        else
        {
            Console.WriteLine("DataSeeder: No AccruedDays issues found - data is correct");
        }
    }

    private static Task SeedSystemSettingsAsync(ApplicationDbContext context)
    {
        if (!context.SystemSettings.Any(s => s.Category == "Localization"))
        {
            var currencySettings = new[]
            {
                new SystemSetting
                {
                    Key = "Currency.Code",
                    Value = "GBP",
                    Category = "Localization",
                    Description = "ISO 4217 currency code (e.g., GBP, USD, EUR)",
                    DataType = "String"
                },
                new SystemSetting
                {
                    Key = "Currency.Culture",
                    Value = "en-GB",
                    Category = "Localization",
                    Description = "Culture name for currency formatting (e.g., en-GB, en-US)",
                    DataType = "String"
                },
                new SystemSetting
                {
                    Key = "Currency.Name",
                    Value = "British Pound Sterling",
                    Category = "Localization",
                    Description = "Full currency name for display purposes",
                    DataType = "String"
                }
            };

            context.SystemSettings.AddRange(currencySettings);
            Console.WriteLine("DataSeeder: Added currency configuration settings (GBP - British Pound Sterling)");
        }

        return Task.CompletedTask;
    }
}