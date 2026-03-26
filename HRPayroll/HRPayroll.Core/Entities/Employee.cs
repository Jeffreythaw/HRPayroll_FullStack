namespace HRPayroll.Core.Entities;

public class Employee
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string? FinNo { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int DepartmentId { get; set; }
    public string Position { get; set; } = string.Empty;
    public string SalaryMode { get; set; } = "Monthly"; // Monthly, Daily
    public decimal BasicSalary { get; set; }
    public decimal DailyRate { get; set; }
    public decimal ShiftAllowance { get; set; }
    public decimal OTRatePerHour { get; set; }
    public decimal SundayPhOtDays { get; set; }
    public decimal PublicHolidayOtHours { get; set; }
    public decimal TransportationFee { get; set; }
    public decimal DeductionNoWork4Days { get; set; }
    public decimal AdvanceSalary { get; set; }
    public int StandardWorkHours { get; set; } = 8; // hours per day
    public DateOnly? JoinDate { get; set; }
    public string Status { get; set; } = "Active"; // Active, Inactive
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Department Department { get; set; } = null!;
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
    public ICollection<EmployeePayrollProfile> PayrollProfiles { get; set; } = new List<EmployeePayrollProfile>();
}
