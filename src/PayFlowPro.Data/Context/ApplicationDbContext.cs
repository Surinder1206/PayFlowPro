using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PayFlowPro.Models.Entities;

namespace PayFlowPro.Data.Context;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Company> Companies { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Payslip> Payslips { get; set; }
    public DbSet<AllowanceType> AllowanceTypes { get; set; }
    public DbSet<DeductionType> DeductionTypes { get; set; }
    public DbSet<EmployeeAllowance> EmployeeAllowances { get; set; }
    public DbSet<EmployeeDeduction> EmployeeDeductions { get; set; }
    public DbSet<PayslipAllowance> PayslipAllowances { get; set; }
    public DbSet<PayslipDeduction> PayslipDeductions { get; set; }
    
    // Audit entities
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SecurityEvent> SecurityEvents { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<DataChangeLog> DataChangeLogs { get; set; }
    
    // Employee profile entities
    public DbSet<EmergencyContact> EmergencyContacts { get; set; }
    public DbSet<ProfileChangeRequest> ProfileChangeRequests { get; set; }
    public DbSet<SalaryHistory> SalaryHistories { get; set; }
    
    // Leave management entities
    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<LeaveBalance> LeaveBalances { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<LeaveRequestApproval> LeaveRequestApprovals { get; set; }
    public DbSet<AutoApprovalRule> AutoApprovalRules { get; set; }
    public DbSet<AutoApprovalRuleLog> AutoApprovalRuleLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure precision for decimal properties
        modelBuilder.Entity<Employee>()
            .Property(e => e.BasicSalary)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payslip>()
            .Property(p => p.BasicSalary)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payslip>()
            .Property(p => p.GrossSalary)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payslip>()
            .Property(p => p.TotalAllowances)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payslip>()
            .Property(p => p.TotalDeductions)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payslip>()
            .Property(p => p.NetSalary)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payslip>()
            .Property(p => p.TaxAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<AllowanceType>()
            .Property(a => a.DefaultAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<AllowanceType>()
            .Property(a => a.DefaultPercentage)
            .HasPrecision(5, 2);

        modelBuilder.Entity<DeductionType>()
            .Property(d => d.DefaultAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DeductionType>()
            .Property(d => d.DefaultPercentage)
            .HasPrecision(5, 2);

        modelBuilder.Entity<EmployeeAllowance>()
            .Property(ea => ea.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<EmployeeAllowance>()
            .Property(ea => ea.Percentage)
            .HasPrecision(5, 2);

        modelBuilder.Entity<EmployeeDeduction>()
            .Property(ed => ed.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<EmployeeDeduction>()
            .Property(ed => ed.Percentage)
            .HasPrecision(5, 2);

        modelBuilder.Entity<PayslipAllowance>()
            .Property(pa => pa.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PayslipDeduction>()
            .Property(pd => pd.Amount)
            .HasPrecision(18, 2);

        // Configure SalaryHistory precision
        modelBuilder.Entity<SalaryHistory>()
            .Property(sh => sh.PreviousSalary)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SalaryHistory>()
            .Property(sh => sh.NewSalary)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SalaryHistory>()
            .Property(sh => sh.SalaryIncrease)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SalaryHistory>()
            .Property(sh => sh.IncreasePercentage)
            .HasPrecision(5, 2);

        // Configure Leave Management decimal properties
        modelBuilder.Entity<LeaveType>()
            .Property(lt => lt.AnnualAllocation)
            .HasPrecision(10, 2);

        modelBuilder.Entity<LeaveType>()
            .Property(lt => lt.MaxCarryOverDays)
            .HasPrecision(10, 2);

        modelBuilder.Entity<LeaveType>()
            .Property(lt => lt.MaxConsecutiveDays)
            .HasPrecision(10, 2);

        modelBuilder.Entity<LeaveBalance>()
            .Property(lb => lb.AllocatedDays)
            .HasPrecision(10, 2);

        modelBuilder.Entity<LeaveBalance>()
            .Property(lb => lb.UsedDays)
            .HasPrecision(10, 2);

        modelBuilder.Entity<LeaveBalance>()
            .Property(lb => lb.CarriedOverDays)
            .HasPrecision(10, 2);

        modelBuilder.Entity<LeaveBalance>()
            .Property(lb => lb.ExpiringDays)
            .HasPrecision(10, 2);

        modelBuilder.Entity<LeaveBalance>()
            .Property(lb => lb.AccruedDays)
            .HasPrecision(10, 2);

        modelBuilder.Entity<LeaveBalance>()
            .Property(lb => lb.PendingDays)
            .HasPrecision(10, 2);

        modelBuilder.Entity<LeaveRequest>()
            .Property(lr => lr.DaysRequested)
            .HasPrecision(10, 2);

        // Configure relationships
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Company)
            .WithMany(c => c.Employees)
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.User)
            .WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Department>()
            .HasOne(d => d.Company)
            .WithMany(c => c.Departments)
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Department>()
            .HasOne(d => d.Manager)
            .WithMany()
            .HasForeignKey(d => d.ManagerEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Payslip>()
            .HasOne(p => p.Employee)
            .WithMany(e => e.Payslips)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SalaryHistory>()
            .HasOne(sh => sh.Employee)
            .WithMany()
            .HasForeignKey(sh => sh.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Leave Management entity relationships
        modelBuilder.Entity<LeaveBalance>()
            .HasOne(lb => lb.Employee)
            .WithMany()
            .HasForeignKey(lb => lb.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeaveBalance>()
            .HasOne(lb => lb.LeaveType)
            .WithMany(lt => lt.LeaveBalances)
            .HasForeignKey(lb => lb.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LeaveRequest>()
            .HasOne(lr => lr.Employee)
            .WithMany()
            .HasForeignKey(lr => lr.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeaveRequest>()
            .HasOne(lr => lr.LeaveType)
            .WithMany(lt => lt.LeaveRequests)
            .HasForeignKey(lr => lr.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LeaveRequestApproval>()
            .HasOne(lra => lra.LeaveRequest)
            .WithMany(lr => lr.Approvals)
            .HasForeignKey(lra => lra.LeaveRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EmployeeAllowance>()
            .HasOne(ea => ea.Employee)
            .WithMany(e => e.EmployeeAllowances)
            .HasForeignKey(ea => ea.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EmployeeAllowance>()
            .HasOne(ea => ea.AllowanceType)
            .WithMany(at => at.EmployeeAllowances)
            .HasForeignKey(ea => ea.AllowanceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeDeduction>()
            .HasOne(ed => ed.Employee)
            .WithMany(e => e.EmployeeDeductions)
            .HasForeignKey(ed => ed.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EmployeeDeduction>()
            .HasOne(ed => ed.DeductionType)
            .WithMany(dt => dt.EmployeeDeductions)
            .HasForeignKey(ed => ed.DeductionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PayslipAllowance>()
            .HasOne(pa => pa.Payslip)
            .WithMany(p => p.PayslipAllowances)
            .HasForeignKey(pa => pa.PayslipId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PayslipAllowance>()
            .HasOne(pa => pa.AllowanceType)
            .WithMany(at => at.PayslipAllowances)
            .HasForeignKey(pa => pa.AllowanceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PayslipDeduction>()
            .HasOne(pd => pd.Payslip)
            .WithMany(p => p.PayslipDeductions)
            .HasForeignKey(pd => pd.PayslipId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PayslipDeduction>()
            .HasOne(pd => pd.DeductionType)
            .WithMany(dt => dt.PayslipDeductions)
            .HasForeignKey(pd => pd.DeductionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure indexes
        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.EmployeeCode)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Email)
            .IsUnique();

        modelBuilder.Entity<Payslip>()
            .HasIndex(p => p.PayslipNumber)
            .IsUnique();

        modelBuilder.Entity<Department>()
            .HasIndex(d => new { d.CompanyId, d.Code })
            .IsUnique();
        
        // Configure audit entity relationships
        modelBuilder.Entity<AuditLog>()
            .HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<SecurityEvent>()
            .HasOne(se => se.User)
            .WithMany()
            .HasForeignKey(se => se.UserId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<UserSession>()
            .HasOne(us => us.User)
            .WithMany()
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<DataChangeLog>()
            .HasOne(dcl => dcl.User)
            .WithMany()
            .HasForeignKey(dcl => dcl.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Configure audit indexes for performance
        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => al.CreatedAt);
            
        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => new { al.UserId, al.CreatedAt });
            
        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => new { al.EntityType, al.EntityId });
            
        modelBuilder.Entity<SecurityEvent>()
            .HasIndex(se => se.CreatedAt);
            
        modelBuilder.Entity<SecurityEvent>()
            .HasIndex(se => new { se.EventType, se.CreatedAt });
            
        modelBuilder.Entity<UserSession>()
            .HasIndex(us => us.SessionId)
            .IsUnique();
            
        modelBuilder.Entity<UserSession>()
            .HasIndex(us => new { us.UserId, us.LoginTime });
            
        modelBuilder.Entity<DataChangeLog>()
            .HasIndex(dcl => dcl.ChangeTime);
            
        modelBuilder.Entity<DataChangeLog>()
            .HasIndex(dcl => new { dcl.EntityType, dcl.EntityId, dcl.ChangeTime });

        // Configure AutoApprovalRuleLog foreign key relationships to prevent cascade conflicts
        modelBuilder.Entity<AutoApprovalRuleLog>()
            .HasOne(arl => arl.AutoApprovalRule)
            .WithMany(ar => ar.ApprovalLogs)
            .HasForeignKey(arl => arl.AutoApprovalRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AutoApprovalRuleLog>()
            .HasOne(arl => arl.LeaveRequest)
            .WithMany()
            .HasForeignKey(arl => arl.LeaveRequestId)
            .OnDelete(DeleteBehavior.Restrict);  // Prevent cascade conflict

        modelBuilder.Entity<AutoApprovalRuleLog>()
            .HasOne(arl => arl.Employee)
            .WithMany()
            .HasForeignKey(arl => arl.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);  // Prevent cascade conflict
    }
}