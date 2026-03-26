using HRPayroll.Core.DTOs;
using HRPayroll.Core.Entities;
using HRPayroll.Core.Interfaces;
using HRPayroll.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRPayroll.Infrastructure.Services;

public class PayrollService : IPayrollService
{
    private readonly AppDbContext _db;
    public PayrollService(AppDbContext db) => _db = db;

    private static PayrollRecordDto ToDto(PayrollRecord p) => new()
    {
        Id = p.Id,
        EmployeeId = p.EmployeeId,
        EmployeeName = p.Employee != null ? $"{p.Employee.FirstName} {p.Employee.LastName}" : "",
        EmployeeCode = p.Employee?.EmployeeCode ?? "",
        DepartmentName = p.Employee?.Department?.Name ?? "",
        Position = p.Employee?.Position ?? "",
        Month = p.Month,
        Year = p.Year,
        WorkingDays = p.WorkingDays,
        PresentDays = p.PresentDays,
        AbsentDays = p.AbsentDays,
        LeaveDays = p.LeaveDays,
        TotalWorkHours = p.TotalWorkHours,
        TotalOTHours = p.TotalOTHours,
        BasicSalary = p.BasicSalary,
        DailyRate = p.DailyRate,
        OTAmount = p.OTAmount,
        Deductions = p.Deductions,
        GrossSalary = p.GrossSalary,
        NetSalary = p.NetSalary,
        Status = p.Status,
        Notes = p.Notes,
        ProcessedAt = p.ProcessedAt
    };

    public async Task<List<PayrollRecordDto>> GetByMonthAsync(int month, int year)
    {
        return await _db.PayrollRecords
            .Include(p => p.Employee).ThenInclude(e => e.Department)
            .Where(p => p.Month == month && p.Year == year)
            .Select(p => new PayrollRecordDto
            {
                Id = p.Id,
                EmployeeId = p.EmployeeId,
                EmployeeName = p.Employee.FirstName + " " + p.Employee.LastName,
                EmployeeCode = p.Employee.EmployeeCode,
                DepartmentName = p.Employee.Department.Name,
                Position = p.Employee.Position,
                Month = p.Month,
                Year = p.Year,
                WorkingDays = p.WorkingDays,
                PresentDays = p.PresentDays,
                AbsentDays = p.AbsentDays,
                LeaveDays = p.LeaveDays,
                TotalWorkHours = p.TotalWorkHours,
                TotalOTHours = p.TotalOTHours,
                BasicSalary = p.BasicSalary,
                DailyRate = p.DailyRate,
                OTAmount = p.OTAmount,
                Deductions = p.Deductions,
                GrossSalary = p.GrossSalary,
                NetSalary = p.NetSalary,
                Status = p.Status,
                Notes = p.Notes,
                ProcessedAt = p.ProcessedAt
            }).ToListAsync();
    }

    public async Task<PayrollRecordDto?> GetByIdAsync(int id)
    {
        var p = await _db.PayrollRecords
            .Include(p => p.Employee).ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(p => p.Id == id);
        return p == null ? null : ToDto(p);
    }

    public async Task<List<PayrollRecordDto>> ProcessPayrollAsync(ProcessPayrollRequest req)
    {
        var employees = await _db.Employees
            .Include(e => e.Department)
            .Where(e => e.Status == "Active" && (req.EmployeeIds == null || req.EmployeeIds.Contains(e.Id)))
            .ToListAsync();

        var daysInMonth = DateTime.DaysInMonth(req.Year, req.Month);
        int workingDays = Enumerable.Range(1, daysInMonth)
            .Count(d => new DateTime(req.Year, req.Month, d).DayOfWeek != DayOfWeek.Sunday);

        var results = new List<PayrollRecordDto>();

        foreach (var emp in employees)
        {
            var attendances = await _db.Attendances
                .Where(a => a.EmployeeId == emp.Id && a.Date.Month == req.Month && a.Date.Year == req.Year)
                .ToListAsync();

            int presentDays = attendances.Count(a => a.Status is "Present" or "HalfDay");
            int absentDays = attendances.Count(a => a.Status == "Absent");
            int leaveDays = attendances.Count(a => a.Status == "Leave");
            decimal totalWorkHours = attendances.Sum(a => a.WorkHours);
            decimal attendanceOTHours = attendances.Sum(a => a.OTHours);
            decimal sundayPhHours = emp.SundayPhOtDays * emp.StandardWorkHours;
            decimal publicHolidayHours = emp.PublicHolidayOtHours;
            decimal totalOTHours = attendanceOTHours + sundayPhHours + publicHolidayHours;

            decimal dailyRate = workingDays > 0 ? emp.BasicSalary / workingDays : 0;
            decimal baseDeductions = absentDays * dailyRate;
            decimal deductions = baseDeductions + emp.DeductionNoWork4Days + emp.AdvanceSalary;
            decimal otAmount = totalOTHours * emp.OTRatePerHour;
            decimal grossSalary = emp.BasicSalary + emp.ShiftAllowance + emp.TransportationFee + otAmount;
            decimal netSalary = grossSalary - deductions;

            // Upsert payroll record
            var existing = await _db.PayrollRecords
                .FirstOrDefaultAsync(p => p.EmployeeId == emp.Id && p.Month == req.Month && p.Year == req.Year);

            if (existing != null)
            {
                existing.WorkingDays = workingDays;
                existing.PresentDays = presentDays;
                existing.AbsentDays = absentDays;
                existing.LeaveDays = leaveDays;
                existing.TotalWorkHours = totalWorkHours;
                existing.TotalOTHours = totalOTHours;
                existing.BasicSalary = emp.BasicSalary;
                existing.DailyRate = dailyRate;
                existing.OTAmount = otAmount;
                existing.Deductions = deductions;
                existing.GrossSalary = grossSalary;
                existing.NetSalary = netSalary;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.Employee = emp;
                results.Add(ToDto(existing));
            }
            else
            {
                var record = new PayrollRecord
                {
                    EmployeeId = emp.Id,
                    Month = req.Month,
                    Year = req.Year,
                    WorkingDays = workingDays,
                    PresentDays = presentDays,
                    AbsentDays = absentDays,
                    LeaveDays = leaveDays,
                    TotalWorkHours = totalWorkHours,
                    TotalOTHours = totalOTHours,
                    BasicSalary = emp.BasicSalary,
                    DailyRate = dailyRate,
                    OTAmount = otAmount,
                    Deductions = deductions,
                    GrossSalary = grossSalary,
                    NetSalary = netSalary,
                    Employee = emp
                };
                _db.PayrollRecords.Add(record);
                results.Add(ToDto(record));
            }
        }

        await _db.SaveChangesAsync();
        return results;
    }

    public async Task<PayrollRecordDto?> UpdateStatusAsync(int id, UpdatePayrollStatusRequest req)
    {
        var p = await _db.PayrollRecords
            .Include(p => p.Employee).ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (p == null) return null;
        p.Status = req.Status;
        p.Notes = req.Notes;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(p);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var p = await _db.PayrollRecords.FindAsync(id);
        if (p == null) return false;
        _db.PayrollRecords.Remove(p);
        await _db.SaveChangesAsync();
        return true;
    }
}
