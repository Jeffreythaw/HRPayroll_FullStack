using HRPayroll.Core.DTOs;
using HRPayroll.Core.Entities;
using HRPayroll.Core.Interfaces;
using HRPayroll.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRPayroll.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _db;
    public EmployeeService(AppDbContext db) => _db = db;

    private static decimal CalculateDefaultOtRate(string salaryMode, decimal basicSalary, decimal dailyRate)
        => string.Equals(salaryMode, "Daily", StringComparison.OrdinalIgnoreCase)
            ? Math.Round((dailyRate > 0 ? dailyRate : 24m) / 8m * 1.5m, 2)
            : Math.Round(basicSalary / 24m / 11m * 1.5m, 2);

    private static decimal ResolveDailyRate(string salaryMode, decimal basicSalary, decimal dailyRate)
        => string.Equals(salaryMode, "Daily", StringComparison.OrdinalIgnoreCase)
            ? (dailyRate > 0 ? dailyRate : 24m)
            : dailyRate;

    private static EmployeeDto ToDto(Employee e) => new()
    {
        Id = e.Id,
        EmployeeCode = e.EmployeeCode,
        FinNo = e.FinNo,
        FirstName = e.FirstName,
        LastName = e.LastName,
        Email = e.Email,
        Phone = e.Phone,
        DepartmentId = e.DepartmentId,
        DepartmentName = e.Department?.Name ?? "",
        Position = e.Position,
        SalaryMode = e.SalaryMode,
        BasicSalary = e.BasicSalary,
        DailyRate = e.DailyRate,
        ShiftAllowance = e.ShiftAllowance,
        OTRatePerHour = e.OTRatePerHour,
        SundayPhOtDays = e.SundayPhOtDays,
        PublicHolidayOtHours = e.PublicHolidayOtHours,
        TransportationFee = e.TransportationFee,
        DeductionNoWork4Days = e.DeductionNoWork4Days,
        AdvanceSalary = e.AdvanceSalary,
        StandardWorkHours = e.StandardWorkHours,
        JoinDate = e.JoinDate,
        Status = e.Status
    };

    private static EmployeePayrollProfileDto ToProfileDto(EmployeePayrollProfile p) => new()
    {
        Id = p.Id,
        EmployeeId = p.EmployeeId,
        EmployeeCode = p.Employee?.EmployeeCode ?? string.Empty,
        EmployeeName = p.Employee == null
            ? string.Empty
            : string.Join(" ", new[] { p.Employee.FirstName, p.Employee.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
        ProfileName = p.ProfileName,
        SalaryMode = p.SalaryMode,
        BasicSalary = p.BasicSalary,
        DailyRate = p.DailyRate,
        ShiftAllowance = p.ShiftAllowance,
        OTRatePerHour = p.OTRatePerHour,
        SundayPhOtDays = p.SundayPhOtDays,
        PublicHolidayOtHours = p.PublicHolidayOtHours,
        TransportationFee = p.TransportationFee,
        DeductionNoWork4Days = p.DeductionNoWork4Days,
        AdvanceSalary = p.AdvanceSalary,
        StandardWorkHours = p.StandardWorkHours,
        IsPrimary = p.IsPrimary,
        Status = p.Status
    };

    public async Task<List<EmployeeDto>> GetAllAsync(string? status = null, int? departmentId = null)
    {
        var query = _db.Employees.Include(e => e.Department).AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(e => e.Status == status);
        if (departmentId.HasValue) query = query.Where(e => e.DepartmentId == departmentId);
        return await query.Select(e => new EmployeeDto
        {
            Id = e.Id,
            EmployeeCode = e.EmployeeCode,
            FinNo = e.FinNo,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            Phone = e.Phone,
            DepartmentId = e.DepartmentId,
            DepartmentName = e.Department.Name,
            Position = e.Position,
            SalaryMode = e.SalaryMode,
            BasicSalary = e.BasicSalary,
            DailyRate = e.DailyRate,
            ShiftAllowance = e.ShiftAllowance,
            OTRatePerHour = e.OTRatePerHour,
            SundayPhOtDays = e.SundayPhOtDays,
            PublicHolidayOtHours = e.PublicHolidayOtHours,
            TransportationFee = e.TransportationFee,
            DeductionNoWork4Days = e.DeductionNoWork4Days,
            AdvanceSalary = e.AdvanceSalary,
            StandardWorkHours = e.StandardWorkHours,
            JoinDate = e.JoinDate,
            Status = e.Status
        }).ToListAsync();
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id)
    {
        var e = await _db.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == id);
        return e == null ? null : ToDto(e);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest req)
    {
        var code = await GenerateEmployeeCode();
        var emp = new Employee
        {
            EmployeeCode = code,
            FinNo = req.FinNo?.Trim(),
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email,
            Phone = req.Phone,
            DepartmentId = req.DepartmentId,
            Position = req.Position,
            SalaryMode = req.SalaryMode,
            BasicSalary = req.BasicSalary,
            DailyRate = req.DailyRate,
            ShiftAllowance = req.ShiftAllowance,
            OTRatePerHour = req.OTRatePerHour > 0 ? req.OTRatePerHour : CalculateDefaultOtRate(req.SalaryMode, req.BasicSalary, req.DailyRate),
            SundayPhOtDays = req.SundayPhOtDays,
            PublicHolidayOtHours = req.PublicHolidayOtHours,
            TransportationFee = req.TransportationFee,
            DeductionNoWork4Days = req.DeductionNoWork4Days,
            AdvanceSalary = req.AdvanceSalary,
            StandardWorkHours = req.StandardWorkHours,
            JoinDate = req.JoinDate
        };
        _db.Employees.Add(emp);
        await _db.SaveChangesAsync();
        await UpsertPrimaryPayrollProfile(emp, req, "Active");
        await _db.Entry(emp).Reference(e => e.Department).LoadAsync();
        return ToDto(emp);
    }

    public async Task<EmployeeDto?> UpdateAsync(int id, UpdateEmployeeRequest req)
    {
        var emp = await _db.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == id);
        if (emp == null) return null;
        emp.FinNo = req.FinNo?.Trim();
        emp.FirstName = req.FirstName;
        emp.LastName = req.LastName;
        emp.Email = req.Email;
        emp.Phone = req.Phone;
        emp.DepartmentId = req.DepartmentId;
        emp.Position = req.Position;
        emp.SalaryMode = req.SalaryMode;
        emp.BasicSalary = req.BasicSalary;
        emp.DailyRate = req.DailyRate;
        emp.ShiftAllowance = req.ShiftAllowance;
        emp.OTRatePerHour = req.OTRatePerHour > 0 ? req.OTRatePerHour : CalculateDefaultOtRate(req.SalaryMode, req.BasicSalary, req.DailyRate);
        emp.SundayPhOtDays = req.SundayPhOtDays;
        emp.PublicHolidayOtHours = req.PublicHolidayOtHours;
        emp.TransportationFee = req.TransportationFee;
        emp.DeductionNoWork4Days = req.DeductionNoWork4Days;
        emp.AdvanceSalary = req.AdvanceSalary;
        emp.StandardWorkHours = req.StandardWorkHours;
        emp.JoinDate = req.JoinDate;
        emp.Status = req.Status;
        emp.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await UpsertPrimaryPayrollProfile(emp, req, req.Status);
        await _db.Entry(emp).Reference(e => e.Department).LoadAsync();
        return ToDto(emp);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var emp = await _db.Employees.FindAsync(id);
        if (emp == null) return false;
        emp.Status = "Inactive";
        emp.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<EmployeePayrollProfileDto>> GetProfilesAsync(int? employeeId = null, string? status = null)
    {
        var query = _db.EmployeePayrollProfiles
            .Include(p => p.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(p => p.EmployeeId == employeeId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);

        return await query
            .OrderBy(p => p.Employee.EmployeeCode)
            .ThenBy(p => p.ProfileName)
            .Select(p => new EmployeePayrollProfileDto
            {
                Id = p.Id,
                EmployeeId = p.EmployeeId,
                EmployeeCode = p.Employee.EmployeeCode,
                EmployeeName = p.Employee.FirstName + " " + p.Employee.LastName,
                ProfileName = p.ProfileName,
                SalaryMode = p.SalaryMode,
                BasicSalary = p.BasicSalary,
                DailyRate = p.DailyRate,
                ShiftAllowance = p.ShiftAllowance,
                OTRatePerHour = p.OTRatePerHour,
                SundayPhOtDays = p.SundayPhOtDays,
                PublicHolidayOtHours = p.PublicHolidayOtHours,
                TransportationFee = p.TransportationFee,
                DeductionNoWork4Days = p.DeductionNoWork4Days,
                AdvanceSalary = p.AdvanceSalary,
                StandardWorkHours = p.StandardWorkHours,
                IsPrimary = p.IsPrimary,
                Status = p.Status
            })
            .ToListAsync();
    }

    public async Task<EmployeePayrollProfileDto?> GetProfileByIdAsync(int id)
    {
        var profile = await _db.EmployeePayrollProfiles
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == id);
        return profile == null ? null : ToProfileDto(profile);
    }

    public async Task<EmployeePayrollProfileDto> CreateProfileAsync(CreateEmployeePayrollProfileRequest request)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found.");

        var profileName = string.IsNullOrWhiteSpace(request.ProfileName) ? "Secondary" : request.ProfileName.Trim();
        var profile = await _db.EmployeePayrollProfiles
            .FirstOrDefaultAsync(p => p.EmployeeId == request.EmployeeId && p.ProfileName == profileName);
        if (profile != null)
            throw new InvalidOperationException("Profile name already exists for this employee.");

        profile = new EmployeePayrollProfile
        {
            EmployeeId = request.EmployeeId,
            ProfileName = profileName,
            IsPrimary = request.IsPrimary,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        ApplyProfileValues(profile, request.SalaryMode, request.BasicSalary, request.DailyRate, request.ShiftAllowance, request.OTRatePerHour,
            request.SundayPhOtDays, request.PublicHolidayOtHours, request.TransportationFee, request.DeductionNoWork4Days, request.AdvanceSalary,
            request.StandardWorkHours);

        _db.EmployeePayrollProfiles.Add(profile);
        await _db.SaveChangesAsync();
        await _db.Entry(profile).Reference(p => p.Employee).LoadAsync();
        return ToProfileDto(profile);
    }

    public async Task<EmployeePayrollProfileDto?> UpdateProfileAsync(int id, UpdateEmployeePayrollProfileRequest request)
    {
        var profile = await _db.EmployeePayrollProfiles
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (profile == null) return null;

        var profileName = string.IsNullOrWhiteSpace(request.ProfileName) ? profile.ProfileName : request.ProfileName.Trim();
        if (!profileName.Equals(profile.ProfileName, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _db.EmployeePayrollProfiles.AnyAsync(p =>
                p.EmployeeId == profile.EmployeeId &&
                p.ProfileName == profileName &&
                p.Id != profile.Id);
            if (exists)
                throw new InvalidOperationException("Profile name already exists for this employee.");
        }

        profile.ProfileName = profileName;
        profile.IsPrimary = request.IsPrimary;
        profile.Status = request.Status;
        profile.UpdatedAt = DateTime.UtcNow;
        ApplyProfileValues(profile, request.SalaryMode, request.BasicSalary, request.DailyRate, request.ShiftAllowance, request.OTRatePerHour,
            request.SundayPhOtDays, request.PublicHolidayOtHours, request.TransportationFee, request.DeductionNoWork4Days, request.AdvanceSalary,
            request.StandardWorkHours);

        await _db.SaveChangesAsync();
        return ToProfileDto(profile);
    }

    public async Task<bool> DeleteProfileAsync(int id)
    {
        var profile = await _db.EmployeePayrollProfiles.FirstOrDefaultAsync(p => p.Id == id);
        if (profile == null || profile.IsPrimary)
            return false;

        _db.EmployeePayrollProfiles.Remove(profile);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<string> GenerateEmployeeCode()
    {
        var count = await _db.Employees.CountAsync();
        return $"EMP{(count + 1):D4}";
    }

    private async Task UpsertPrimaryPayrollProfile(Employee emp, CreateEmployeeRequest req, string status)
    {
        var profile = await _db.EmployeePayrollProfiles
            .FirstOrDefaultAsync(p => p.EmployeeId == emp.Id && p.IsPrimary);

        if (profile == null)
        {
            profile = new EmployeePayrollProfile
            {
                EmployeeId = emp.Id,
                ProfileName = "Primary",
                IsPrimary = true,
            };
            _db.EmployeePayrollProfiles.Add(profile);
        }

        profile.SalaryMode = req.SalaryMode;
        profile.BasicSalary = req.BasicSalary;
        profile.DailyRate = ResolveDailyRate(req.SalaryMode, req.BasicSalary, req.DailyRate);
        profile.ShiftAllowance = req.ShiftAllowance;
        profile.OTRatePerHour = req.OTRatePerHour > 0 ? req.OTRatePerHour : CalculateDefaultOtRate(req.SalaryMode, req.BasicSalary, req.DailyRate);
        profile.SundayPhOtDays = req.SundayPhOtDays;
        profile.PublicHolidayOtHours = req.PublicHolidayOtHours;
        profile.TransportationFee = req.TransportationFee;
        profile.DeductionNoWork4Days = req.DeductionNoWork4Days;
        profile.AdvanceSalary = req.AdvanceSalary;
        profile.StandardWorkHours = req.StandardWorkHours;
        profile.Status = status;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static void ApplyProfileValues(
        EmployeePayrollProfile profile,
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
        profile.SalaryMode = salaryMode;
        profile.BasicSalary = basicSalary;
        profile.DailyRate = ResolveDailyRate(salaryMode, basicSalary, dailyRate);
        profile.ShiftAllowance = shiftAllowance;
        profile.OTRatePerHour = otRate > 0 ? otRate : CalculateDefaultOtRate(salaryMode, basicSalary, dailyRate);
        profile.SundayPhOtDays = sundayPhDays;
        profile.PublicHolidayOtHours = publicHolidayHours;
        profile.TransportationFee = transport;
        profile.DeductionNoWork4Days = deduction;
        profile.AdvanceSalary = advance;
        profile.StandardWorkHours = standardWorkHours;
    }
}
