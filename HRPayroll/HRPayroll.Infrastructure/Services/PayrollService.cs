using HRPayroll.Core.DTOs;
using HRPayroll.Core.Entities;
using HRPayroll.Core.Interfaces;
using HRPayroll.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRPayroll.Infrastructure.Services;

public class PayrollService : IPayrollService
{
    private readonly AppDbContext _db;
    private readonly IPublicHolidayService _publicHolidayService;
    public PayrollService(AppDbContext db, IPublicHolidayService publicHolidayService)
    {
        _db = db;
        _publicHolidayService = publicHolidayService;
    }

    private static PayrollRecordDto ToDto(PayrollRecord p) => new()
    {
        Id = p.Id,
        EmployeeId = p.EmployeeId,
        EmployeeName = p.Employee != null ? $"{p.Employee.FirstName} {p.Employee.LastName}" : "",
        EmployeeCode = p.Employee?.EmployeeCode ?? "",
        DepartmentName = p.Employee?.Department?.Name ?? "",
        Position = p.Employee?.Position ?? "",
        PayrollProfileName = p.EmployeePayrollProfile?.ProfileName ?? "",
        SalaryMode = p.EmployeePayrollProfile?.SalaryMode ?? "Monthly",
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
            .Include(p => p.EmployeePayrollProfile)
            .Where(p => p.Month == month && p.Year == year)
            .OrderBy(p => p.Employee.EmployeeCode)
            .ThenBy(p => p.EmployeePayrollProfile.ProfileName)
            .Select(p => new PayrollRecordDto
            {
                Id = p.Id,
                EmployeeId = p.EmployeeId,
                EmployeeName = p.Employee.FirstName + " " + p.Employee.LastName,
                EmployeeCode = p.Employee.EmployeeCode,
                DepartmentName = p.Employee.Department.Name,
                Position = p.Employee.Position,
                PayrollProfileName = p.EmployeePayrollProfile.ProfileName,
                SalaryMode = p.EmployeePayrollProfile.SalaryMode,
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
            .Include(p => p.EmployeePayrollProfile)
            .FirstOrDefaultAsync(p => p.Id == id);
        return p == null ? null : ToDto(p);
    }

    public async Task<List<PayrollRecordDto>> ProcessPayrollAsync(ProcessPayrollRequest req)
    {
        await _publicHolidayService.EnsureYearAsync(req.Year);
        var holidayDates = await _publicHolidayService.GetHolidayDatesAsync(req.Year);
        var holidaySet = holidayDates.ToHashSet();
        var adjustmentMap = (req.Adjustments ?? new List<PayrollProcessAdjustmentRequest>())
            .GroupBy(x => x.EmployeePayrollProfileId)
            .ToDictionary(g => g.Key, g => g.Last());

        var profiles = await _db.EmployeePayrollProfiles
            .Include(p => p.Employee).ThenInclude(e => e.Department)
            .Where(p => p.Status == "Active" && p.Employee.Status == "Active" &&
                        (req.EmployeeIds == null || req.EmployeeIds.Contains(p.EmployeeId)))
            .OrderBy(p => p.Employee.EmployeeCode)
            .ThenBy(p => p.ProfileName)
            .ToListAsync();

        var daysInMonth = DateTime.DaysInMonth(req.Year, req.Month);
        int workingDays = Enumerable.Range(1, daysInMonth)
            .Count(d =>
            {
                var date = new DateOnly(req.Year, req.Month, d);
                return date.DayOfWeek != DayOfWeek.Sunday && !holidayDates.Contains(date);
            });

        var results = new List<PayrollRecordDto>();

        foreach (var profile in profiles)
        {
            var emp = profile.Employee;
            var attendances = await _db.Attendances
                .Where(a => a.EmployeeId == emp.Id && a.Date.Month == req.Month && a.Date.Year == req.Year)
                .ToListAsync();

            int presentDays = attendances.Count(a => a.Status is "Present" or "HalfDay");
            int absentDays = attendances.Count(a => a.Status == "Absent");
            int leaveDays = attendances.Count(a => a.Status == "Leave");
            decimal totalWorkHours = attendances.Sum(a => a.WorkHours);
            decimal attendanceOTHours = attendances
                .Where(a => !holidaySet.Contains(a.Date))
                .Sum(a => a.OTHours);
            decimal transportDays = attendances.Count(a =>
                a.Status is "Present" or "HalfDay" &&
                !string.IsNullOrWhiteSpace(a.Transport));
            decimal sundayPhHours = attendances
                .Where(a =>
                    a.Status is "Present" or "HalfDay" &&
                    a.Date.DayOfWeek == DayOfWeek.Sunday &&
                    !holidaySet.Contains(a.Date))
                .Sum(a => a.WorkHours + a.OTHours);
            decimal publicHolidayHours = attendances
                .Where(a => a.Status is "Present" or "HalfDay" && holidaySet.Contains(a.Date))
                .Sum(a => a.WorkHours + a.OTHours);
            decimal totalOTHours = attendanceOTHours + sundayPhHours + publicHolidayHours;

            var salaryMode = string.IsNullOrWhiteSpace(profile.SalaryMode) ? "Monthly" : profile.SalaryMode;
            var isDaily = salaryMode.Equals("Daily", StringComparison.OrdinalIgnoreCase);
            decimal grossMonthlyRate = salaryMode.Equals("Daily", StringComparison.OrdinalIgnoreCase)
                ? 0
                : profile.BasicSalary + profile.ShiftAllowance;
            decimal dailyRate = isDaily
                ? (profile.DailyRate > 0 ? profile.DailyRate : 0)
                : (workingDays > 0 ? grossMonthlyRate / workingDays : 0);
            decimal hourlyBasicRate = isDaily
                ? (profile.DailyRate > 0 ? profile.DailyRate : 0) / Math.Max(profile.StandardWorkHours, 1)
                : (profile.BasicSalary * 12m) / (52m * 44m);
            decimal transportAmount = Math.Max(profile.TransportationFee, 0) * transportDays * 2m;

            adjustmentMap.TryGetValue(profile.Id, out var adjustment);
            decimal baseSalary = isDaily
                ? dailyRate * presentDays
                : profile.BasicSalary;
            decimal fixedDeduction = isDaily ? 0 : Math.Max(adjustment?.DeductionNoWork4Days ?? 0, 0);
            decimal advanceSalary = Math.Max(adjustment?.AdvanceSalary ?? 0, 0);
            decimal deductions = fixedDeduction + advanceSalary;
            decimal otRate = profile.OTRatePerHour > 0
                ? profile.OTRatePerHour
                : Math.Round(hourlyBasicRate * 1.5m, 2);
            decimal otAmount = totalOTHours * otRate;
            decimal grossSalary = baseSalary + profile.ShiftAllowance + transportAmount + otAmount;
            decimal netSalary = grossSalary - deductions;

            var existing = await _db.PayrollRecords
                .FirstOrDefaultAsync(p => p.EmployeePayrollProfileId == profile.Id && p.Month == req.Month && p.Year == req.Year);

            if (existing != null)
            {
                existing.EmployeeId = emp.Id;
                existing.WorkingDays = workingDays;
                existing.PresentDays = presentDays;
                existing.AbsentDays = absentDays;
                existing.LeaveDays = leaveDays;
                existing.TotalWorkHours = totalWorkHours;
                existing.TotalOTHours = totalOTHours;
                existing.BasicSalary = baseSalary;
                existing.DailyRate = dailyRate;
                existing.OTAmount = otAmount;
                existing.Deductions = deductions;
                existing.GrossSalary = grossSalary;
                existing.NetSalary = netSalary;
                existing.Notes = BuildAdjustmentNotes(fixedDeduction, advanceSalary);
                existing.UpdatedAt = DateTime.UtcNow;
                existing.Employee = emp;
                existing.EmployeePayrollProfile = profile;
                results.Add(ToDto(existing));
            }
            else
            {
                var record = new PayrollRecord
                {
                    EmployeeId = emp.Id,
                    EmployeePayrollProfileId = profile.Id,
                    Month = req.Month,
                    Year = req.Year,
                    WorkingDays = workingDays,
                    PresentDays = presentDays,
                    AbsentDays = absentDays,
                    LeaveDays = leaveDays,
                    TotalWorkHours = totalWorkHours,
                    TotalOTHours = totalOTHours,
                    BasicSalary = baseSalary,
                    DailyRate = dailyRate,
                    OTAmount = otAmount,
                    Deductions = deductions,
                    GrossSalary = grossSalary,
                    NetSalary = netSalary,
                    Notes = BuildAdjustmentNotes(fixedDeduction, advanceSalary),
                    Employee = emp,
                    EmployeePayrollProfile = profile
                };
                _db.PayrollRecords.Add(record);
                results.Add(ToDto(record));
            }
        }

        await _db.SaveChangesAsync();
        return results;
    }

    private static string BuildAdjustmentNotes(decimal fixedDeduction, decimal advanceSalary)
        => $"adj:fixedDeduction={fixedDeduction:0.00};advanceSalary={advanceSalary:0.00}";

    private static string ExtractAdjustmentPrefix(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes) || !notes.StartsWith("adj:", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var newlineIndex = notes.IndexOf('\n');
        return newlineIndex >= 0 ? notes[..newlineIndex] : notes;
    }

    public async Task<PayrollRecordDto?> UpdateStatusAsync(int id, UpdatePayrollStatusRequest req)
    {
        var p = await _db.PayrollRecords
            .Include(p => p.Employee).ThenInclude(e => e.Department)
            .Include(p => p.EmployeePayrollProfile)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (p == null) return null;
        var adjustmentPrefix = ExtractAdjustmentPrefix(p.Notes);
        p.Status = req.Status;
        p.Notes = string.IsNullOrWhiteSpace(req.Notes)
            ? adjustmentPrefix
            : string.IsNullOrWhiteSpace(adjustmentPrefix)
                ? req.Notes
                : $"{adjustmentPrefix}\n{req.Notes}";
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
