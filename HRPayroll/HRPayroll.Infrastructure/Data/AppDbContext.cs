using HRPayroll.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRPayroll.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeePayrollProfile> EmployeePayrollProfiles => Set<EmployeePayrollProfile>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<AttendanceLookup> AttendanceLookups => Set<AttendanceLookup>();
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("SLE_Users");
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Role).HasDefaultValue("Admin");
        });

        // Department
        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("SLE_Departments");
            e.Property(d => d.Name).HasMaxLength(100).IsRequired();
        });

        // Employee
        modelBuilder.Entity<Employee>(e =>
        {
            e.ToTable("SLE_Employees");
            e.HasIndex(emp => emp.FinNo).IsUnique().HasFilter("[FinNo] IS NOT NULL AND [FinNo] <> ''");
            e.HasIndex(emp => emp.EmployeeCode).IsUnique();
            e.HasIndex(emp => emp.Email).IsUnique();
            e.Property(emp => emp.FinNo).HasMaxLength(100);
            e.Property(emp => emp.SalaryMode).HasMaxLength(20).HasDefaultValue("Monthly");
            e.Property(emp => emp.BasicSalary).HasColumnType("decimal(18,2)");
            e.Property(emp => emp.DailyRate).HasColumnType("decimal(18,2)");
            e.Property(emp => emp.ShiftAllowance).HasColumnType("decimal(18,2)");
            e.Property(emp => emp.OTRatePerHour).HasColumnType("decimal(18,2)");
            e.Property(emp => emp.SundayPhOtDays).HasColumnType("decimal(5,2)");
            e.Property(emp => emp.PublicHolidayOtHours).HasColumnType("decimal(5,2)");
            e.Property(emp => emp.TransportationFee).HasColumnType("decimal(18,2)");
            e.Property(emp => emp.DeductionNoWork4Days).HasColumnType("decimal(18,2)");
            e.Property(emp => emp.AdvanceSalary).HasColumnType("decimal(18,2)");
            e.Property(emp => emp.JoinDate).IsRequired(false);
            e.HasOne(emp => emp.Department)
             .WithMany(d => d.Employees)
             .HasForeignKey(emp => emp.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeePayrollProfile>(e =>
        {
            e.ToTable("SLE_EmployeePayrollProfiles");
            e.HasIndex(p => new { p.EmployeeId, p.ProfileName }).IsUnique();
            e.Property(p => p.ProfileName).HasMaxLength(100).IsRequired();
            e.Property(p => p.SalaryMode).HasMaxLength(20).HasDefaultValue("Monthly");
            e.Property(p => p.BasicSalary).HasColumnType("decimal(18,2)");
            e.Property(p => p.DailyRate).HasColumnType("decimal(18,2)");
            e.Property(p => p.ShiftAllowance).HasColumnType("decimal(18,2)");
            e.Property(p => p.OTRatePerHour).HasColumnType("decimal(18,2)");
            e.Property(p => p.SundayPhOtDays).HasColumnType("decimal(5,2)");
            e.Property(p => p.PublicHolidayOtHours).HasColumnType("decimal(5,2)");
            e.Property(p => p.TransportationFee).HasColumnType("decimal(18,2)");
            e.Property(p => p.DeductionNoWork4Days).HasColumnType("decimal(18,2)");
            e.Property(p => p.AdvanceSalary).HasColumnType("decimal(18,2)");
            e.HasOne(p => p.Employee)
             .WithMany(emp => emp.PayrollProfiles)
             .HasForeignKey(p => p.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Attendance
        modelBuilder.Entity<Attendance>(e =>
        {
            e.ToTable("SLE_Attendances");
            e.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();
            e.Property(a => a.WorkHours).HasColumnType("decimal(5,2)");
            e.Property(a => a.OTHours).HasColumnType("decimal(5,2)");
            e.Property(a => a.SiteProject).HasMaxLength(100);
            e.Property(a => a.Transport).HasMaxLength(100);
            e.HasOne(a => a.Employee)
             .WithMany(emp => emp.Attendances)
             .HasForeignKey(a => a.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AttendanceLookup>(e =>
        {
            e.ToTable("SLE_AttendanceLookups");
            e.HasIndex(x => new { x.Category, x.Name }).IsUnique();
            e.Property(x => x.Category).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.SortOrder).HasDefaultValue(0);
        });

        modelBuilder.Entity<PublicHoliday>(e =>
        {
            e.ToTable("SLE_PublicHolidays");
            e.HasIndex(x => x.Date).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.CountryCode).HasMaxLength(5).HasDefaultValue("SG");
            e.Property(x => x.Source).HasMaxLength(100).HasDefaultValue("data.gov.sg");
        });

        // PayrollRecord
        modelBuilder.Entity<PayrollRecord>(e =>
        {
            e.ToTable("SLE_PayrollRecords");
            e.HasIndex(p => new { p.EmployeePayrollProfileId, p.Month, p.Year }).IsUnique();
            e.Property(p => p.BasicSalary).HasColumnType("decimal(18,2)");
            e.Property(p => p.DailyRate).HasColumnType("decimal(18,2)");
            e.Property(p => p.OTAmount).HasColumnType("decimal(18,2)");
            e.Property(p => p.Deductions).HasColumnType("decimal(18,2)");
            e.Property(p => p.GrossSalary).HasColumnType("decimal(18,2)");
            e.Property(p => p.NetSalary).HasColumnType("decimal(18,2)");
            e.Property(p => p.TotalWorkHours).HasColumnType("decimal(8,2)");
            e.Property(p => p.TotalOTHours).HasColumnType("decimal(8,2)");
            e.HasOne(p => p.Employee)
             .WithMany(emp => emp.PayrollRecords)
             .HasForeignKey(p => p.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.EmployeePayrollProfile)
             .WithMany(profile => profile.PayrollRecords)
             .HasForeignKey(p => p.EmployeePayrollProfileId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed default admin user (password: Admin@123)
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = "Admin@123",
            PasswordSalt = "local-demo",
            Role = "Admin",
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Seed departments
        modelBuilder.Entity<Department>().HasData(
            new Department { Id = 1, Name = "Engineering", Description = "Software & IT", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 2, Name = "Human Resources", Description = "HR & Admin", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 3, Name = "Finance", Description = "Finance & Accounting", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 4, Name = "Operations", Description = "Operations & Logistics", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

    }
}
