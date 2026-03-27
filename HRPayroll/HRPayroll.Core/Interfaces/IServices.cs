using HRPayroll.Core.DTOs;
using HRPayroll.Core.Entities;

namespace HRPayroll.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetAllAsync();
    Task<DepartmentDto?> GetByIdAsync(int id);
    Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request);
    Task<DepartmentDto?> UpdateAsync(int id, CreateDepartmentRequest request);
    Task<bool> DeleteAsync(int id);
}

public interface IEmployeeService
{
    Task<List<EmployeeDto>> GetAllAsync(string? status = null, int? departmentId = null);
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request);
    Task<EmployeeDto?> UpdateAsync(int id, UpdateEmployeeRequest request);
    Task<bool> DeleteAsync(int id);
    Task<List<EmployeePayrollProfileDto>> GetProfilesAsync(int? employeeId = null, string? status = null);
    Task<EmployeePayrollProfileDto?> GetProfileByIdAsync(int id);
    Task<EmployeePayrollProfileDto> CreateProfileAsync(CreateEmployeePayrollProfileRequest request);
    Task<EmployeePayrollProfileDto?> UpdateProfileAsync(int id, UpdateEmployeePayrollProfileRequest request);
    Task<bool> DeleteProfileAsync(int id);
}

public interface IAttendanceService
{
    Task<List<AttendanceDto>> GetByMonthAsync(int month, int year, int? employeeId = null);
    Task<AttendanceDto?> GetByIdAsync(int id);
    Task<AttendanceDto> CreateAsync(CreateAttendanceRequest request);
    Task<AttendanceDto?> UpdateAsync(int id, UpdateAttendanceRequest request);
    Task<bool> DeleteAsync(int id);
    Task<AttendanceSummaryDto?> GetSummaryAsync(int employeeId, int month, int year);
    (decimal workHours, decimal otHours) CalculateHours(TimeOnly? start, TimeOnly? end, int standardHours, bool isOvernight, string status);
}

public interface IAttendanceLookupService
{
    Task<List<AttendanceLookupDto>> GetAllAsync(string? category = null);
    Task<AttendanceLookupDto?> GetByIdAsync(int id);
    Task<AttendanceLookupDto> CreateAsync(CreateAttendanceLookupRequest request);
    Task<AttendanceLookupDto?> UpdateAsync(int id, UpdateAttendanceLookupRequest request);
    Task<bool> DeleteAsync(int id);
}

public interface IPublicHolidayService
{
    Task<List<PublicHolidayDto>> GetByYearAsync(int year);
    Task<List<PublicHolidayDto>> SyncYearAsync(int year);
    Task EnsureYearAsync(int year);
    Task<HashSet<DateOnly>> GetHolidayDatesAsync(int year);
    Task<bool> IsHolidayAsync(DateOnly date);
}

public interface IPayrollService
{
    Task<List<PayrollRecordDto>> GetByMonthAsync(int month, int year);
    Task<PayrollRecordDto?> GetByIdAsync(int id);
    Task<List<PayrollRecordDto>> ProcessPayrollAsync(ProcessPayrollRequest request);
    Task<PayrollRecordDto?> UpdateStatusAsync(int id, UpdatePayrollStatusRequest request);
    Task<bool> DeleteAsync(int id);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync();
}

public interface IExcelReportService
{
    Task<byte[]> GenerateMonthlyReportAsync(ExcelReportRequest request);
    Task<byte[]> GeneratePaymentVoucherPdfAsync(ExcelReportRequest request);
}
