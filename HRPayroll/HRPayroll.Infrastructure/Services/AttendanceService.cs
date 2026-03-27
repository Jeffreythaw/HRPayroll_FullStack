using HRPayroll.Core.DTOs;
using HRPayroll.Core.Entities;
using HRPayroll.Core.Interfaces;
using HRPayroll.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRPayroll.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _db;
    public AttendanceService(AppDbContext db) => _db = db;

    private static AttendanceDto ToDto(Attendance a) => new()
    {
        Id = a.Id,
        EmployeeId = a.EmployeeId,
        EmployeeName = a.Employee != null ? $"{a.Employee.FirstName} {a.Employee.LastName}" : "",
        EmployeeCode = a.Employee?.EmployeeCode ?? "",
        Date = a.Date,
        Start = a.CheckIn,
        End = a.CheckOut,
        WorkHours = a.WorkHours,
        OTHours = a.OTHours,
        SiteProject = a.SiteProject,
        Transport = a.Transport,
        Status = a.Status,
        Remarks = a.Remarks
    };

    public async Task<List<AttendanceDto>> GetByMonthAsync(int month, int year, int? employeeId = null)
    {
        var query = _db.Attendances
            .Include(a => a.Employee)
            .Where(a => a.Date.Month == month && a.Date.Year == year);
        if (employeeId.HasValue) query = query.Where(a => a.EmployeeId == employeeId);
        return await query.OrderBy(a => a.Date).ThenBy(a => a.Employee.LastName).Select(a => new AttendanceDto
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            EmployeeName = a.Employee.FirstName + " " + a.Employee.LastName,
            EmployeeCode = a.Employee.EmployeeCode,
            Date = a.Date,
            Start = a.CheckIn,
            End = a.CheckOut,
            WorkHours = a.WorkHours,
            OTHours = a.OTHours,
            SiteProject = a.SiteProject,
            Transport = a.Transport,
            Status = a.Status,
            Remarks = a.Remarks
        }).ToListAsync();
    }

    public async Task<AttendanceDto?> GetByIdAsync(int id)
    {
        var a = await _db.Attendances.Include(a => a.Employee).FirstOrDefaultAsync(a => a.Id == id);
        return a == null ? null : ToDto(a);
    }

    public async Task<AttendanceDto> CreateAsync(CreateAttendanceRequest req)
    {
        var emp = await _db.Employees.FindAsync(req.EmployeeId)
            ?? throw new InvalidOperationException("Employee not found");

        var (workHours, otHours) = CalculateHours(req.Start, req.End, emp.StandardWorkHours);

        var att = new Attendance
        {
            EmployeeId = req.EmployeeId,
            Date = req.Date,
            CheckIn = req.Start,
            CheckOut = req.End,
            WorkHours = workHours,
            OTHours = otHours,
            SiteProject = req.SiteProject?.Trim(),
            Transport = req.Transport?.Trim(),
            Status = req.Status,
            Remarks = req.Remarks
        };
        _db.Attendances.Add(att);
        await _db.SaveChangesAsync();
        await _db.Entry(att).Reference(a => a.Employee).LoadAsync();
        return ToDto(att);
    }

    public async Task<AttendanceDto?> UpdateAsync(int id, UpdateAttendanceRequest req)
    {
        var att = await _db.Attendances.Include(a => a.Employee).FirstOrDefaultAsync(a => a.Id == id);
        if (att == null) return null;

        var (workHours, otHours) = CalculateHours(req.Start, req.End, att.Employee?.StandardWorkHours ?? 8);
        att.CheckIn = req.Start;
        att.CheckOut = req.End;
        att.WorkHours = workHours;
        att.OTHours = otHours;
        att.SiteProject = req.SiteProject?.Trim();
        att.Transport = req.Transport?.Trim();
        att.Status = req.Status;
        att.Remarks = req.Remarks;
        att.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(att);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var att = await _db.Attendances.FindAsync(id);
        if (att == null) return false;
        _db.Attendances.Remove(att);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<AttendanceSummaryDto?> GetSummaryAsync(int employeeId, int month, int year)
    {
        var emp = await _db.Employees.FindAsync(employeeId);
        if (emp == null) return null;

        var attendances = await _db.Attendances
            .Where(a => a.EmployeeId == employeeId && a.Date.Month == month && a.Date.Year == year)
            .ToListAsync();

        // Calculate working days in month (Mon–Sat, excluding Sunday)
        var daysInMonth = DateTime.DaysInMonth(year, month);
        int workingDays = Enumerable.Range(1, daysInMonth)
            .Count(d => new DateTime(year, month, d).DayOfWeek != DayOfWeek.Sunday);

        return new AttendanceSummaryDto
        {
            EmployeeId = employeeId,
            EmployeeName = $"{emp.FirstName} {emp.LastName}",
            Month = month,
            Year = year,
            WorkingDays = workingDays,
            PresentDays = attendances.Count(a => a.Status == "Present" || a.Status == "HalfDay"),
            AbsentDays = attendances.Count(a => a.Status == "Absent"),
            LeaveDays = attendances.Count(a => a.Status == "Leave"),
            TotalWorkHours = attendances.Sum(a => a.WorkHours),
            TotalOTHours = attendances.Sum(a => a.OTHours)
        };
    }

    public (decimal workHours, decimal otHours) CalculateHours(TimeOnly? start, TimeOnly? end, int standardHours)
    {
        if (!start.HasValue || !end.HasValue) return (0, 0);
        // Support overnight shifts (for example 9:00 PM -> 9:00 AM next day).
        var startTime = start.Value.ToTimeSpan();
        var endTime = end.Value.ToTimeSpan();
        var totalHours = (endTime - startTime).TotalHours;
        if (totalHours <= 0)
        {
            totalHours += 24;
        }
        var total = (decimal)totalHours;
        if (total <= 0) return (0, 0);
        // Deduct 1 hour lunch break if > 6 hours
        var effective = total > 6 ? total - 1 : total;
        var workHours = Math.Min(effective, standardHours);
        var otHours = effective > standardHours ? effective - standardHours : 0;
        return (Math.Round(workHours, 2), Math.Round(otHours, 2));
    }
}
