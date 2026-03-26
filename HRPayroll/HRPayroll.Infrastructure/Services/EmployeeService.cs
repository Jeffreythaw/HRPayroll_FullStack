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
}
