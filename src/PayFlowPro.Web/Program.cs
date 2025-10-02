using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.Entities;
using PayFlowPro.Data.Seeds;
using PayFlowPro.Core.Interfaces;
using PayFlowPro.Core.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Use DbContextFactory for Blazor Server to avoid concurrency issues
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Also register a scoped DbContext for Identity and other services that require it
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")), ServiceLifetime.Scoped);

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddAntiforgery();

// Add Blazor authentication state provider
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider,
    Microsoft.AspNetCore.Components.Server.ServerAuthenticationStateProvider>();

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("HROnly", policy => policy.RequireRole("Admin", "HR"));
    options.AddPolicy("ManagerOnly", policy => policy.RequireRole("Admin", "HR", "Manager"));
});

builder.Services.AddHttpContextAccessor();

// Add custom services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IPayslipService, PayslipService>();
builder.Services.AddScoped<IPayslipCalculationService, PayslipCalculationService>();
builder.Services.AddScoped<IPersonalProfileService, PersonalProfileService>();
builder.Services.AddScoped<ISalaryHistoryService, SalaryHistoryService>();
builder.Services.AddScoped<ILeaveManagementService, LeaveManagementService>();
builder.Services.AddScoped<IAutoApprovalService, AutoApprovalService>();
builder.Services.AddScoped<IEmployeeIdentityService, EmployeeIdentityService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<PayFlowPro.Web.Services.DataFixService>();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await context.Database.EnsureCreatedAsync();
    await DataSeeder.SeedDataAsync(context, userManager, roleManager);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
