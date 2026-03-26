using HRPayroll.Core.DTOs;
using HRPayroll.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRPayroll.API.Controllers;

// ─── Auth ─────────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _auth.LoginAsync(req);
        if (result == null) return Unauthorized(new { message = "Invalid credentials" });
        return Ok(result);
    }
}

// ─── Dashboard ────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _svc;
    public DashboardController(IDashboardService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _svc.GetSummaryAsync());
}

// ─── Departments ──────────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _svc;
    public DepartmentsController(IDepartmentService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _svc.GetAllAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id)
    {
        var d = await _svc.GetByIdAsync(id);
        return d == null ? NotFound() : Ok(d);
    }
    [HttpPost] public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest req) =>
        CreatedAtAction(nameof(Get), new { id = (await _svc.CreateAsync(req)).Id }, await _svc.CreateAsync(req));
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] CreateDepartmentRequest req)
    {
        var d = await _svc.UpdateAsync(id, req);
        return d == null ? NotFound() : Ok(d);
    }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id)
    {
        var ok = await _svc.DeleteAsync(id);
        return ok ? NoContent() : BadRequest(new { message = "Cannot delete department with employees" });
    }
}

// ─── Employees ────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _svc;
    public EmployeesController(IEmployeeService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int? departmentId) =>
        Ok(await _svc.GetAllAsync(status, departmentId));

    [HttpGet("{id}")] public async Task<IActionResult> Get(int id)
    {
        var e = await _svc.GetByIdAsync(id);
        return e == null ? NotFound() : Ok(e);
    }

    [HttpPost] public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest req)
    {
        var e = await _svc.CreateAsync(req);
        return CreatedAtAction(nameof(Get), new { id = e.Id }, e);
    }

    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeRequest req)
    {
        var e = await _svc.UpdateAsync(id, req);
        return e == null ? NotFound() : Ok(e);
    }

    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id)
    {
        var ok = await _svc.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}

// ─── Employee Payroll Profiles ───────────────────────────────────────────────
[ApiController]
[Route("api/employee-profiles")]
[Authorize]
public class EmployeeProfilesController : ControllerBase
{
    private readonly IEmployeeService _svc;
    public EmployeeProfilesController(IEmployeeService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? employeeId, [FromQuery] string? status) =>
        Ok(await _svc.GetProfilesAsync(employeeId, status));

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var profile = await _svc.GetProfileByIdAsync(id);
        return profile == null ? NotFound() : Ok(profile);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeePayrollProfileRequest req)
    {
        try
        {
            var profile = await _svc.CreateProfileAsync(req);
            return CreatedAtAction(nameof(Get), new { id = profile.Id }, profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeePayrollProfileRequest req)
    {
        try
        {
            var profile = await _svc.UpdateProfileAsync(id, req);
            return profile == null ? NotFound() : Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _svc.DeleteProfileAsync(id);
        return ok ? NoContent() : BadRequest(new { message = "Primary profiles cannot be deleted" });
    }
}

// ─── Attendance ───────────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendancesController : ControllerBase
{
    private readonly IAttendanceService _svc;
    public AttendancesController(IAttendanceService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetByMonth([FromQuery] int month, [FromQuery] int year, [FromQuery] int? employeeId) =>
        Ok(await _svc.GetByMonthAsync(month, year, employeeId));

    [HttpGet("{id}")] public async Task<IActionResult> Get(int id)
    {
        var a = await _svc.GetByIdAsync(id);
        return a == null ? NotFound() : Ok(a);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int employeeId, [FromQuery] int month, [FromQuery] int year)
    {
        var s = await _svc.GetSummaryAsync(employeeId, month, year);
        return s == null ? NotFound() : Ok(s);
    }

    [HttpPost] public async Task<IActionResult> Create([FromBody] CreateAttendanceRequest req)
    {
        try
        {
            var a = await _svc.CreateAsync(req);
            return CreatedAtAction(nameof(Get), new { id = a.Id }, a);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] UpdateAttendanceRequest req)
    {
        var a = await _svc.UpdateAsync(id, req);
        return a == null ? NotFound() : Ok(a);
    }

    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id)
    {
        var ok = await _svc.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}

// ─── Attendance Lookups ──────────────────────────────────────────────────────
[ApiController]
[Route("api/attendance-lookups")]
[Authorize]
public class AttendanceLookupsController : ControllerBase
{
    private readonly IAttendanceLookupService _svc;
    public AttendanceLookupsController(IAttendanceLookupService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? category) =>
        Ok(await _svc.GetAllAsync(category));

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await _svc.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAttendanceLookupRequest req)
    {
        try
        {
            var item = await _svc.CreateAsync(req);
            return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAttendanceLookupRequest req)
    {
        try
        {
            var item = await _svc.UpdateAsync(id, req);
            return item == null ? NotFound() : Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _svc.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}

// ─── Payroll ──────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _svc;
    public PayrollController(IPayrollService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetByMonth([FromQuery] int month, [FromQuery] int year) =>
        Ok(await _svc.GetByMonthAsync(month, year));

    [HttpGet("{id}")] public async Task<IActionResult> Get(int id)
    {
        var p = await _svc.GetByIdAsync(id);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpPost("process")]
    public async Task<IActionResult> Process([FromBody] ProcessPayrollRequest req) =>
        Ok(await _svc.ProcessPayrollAsync(req));

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdatePayrollStatusRequest req)
    {
        var p = await _svc.UpdateStatusAsync(id, req);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id)
    {
        var ok = await _svc.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}

// ─── Reports ──────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IExcelReportService _svc;
    public ReportsController(IExcelReportService svc) => _svc = svc;

    [HttpPost("excel")]
    public async Task<IActionResult> GenerateExcel([FromBody] ExcelReportRequest req)
    {
        var bytes = await _svc.GenerateMonthlyReportAsync(req);
        var fileName = $"Payroll_Report_{new DateTime(req.Year, req.Month, 1):MMM_yyyy}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("payment-voucher")]
    public async Task<IActionResult> GeneratePaymentVoucher([FromBody] ExcelReportRequest req)
    {
        var bytes = await _svc.GeneratePaymentVoucherPdfAsync(req);
        var fileName = $"Payment_Voucher_{new DateTime(req.Year, req.Month, 1):MMM_yyyy}.pdf";
        return File(bytes, "application/pdf", fileName);
    }
}
