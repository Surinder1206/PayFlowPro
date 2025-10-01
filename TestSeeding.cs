using Microsoft.EntityFrameworkCore;
using PayFlowPro.Data.Context;

// Test database seeding
var connectionString = "Server=(localdb)\\mssqllocaldb;Database=PayFlowProDb_Test;Trusted_Connection=true;MultipleActiveResultSets=true";

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(connectionString)
    .Options;

using var context = new ApplicationDbContext(options);

// Ensure database is created
await context.Database.EnsureCreatedAsync();

// Test if we can seed companies and departments
Console.WriteLine("Testing company and department seeding...");

if (!context.Companies.Any())
{
    var company = new PayFlowPro.Models.Entities.Company
    {
        Name = "Test Company",
        IsActive = true
    };
    
    context.Companies.Add(company);
    await context.SaveChangesAsync();
    
    Console.WriteLine($"Company created with ID: {company.Id}");
    
    var department = new PayFlowPro.Models.Entities.Department
    {
        Name = "Test Department",
        Code = "TEST", 
        CompanyId = company.Id,
        IsActive = true
    };
    
    context.Departments.Add(department);
    await context.SaveChangesAsync();
    
    Console.WriteLine($"Department created with ID: {department.Id}");
}

// Test querying the department
var testDept = await context.Departments.FirstOrDefaultAsync(d => d.Code == "TEST");
if (testDept != null)
{
    Console.WriteLine($"Successfully found department: {testDept.Name}");
}
else
{
    Console.WriteLine("Could not find test department!");
}

Console.WriteLine("Test completed successfully!");