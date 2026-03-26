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

    private static decimal CalculateDefaultOtRate(decimal basicSalary)
        => Math.Round(basicSalary / 24m / 11m * 1.5m, 2);

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
        BasicSalary = e.BasicSalary,
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
            BasicSalary = e.BasicSalary,
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
            BasicSalary = req.BasicSalary,
            ShiftAllowance = req.ShiftAllowance,
            OTRatePerHour = req.OTRatePerHour > 0 ? req.OTRatePerHour : CalculateDefaultOtRate(req.BasicSalary),
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
        emp.BasicSalary = req.BasicSalary;
        emp.ShiftAllowance = req.ShiftAllowance;
        emp.OTRatePerHour = req.OTRatePerHour > 0 ? req.OTRatePerHour : CalculateDefaultOtRate(req.BasicSalary);
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
}
