namespace HRPayroll.API;

public static class SeedData
{
    public static void SeedAttendanceLookups(HRPayroll.Infrastructure.Data.AppDbContext db)
    {
        if (db.AttendanceLookups.Any())
            return;

        db.AttendanceLookups.AddRange(
            new HRPayroll.Core.Entities.AttendanceLookup { Category = "SiteProject", Name = "HQ", SortOrder = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HRPayroll.Core.Entities.AttendanceLookup { Category = "SiteProject", Name = "Client Site", SortOrder = 2, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HRPayroll.Core.Entities.AttendanceLookup { Category = "Transport", Name = "Company Transport", SortOrder = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HRPayroll.Core.Entities.AttendanceLookup { Category = "Transport", Name = "Self Transport", SortOrder = 2, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        db.SaveChanges();
    }

    public static void SeedPayrollProfiles(HRPayroll.Infrastructure.Data.AppDbContext db)
    {
        var unassigned = db.Departments.FirstOrDefault(d => d.Name == "Unassigned");
        if (unassigned == null)
        {
            unassigned = new HRPayroll.Core.Entities.Department
            {
                Name = "Unassigned",
                Description = "Imported workers without a department",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            db.Departments.Add(unassigned);
            db.SaveChanges();
        }

        var workers = new[]
        {
            new WorkerSeed("MYINT THAN", "M3402619R", 630m, 250m, 2.8m, "Monthly", 3.60m, null, null, null),
            new WorkerSeed("NAING MYINT", "G5893755Q", 1150m, 250m, 2.8m, "Monthly", 6.53m, null, null, null),
            new WorkerSeed("CHUA ZI JIAN", "M3487971T", 1000m, 250m, 0m, "Monthly", 5.68m, null, null, null),
            new WorkerSeed("KYAW ZIN HEIN", "G8924161N", 0m, 2m, 0m, "Daily", 4.50m, 24m, "Primary", new[]
            {
                new ProfileSeed("Transport Included", "Daily", 0m, 24m, 2m, 4.50m, 2.8m, 0m, 0m, 0m)
            }),
            new WorkerSeed("SAI MYAT SOE", "M3059427K", 0m, 2m, 0m, "Daily", 4.88m, 26m, null, null),
            new WorkerSeed("GOH WOO HANG", null, 0m, 0m, 2.8m, "Daily", 8.43m, 45m, null, null),
            new WorkerSeed("DINH VAN CU", "G4017889P", 1600m, 0m, 2.8m, "Monthly", 11.54m, null, "Primary", new[]
            {
                new ProfileSeed("Fixed Allowance", "Monthly", 3400m, 0m, 568m, 0m, 0m, 0m, 0m, 0m)
            }),
            new WorkerSeed("ZHANG JI JUN", null, 2500m, 0m, 0m, "Monthly", 4.50m, null, null, null)
        };

        foreach (var seed in workers)
        {
            var parts = seed.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = parts.Length > 1 ? string.Join(" ", parts.Take(parts.Length - 1)) : seed.Name;
            var lastName = parts.Length > 1 ? parts.Last() : "";
            var email = BuildEmail(seed.FinNo ?? seed.Name);

            var emp = seed.FinNo != null
                ? db.Employees.FirstOrDefault(e => e.FinNo == seed.FinNo)
                : db.Employees.FirstOrDefault(e => e.FirstName == firstName && e.LastName == lastName);

            if (emp == null)
            {
                emp = new HRPayroll.Core.Entities.Employee
                {
                    EmployeeCode = GenerateEmployeeCode(db),
                    FinNo = seed.FinNo,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Phone = null,
                    DepartmentId = unassigned.Id,
                    Position = "Worker",
                    SalaryMode = seed.SalaryMode,
                    BasicSalary = seed.BasicSalary,
                    DailyRate = seed.DailyRate ?? 0m,
                    ShiftAllowance = seed.ShiftAllowance,
                    OTRatePerHour = seed.OtRate,
                    SundayPhOtDays = 0m,
                    PublicHolidayOtHours = 0m,
                    TransportationFee = seed.Transport,
                    DeductionNoWork4Days = 0m,
                    AdvanceSalary = 0m,
                    StandardWorkHours = 8,
                    JoinDate = null,
                    Status = "Active",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                };
                db.Employees.Add(emp);
                db.SaveChanges();
            }
            else
            {
                emp.FirstName = firstName;
                emp.LastName = lastName;
                emp.Email = email;
                emp.DepartmentId = unassigned.Id;
                emp.Position = "Worker";
                emp.SalaryMode = seed.SalaryMode;
                emp.BasicSalary = seed.BasicSalary;
                emp.DailyRate = seed.DailyRate ?? 0m;
                emp.ShiftAllowance = seed.ShiftAllowance;
                emp.OTRatePerHour = seed.OtRate;
                emp.TransportationFee = seed.Transport;
                emp.JoinDate = null;
                emp.Status = "Active";
                emp.UpdatedAt = DateTime.UtcNow;
                db.SaveChanges();
            }

            UpsertProfile(db, emp.Id, "Primary", true, seed.SalaryMode, seed.BasicSalary, seed.DailyRate ?? 0m, seed.ShiftAllowance, seed.OtRate, 0m, 0m, seed.Transport, 0m, 0m, 8);

            if (seed.SecondaryProfiles != null)
            {
                foreach (var profile in seed.SecondaryProfiles)
                {
                    UpsertProfile(db, emp.Id, profile.ProfileName, false, profile.SalaryMode, profile.BasicSalary, profile.DailyRate, profile.ShiftAllowance, profile.OtRate, 0m, 0m, profile.Transport, 0m, 0m, 8);
                }
            }
        }

        db.SaveChanges();
    }

    private static void UpsertProfile(
        HRPayroll.Infrastructure.Data.AppDbContext db,
        int employeeId,
        string profileName,
        bool isPrimary,
        string salaryMode,
        decimal basicSalary,
        decimal dailyRate,
        decimal shiftAllowance,
        decimal otRate,
        decimal sundayPhDays,
        decimal publicHolidayHours,
        decimal transport,
        decimal deduction,
        decimal advance,
        int standardWorkHours)
    {
        var profile = db.EmployeePayrollProfiles.FirstOrDefault(p => p.EmployeeId == employeeId && p.ProfileName == profileName);
        if (profile == null)
        {
            profile = new HRPayroll.Core.Entities.EmployeePayrollProfile
            {
                EmployeeId = employeeId,
                ProfileName = profileName,
                IsPrimary = isPrimary,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            db.EmployeePayrollProfiles.Add(profile);
        }

        profile.SalaryMode = salaryMode;
        profile.BasicSalary = basicSalary;
        profile.DailyRate = dailyRate;
        profile.ShiftAllowance = shiftAllowance;
        profile.OTRatePerHour = otRate;
        profile.SundayPhOtDays = sundayPhDays;
        profile.PublicHolidayOtHours = publicHolidayHours;
        profile.TransportationFee = transport;
        profile.DeductionNoWork4Days = deduction;
        profile.AdvanceSalary = advance;
        profile.StandardWorkHours = standardWorkHours;
        profile.Status = "Active";
        profile.UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateEmployeeCode(HRPayroll.Infrastructure.Data.AppDbContext db)
        => $"EMP{(db.Employees.Count() + 1):D4}";

    private static string BuildEmail(string seed)
        => $"{seed.ToLowerInvariant().Replace(' ', '.').Replace("/", ".").Replace("@", "").Replace("(", "").Replace(")", "").Replace("-", ".").Replace(".", ".")}@company.local";

    private sealed record WorkerSeed(
        string Name,
        string? FinNo,
        decimal BasicSalary,
        decimal ShiftAllowance,
        decimal Transport,
        string SalaryMode,
        decimal OtRate,
        decimal? DailyRate,
        string? DefaultProfileName,
        ProfileSeed[]? SecondaryProfiles);

    private sealed record ProfileSeed(
        string ProfileName,
        string SalaryMode,
        decimal BasicSalary,
        decimal DailyRate,
        decimal ShiftAllowance,
        decimal OtRate,
        decimal Transport,
        decimal SundayPhDays,
        decimal PublicHolidayHours,
        decimal AdvanceSalary);
}
