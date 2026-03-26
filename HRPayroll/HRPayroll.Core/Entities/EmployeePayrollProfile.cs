namespace HRPayroll.Core.Entities;

public class EmployeePayrollProfile
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string ProfileName { get; set; } = "Primary";
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
    public int StandardWorkHours { get; set; } = 8;
    public bool IsPrimary { get; set; } = true;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Employee Employee { get; set; } = null!;
    public ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
}
