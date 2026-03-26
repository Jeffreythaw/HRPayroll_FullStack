using HRPayroll.Core.DTOs;
using HRPayroll.Core.Interfaces;
using HRPayroll.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRPayroll.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var now = DateTime.UtcNow;

        var totalEmployees = await _db.Employees.CountAsync();
        var activeEmployees = await _db.Employees.CountAsync(e => e.Status == "Active");

        var todayAttendances = await _db.Attendances
            .Where(a => a.Date == today)
            .ToListAsync();

        var presentToday = todayAttendances.Count(a => a.Status is "Present" or "HalfDay");
        var absentToday = todayAttendances.Count(a => a.Status == "Absent");
        var onLeaveToday = todayAttendances.Count(a => a.Status == "Leave");

        var totalPayroll = await _db.PayrollRecords
            .Where(p => p.Month == now.Month && p.Year == now.Year)
            .SumAsync(p => (decimal?)p.NetSalary) ?? 0;

        var deptHeadcounts = await _db.Departments
            .Include(d => d.Employees)
            .Select(d => new DepartmentHeadcountDto
            {
                Department = d.Name,
                Count = d.Employees.Count(e => e.Status == "Active")
            })
            .ToListAsync();

        var recentAttendances = await _db.Attendances
            .Include(a => a.Employee)
            .OrderByDescending(a => a.Date)
            .Take(10)
            .Select(a => new RecentAttendanceDto
            {
                EmployeeName = a.Employee.FirstName + " " + a.Employee.LastName,
                Status = a.Status,
                Start = a.CheckIn,
                Date = a.Date
            })
            .ToListAsync();

        return new DashboardSummaryDto
        {
            TotalEmployees = totalEmployees,
            ActiveEmployees = activeEmployees,
            PresentToday = presentToday,
            AbsentToday = absentToday,
            OnLeaveToday = onLeaveToday,
            TotalPayrollThisMonth = totalPayroll,
            DepartmentHeadcounts = deptHeadcounts,
            RecentAttendances = recentAttendances
        };
    }
}
