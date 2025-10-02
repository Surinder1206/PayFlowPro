using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PayFlowPro.Models.Entities;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace PayFlowPro.Web.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly ApplicationDbContext _context;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public class InputModel
        {
            [Required]
            [Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    EmailConfirmed = true, // For demo purposes - in production you'd send confirmation email
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Assign default Employee role to new registrants
                    await _userManager.AddToRoleAsync(user, "Employee");

                    // Create corresponding Employee record
                    await CreateEmployeeRecordAsync(user);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private async Task CreateEmployeeRecordAsync(ApplicationUser user)
        {
            try
            {
                // Get the default company (first company in the system)
                var defaultCompany = await _context.Companies.FirstAsync();

                // Get or create a default department (look for HR or General department)
                var defaultDepartment = await _context.Departments
                    .Where(d => d.Code == "HR" || d.Code == "GENERAL" || d.Name.Contains("General"))
                    .FirstOrDefaultAsync();

                if (defaultDepartment == null)
                {
                    // Create a General department if none exists
                    defaultDepartment = new Department
                    {
                        Name = "General",
                        Code = "GENERAL",
                        Description = "General department for new employees",
                        CompanyId = defaultCompany.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Departments.Add(defaultDepartment);
                    await _context.SaveChangesAsync();
                }

                // Create Employee record
                var employee = new Employee
                {
                    FirstName = user.FirstName ?? "Unknown",
                    LastName = user.LastName ?? "User",
                    Email = user.Email ?? "",
                    EmployeeCode = GenerateEmployeeCode(),
                    CompanyId = defaultCompany.Id,
                    DepartmentId = defaultDepartment.Id,
                    UserId = user.Id,
                    DateOfJoining = DateTime.UtcNow,
                    Status = EmploymentStatus.Active,
                    JobTitle = "Employee", // Default job title
                    BasicSalary = 0, // Will be set by HR later
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // Create default leave balances for the new employee
                await CreateDefaultLeaveBalancesAsync(employee.Id);

                _logger.LogInformation($"Employee record created for user {user.Email} with Employee ID: {employee.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create employee record for user {user.Email}");
                // Don't throw - let the user login, HR can create employee record manually
            }
        }

        private async Task CreateDefaultLeaveBalancesAsync(int employeeId)
        {
            try
            {
                var currentYear = DateTime.UtcNow.Year;
                var leaveTypes = await _context.LeaveTypes.Where(lt => lt.IsActive).ToListAsync();

                foreach (var leaveType in leaveTypes)
                {
                    var leaveBalance = new LeaveBalance
                    {
                        EmployeeId = employeeId,
                        LeaveTypeId = leaveType.Id,
                        FinancialYear = currentYear,
                        AllocatedDays = leaveType.AnnualAllocation,
                        UsedDays = 0,
                        AccruedDays = 0,
                        CarriedOverDays = 0,
                        ExpiringDays = 0,
                        PendingDays = 0,
                        ExpiryDate = new DateTime(currentYear + 1, 3, 31), // End of financial year
                        LastAccrualProcessed = DateTime.UtcNow,
                        Notes = "Initial allocation for new employee",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.LeaveBalances.Add(leaveBalance);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Default leave balances created for employee {employeeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create leave balances for employee {employeeId}");
            }
        }

        private string GenerateEmployeeCode()
        {
            // Generate a simple employee code: EMP + timestamp
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            return $"EMP{timestamp.Substring(timestamp.Length - 6)}"; // Last 6 digits for uniqueness
        }
    }
}