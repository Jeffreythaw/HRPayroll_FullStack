namespace HRPayroll.Core.Entities;

public class Attendance
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    public decimal WorkHours { get; set; }
    public decimal OTHours { get; set; }
    public string? SiteProject { get; set; }
    public string? Transport { get; set; }
    public string Status { get; set; } = "Present"; // Present, Absent, Leave, Holiday, HalfDay
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Employee Employee { get; set; } = null!;
}
