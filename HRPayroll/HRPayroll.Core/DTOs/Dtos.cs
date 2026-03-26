namespace HRPayroll.Core.DTOs;

// ─── Auth ─────────────────────────────────────────────────────
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

// ─── Department ───────────────────────────────────────────────
public class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int EmployeeCount { get; set; }
}

public class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ─── Employee ─────────────────────────────────────────────────
public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string? FinNo { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string SalaryMode { get; set; } = "Monthly";
    public decimal BasicSalary { get; set; }
    public decimal DailyRate { get; set; }
    public decimal ShiftAllowance { get; set; }
    public decimal OTRatePerHour { get; set; }
    public decimal SundayPhOtDays { get; set; }
    public decimal PublicHolidayOtHours { get; set; }
    public decimal TransportationFee { get; set; }
    public decimal DeductionNoWork4Days { get; set; }
    public decimal AdvanceSalary { get; set; }
    public int StandardWorkHours { get; set; }
    public DateOnly? JoinDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateEmployeeRequest
{
    public string? FinNo { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int DepartmentId { get; set; }
    public string Position { get; set; } = string.Empty;
    public string SalaryMode { get; set; } = "Monthly";
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
    public DateOnly? JoinDate { get; set; }
}

public class UpdateEmployeeRequest : CreateEmployeeRequest
{
    public string Status { get; set; } = "Active";
}

// ─── Employee Payroll Profiles ────────────────────────────────
public class EmployeePayrollProfileDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public string SalaryMode { get; set; } = "Monthly";
    public decimal BasicSalary { get; set; }
    public decimal DailyRate { get; set; }
    public decimal ShiftAllowance { get; set; }
    public decimal OTRatePerHour { get; set; }
    public decimal SundayPhOtDays { get; set; }
    public decimal PublicHolidayOtHours { get; set; }
    public decimal TransportationFee { get; set; }
    public decimal DeductionNoWork4Days { get; set; }
    public decimal AdvanceSalary { get; set; }
    public int StandardWorkHours { get; set; }
    public bool IsPrimary { get; set; }
    public string Status { get; set; } = "Active";
}

public class CreateEmployeePayrollProfileRequest
{
    public int EmployeeId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string SalaryMode { get; set; } = "Monthly";
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
    public bool IsPrimary { get; set; } = false;
    public string Status { get; set; } = "Active";
}

public class UpdateEmployeePayrollProfileRequest : CreateEmployeePayrollProfileRequest { }

// ─── Attendance ───────────────────────────────────────────────
public class AttendanceDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly? Start { get; set; }
    public TimeOnly? End { get; set; }
    public decimal WorkHours { get; set; }
    public decimal OTHours { get; set; }
    public string? SiteProject { get; set; }
    public string? Transport { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class CreateAttendanceRequest
{
    public int EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? Start { get; set; }
    public TimeOnly? End { get; set; }
    public string? SiteProject { get; set; }
    public string? Transport { get; set; }
    public string Status { get; set; } = "Present";
    public string? Remarks { get; set; }
}

public class UpdateAttendanceRequest
{
    public TimeOnly? Start { get; set; }
    public TimeOnly? End { get; set; }
    public string? SiteProject { get; set; }
    public string? Transport { get; set; }
    public string Status { get; set; } = "Present";
    public string? Remarks { get; set; }
}

public class AttendanceLookupDto
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class CreateAttendanceLookupRequest
{
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class UpdateAttendanceLookupRequest : CreateAttendanceLookupRequest { }

public class AttendanceSummaryDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public int WorkingDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LeaveDays { get; set; }
    public decimal TotalWorkHours { get; set; }
    public decimal TotalOTHours { get; set; }
}

// ─── Payroll ──────────────────────────────────────────────────
public class PayrollRecordDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string PayrollProfileName { get; set; } = string.Empty;
    public string SalaryMode { get; set; } = "Monthly";
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
    public decimal Deductions { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal NetSalary { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class ProcessPayrollRequest
{
    public int Month { get; set; }
    public int Year { get; set; }
    public List<int>? EmployeeIds { get; set; } // null = all active employees
}

public class UpdatePayrollStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

// ─── Dashboard ────────────────────────────────────────────────
public class DashboardSummaryDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public int OnLeaveToday { get; set; }
    public decimal TotalPayrollThisMonth { get; set; }
    public List<DepartmentHeadcountDto> DepartmentHeadcounts { get; set; } = new();
    public List<RecentAttendanceDto> RecentAttendances { get; set; } = new();
}

public class DepartmentHeadcountDto
{
    public string Department { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RecentAttendanceDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public TimeOnly? Start { get; set; }
    public DateOnly Date { get; set; }
}

// ─── Excel Report ─────────────────────────────────────────────
public class ExcelReportRequest
{
    public int Month { get; set; }
    public int Year { get; set; }
    public List<int>? EmployeeIds { get; set; }
    public List<int>? ProfileIds { get; set; }
}
