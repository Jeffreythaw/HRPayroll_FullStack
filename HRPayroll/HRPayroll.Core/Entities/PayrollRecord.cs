namespace HRPayroll.Core.Entities;

public class PayrollRecord
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int WorkingDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LeaveDays { get; set; }
    public decimal TotalWorkHours { get; set; }
    public decimal TotalOTHours { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal DailyRate { get; set; }
    public decimal OTAmount { get; set; }
    public decimal Deductions { get; set; }  // absent deductions
    public decimal GrossSalary { get; set; }
    public decimal NetSalary { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Approved, Paid
    public string? Notes { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Employee Employee { get; set; } = null!;
}
