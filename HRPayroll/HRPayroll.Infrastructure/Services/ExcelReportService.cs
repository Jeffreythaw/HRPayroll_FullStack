using ClosedXML.Excel;
using HRPayroll.Core.DTOs;
using HRPayroll.Core.Interfaces;
using HRPayroll.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HRPayroll.Infrastructure.Services;

public class ExcelReportService : IExcelReportService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IPublicHolidayService _publicHolidayService;

    public ExcelReportService(AppDbContext db, IConfiguration configuration, IPublicHolidayService publicHolidayService)
    {
        _db = db;
        _configuration = configuration;
        _publicHolidayService = publicHolidayService;
    }

    public async Task<byte[]> GenerateMonthlyReportAsync(ExcelReportRequest req)
    {
        var profileIdsActive = req.ProfileIds != null && req.ProfileIds.Count > 0;
        var employeeIdsActive = req.EmployeeIds != null && req.EmployeeIds.Count > 0;

        var payrolls = await _db.PayrollRecords
            .Include(p => p.Employee).ThenInclude(e => e.Department)
            .Include(p => p.EmployeePayrollProfile)
            .Where(p => p.Employee.Status == "Active" &&
                        p.EmployeePayrollProfile.Status == "Active" &&
                        p.Month == req.Month &&
                        p.Year == req.Year &&
                        (profileIdsActive ? req.ProfileIds!.Contains(p.EmployeePayrollProfileId)
                                          : (!employeeIdsActive || req.EmployeeIds!.Contains(p.EmployeeId))))
            .OrderBy(p => p.Employee.EmployeeCode)
            .ThenBy(p => p.EmployeePayrollProfile.ProfileName)
            .ToListAsync();

        var monthName = new DateTime(req.Year, req.Month, 1).ToString("MMMM yyyy");
        var daysInMonth = DateTime.DaysInMonth(req.Year, req.Month);

        using var workbook = new XLWorkbook();
        workbook.Style.Font.FontName = "Calibri";

        // Define colour palette
        var headerBg = XLColor.FromHtml("#1E3A5F");        // deep navy
        var subHeaderBg = XLColor.FromHtml("#2D6A9F");     // medium blue
        var sectionBg = XLColor.FromHtml("#E8F0FB");       // light blue tint
        var altRow = XLColor.FromHtml("#F5F8FF");           // very light blue
        var presentColor = XLColor.FromHtml("#1A7F37");     // green
        var absentColor = XLColor.FromHtml("#CF222E");      // red
        var leaveColor = XLColor.FromHtml("#9A6700");       // amber
        var holidayColor = XLColor.FromHtml("#6E40C9");     // purple
        var otColor = XLColor.FromHtml("#0969DA");          // blue
        var totalBg = XLColor.FromHtml("#FFF8DC");          // gold tint
        var summaryHeaderBg = XLColor.FromHtml("#0D47A1");  // dark blue

        if (payrolls.Count == 0)
        {
            throw new InvalidOperationException("No payroll records were found for the selected period.");
        }

        foreach (var payroll in payrolls)
        {
            var emp = payroll.Employee;
            var profile = payroll.EmployeePayrollProfile;
            var attendances = await _db.Attendances
                .Where(a => a.EmployeeId == emp.Id && a.Date.Month == req.Month && a.Date.Year == req.Year)
                .OrderBy(a => a.Date)
                .ToListAsync();

            var profileName = string.IsNullOrWhiteSpace(profile.ProfileName) ? "Primary" : profile.ProfileName;
            var sheetTitle = $"{emp.EmployeeCode} - {profileName}";
            var sheetName = sheetTitle[..Math.Min(31, sheetTitle.Length)];
            var ws = workbook.Worksheets.Add(sheetName);

            // ── Page Title ───────────────────────────────────────────
            ws.Range("A1:L1").Merge();
            var titleCell = ws.Cell("A1");
            titleCell.Value = $"PAYROLL REPORT — {monthName.ToUpper()}";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 16;
            titleCell.Style.Font.FontColor = XLColor.White;
            titleCell.Style.Fill.BackgroundColor = headerBg;
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 36;

            // ── Employee Info Header ──────────────────────────────────
            ws.Range("A2:L2").Merge();
            var infoTitleCell = ws.Cell("A2");
            infoTitleCell.Value = "EMPLOYEE INFORMATION";
            infoTitleCell.Style.Font.Bold = true;
            infoTitleCell.Style.Font.FontSize = 11;
            infoTitleCell.Style.Font.FontColor = XLColor.White;
            infoTitleCell.Style.Fill.BackgroundColor = subHeaderBg;
            infoTitleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Row(2).Height = 22;
            StyleAllBorders(ws.Range("A2:L2"), XLBorderStyleValues.Thin);

            // Employee info block (2 columns: label/value pairs)
            var infoData = new[]
            {
                ("Employee Code:", emp.EmployeeCode, "Full Name:", $"{emp.FirstName} {emp.LastName}"),
                ("Payroll Profile:", profileName, "Salary Mode:", string.IsNullOrWhiteSpace(profile.SalaryMode) ? "Monthly" : profile.SalaryMode),
                ("Department:", emp.Department?.Name ?? "", "Position:", emp.Position),
                ("Join Date:", emp.JoinDate?.ToString("dd/MM/yyyy") ?? "", "Basic Salary:", $"SGD {payroll.BasicSalary:N2}"),
                ("Daily Rate:", $"SGD {payroll.DailyRate:N2}", "OT Rate/Hour:", $"SGD {(profile.OTRatePerHour > 0 ? profile.OTRatePerHour : emp.OTRatePerHour):N2}"),
                ("Std. Work Hours:", $"{profile.StandardWorkHours} hrs/day", "Report Period:", monthName)
            };

            int infoRow = 3;
            foreach (var (lbl1, val1, lbl2, val2) in infoData)
            {
                ws.Range($"A{infoRow}:B{infoRow}").Merge();
                ws.Cell($"A{infoRow}").Value = lbl1;
                ws.Cell($"A{infoRow}").Style.Font.Bold = true;
                ws.Cell($"A{infoRow}").Style.Fill.BackgroundColor = sectionBg;

                ws.Range($"C{infoRow}:F{infoRow}").Merge();
                ws.Cell($"C{infoRow}").Value = val1;

                ws.Range($"G{infoRow}:H{infoRow}").Merge();
                ws.Cell($"G{infoRow}").Value = lbl2;
                ws.Cell($"G{infoRow}").Style.Font.Bold = true;
                ws.Cell($"G{infoRow}").Style.Fill.BackgroundColor = sectionBg;

                ws.Range($"I{infoRow}:L{infoRow}").Merge();
                ws.Cell($"I{infoRow}").Value = val2;

                ws.Row(infoRow).Height = 20;
                StyleAllBorders(ws.Range($"A{infoRow}:L{infoRow}"), XLBorderStyleValues.Thin);
                infoRow++;
            }

            // ── Spacer ────────────────────────────────────────────────
            infoRow++;

            // ── Attendance Table Header ───────────────────────────────
            ws.Range($"A{infoRow}:L{infoRow}").Merge();
            var attTitle = ws.Cell($"A{infoRow}");
            attTitle.Value = "DAILY ATTENDANCE RECORD";
            attTitle.Style.Font.Bold = true;
            attTitle.Style.Font.FontSize = 11;
            attTitle.Style.Font.FontColor = XLColor.White;
            attTitle.Style.Fill.BackgroundColor = subHeaderBg;
            attTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Row(infoRow).Height = 22;
            infoRow++;

            // Column headers
            var attHeaders = new[] { "No", "Date", "Day", "Start", "End", "Work Hours", "OT Hours", "Status", "Remarks", "", "", "" };
            // Use specific columns: A=No, B=Date, C=Day, D=Start, E=End, F=Work Hours, G=OT Hours, H=Status, I-L=Remarks
            string[] colLetters = { "A", "B", "C", "D", "E", "F", "G", "H" };
            string[] colHeaders = { "No.", "Date", "Day", "Start", "End", "Work Hours", "OT Hours", "Status" };

            for (int c = 0; c < colHeaders.Length; c++)
            {
                var cell = ws.Cell($"{colLetters[c]}{infoRow}");
                cell.Value = colHeaders[c];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = headerBg;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            ws.Range($"I{infoRow}:L{infoRow}").Merge();
            var remarkHeader = ws.Cell($"I{infoRow}");
            remarkHeader.Value = "Remarks";
            remarkHeader.Style.Font.Bold = true;
            remarkHeader.Style.Font.FontColor = XLColor.White;
            remarkHeader.Style.Fill.BackgroundColor = headerBg;
            remarkHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            StyleAllBorders(ws.Range($"A{infoRow}:L{infoRow}"), XLBorderStyleValues.Thin);
            ws.Row(infoRow).Height = 22;
            infoRow++;

            // Attendance data rows
            decimal totalWorkHours = 0; decimal totalOTHours = 0;
            int presentCount = 0, absentCount = 0, leaveCount = 0, holidayCount = 0;

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateOnly(req.Year, req.Month, day);
                var att = attendances.FirstOrDefault(a => a.Date == date);
                var dow = new DateTime(req.Year, req.Month, day).DayOfWeek;
                bool isSunday = dow == DayOfWeek.Sunday;

                var rowBg = (day % 2 == 0) ? altRow : XLColor.White;
                if (isSunday) rowBg = XLColor.FromHtml("#F0F0F0");

                ws.Cell($"A{infoRow}").Value = day;
                ws.Cell($"B{infoRow}").Value = date.ToString("dd/MM/yyyy");
                ws.Cell($"C{infoRow}").Value = dow.ToString()[..3].ToUpper();
                ws.Cell($"D{infoRow}").Value = att?.CheckIn?.ToString("HH:mm") ?? (isSunday ? "—" : "");
                ws.Cell($"E{infoRow}").Value = att?.CheckOut?.ToString("HH:mm") ?? (isSunday ? "—" : "");
                if (att != null)
                {
                    ws.Cell($"F{infoRow}").Value = att.WorkHours;
                    ws.Cell($"G{infoRow}").Value = att.OTHours;
                }
                else if (isSunday)
                {
                    ws.Cell($"F{infoRow}").Value = "—";
                    ws.Cell($"G{infoRow}").Value = "—";
                }
                else
                {
                    ws.Cell($"F{infoRow}").Value = "";
                    ws.Cell($"G{infoRow}").Value = "0";
                }

                var status = att?.Status ?? (isSunday ? "Sunday" : "Absent");
                ws.Range($"I{infoRow}:L{infoRow}").Merge();
                ws.Cell($"H{infoRow}").Value = status;
                ws.Cell($"I{infoRow}").Value = att?.Remarks ?? "";

                // Status colour
                var statusCell = ws.Cell($"H{infoRow}");
                statusCell.Style.Font.FontColor = status switch
                {
                    "Present" => presentColor,
                    "Absent" => absentColor,
                    "Leave" => leaveColor,
                    "Holiday" => holidayColor,
                    "HalfDay" => otColor,
                    _ => XLColor.Gray
                };
                statusCell.Style.Font.Bold = true;

                // Row background
                foreach (var col in new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I" })
                    ws.Cell($"{col}{infoRow}").Style.Fill.BackgroundColor = rowBg;

                StyleAllBorders(ws.Range($"A{infoRow}:L{infoRow}"), XLBorderStyleValues.Thin);
                ws.Cell($"A{infoRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell($"C{infoRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell($"D{infoRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell($"E{infoRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell($"F{infoRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell($"G{infoRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell($"H{infoRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (att != null)
                {
                    totalWorkHours += att.WorkHours;
                    totalOTHours += att.OTHours;
                    if (att.Status is "Present" or "HalfDay") presentCount++;
                    else if (att.Status == "Absent") absentCount++;
                    else if (att.Status == "Leave") leaveCount++;
                    else if (att.Status == "Holiday") holidayCount++;
                }
                else if (!isSunday) absentCount++;

                ws.Row(infoRow).Height = 18;
                infoRow++;
            }

            // Totals row
            ws.Range($"A{infoRow}:E{infoRow}").Merge();
            ws.Cell($"A{infoRow}").Value = "TOTALS";
            ws.Cell($"A{infoRow}").Style.Font.Bold = true;
            ws.Cell($"A{infoRow}").Style.Fill.BackgroundColor = totalBg;
            ws.Cell($"A{infoRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell($"F{infoRow}").Value = totalWorkHours;
            ws.Cell($"F{infoRow}").Style.Font.Bold = true;
            ws.Cell($"F{infoRow}").Style.Fill.BackgroundColor = totalBg;
            ws.Cell($"F{infoRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell($"G{infoRow}").Value = totalOTHours;
            ws.Cell($"G{infoRow}").Style.Font.Bold = true;
            ws.Cell($"G{infoRow}").Style.Fill.BackgroundColor = totalBg;
            ws.Cell($"G{infoRow}").Style.Fill.BackgroundColor = totalBg;
            ws.Cell($"G{infoRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range($"H{infoRow}:L{infoRow}").Merge();
            ws.Cell($"H{infoRow}").Value = $"Present: {presentCount}  Absent: {absentCount}  Leave: {leaveCount}";
            ws.Cell($"H{infoRow}").Style.Font.Bold = true;
            ws.Cell($"H{infoRow}").Style.Fill.BackgroundColor = totalBg;
            StyleAllBorders(ws.Range($"A{infoRow}:L{infoRow}"), XLBorderStyleValues.Medium);
            ws.Row(infoRow).Height = 22;
            infoRow += 2;

            // ── Payroll Summary ───────────────────────────────────────
            ws.Range($"A{infoRow}:L{infoRow}").Merge();
            var payTitle = ws.Cell($"A{infoRow}");
            payTitle.Value = "PAYROLL SUMMARY";
            payTitle.Style.Font.Bold = true;
            payTitle.Style.Font.FontSize = 11;
            payTitle.Style.Font.FontColor = XLColor.White;
            payTitle.Style.Fill.BackgroundColor = summaryHeaderBg;
            payTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Row(infoRow).Height = 22;
            infoRow++;

            // Payroll summary rows
            decimal workDays = Enumerable.Range(1, daysInMonth)
                .Count(d => new DateTime(req.Year, req.Month, d).DayOfWeek != DayOfWeek.Sunday);
            decimal grossMonthlyRate = payroll.BasicSalary + profile.ShiftAllowance;
            decimal dailyRate = payroll.DailyRate > 0
                ? payroll.DailyRate
                : (string.Equals(profile.SalaryMode, "Daily", StringComparison.OrdinalIgnoreCase)
                    ? profile.DailyRate
                    : (workDays > 0 ? grossMonthlyRate / workDays : 0));
            decimal hourlyBasicRate = string.Equals(profile.SalaryMode, "Daily", StringComparison.OrdinalIgnoreCase)
                ? (profile.DailyRate > 0 ? profile.DailyRate : dailyRate) / Math.Max(profile.StandardWorkHours, 1)
                : (payroll.BasicSalary * 12m) / (52m * 44m);
            decimal otRate = profile.OTRatePerHour > 0
                ? profile.OTRatePerHour
                : Math.Round(hourlyBasicRate * 1.5m, 2);
            decimal otAmount = payroll.OTAmount > 0 ? payroll.OTAmount : (totalOTHours * otRate);
            decimal deductions = payroll.Deductions > 0 ? payroll.Deductions : (absentCount * dailyRate);
            decimal grossSalary = payroll.GrossSalary > 0
                ? payroll.GrossSalary
                : (payroll.BasicSalary + profile.ShiftAllowance + profile.TransportationFee + otAmount);
            decimal netSalary = payroll.NetSalary > 0 ? payroll.NetSalary : (grossSalary - deductions);

            var summaryItems = new[]
            {
                ("Working Days in Month", workDays.ToString("N0"), "Present Days", presentCount.ToString()),
                ("Absent Days", absentCount.ToString(), "Leave Days", leaveCount.ToString()),
                ("Total Work Hours", totalWorkHours.ToString("N2"), "Total OT Hours", totalOTHours.ToString("N2")),
                ("Basic Salary (SGD)", payroll.BasicSalary.ToString("N2"), "Daily Rate (SGD)", dailyRate.ToString("N2")),
                ("OT Amount (SGD)", otAmount.ToString("N2"), "Deductions (SGD)", deductions.ToString("N2")),
                ("Gross Salary (SGD)", grossSalary.ToString("N2"), "Net Salary (SGD)", netSalary.ToString("N2"))
            };

            foreach (var (lbl1, val1, lbl2, val2) in summaryItems)
            {
                ws.Range($"A{infoRow}:C{infoRow}").Merge();
                ws.Cell($"A{infoRow}").Value = lbl1;
                ws.Cell($"A{infoRow}").Style.Font.Bold = true;
                ws.Cell($"A{infoRow}").Style.Fill.BackgroundColor = sectionBg;

                ws.Range($"D{infoRow}:F{infoRow}").Merge();
                ws.Cell($"D{infoRow}").Value = val1;
                ws.Cell($"D{infoRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Range($"G{infoRow}:I{infoRow}").Merge();
                ws.Cell($"G{infoRow}").Value = lbl2;
                ws.Cell($"G{infoRow}").Style.Font.Bold = true;
                ws.Cell($"G{infoRow}").Style.Fill.BackgroundColor = sectionBg;

                ws.Range($"J{infoRow}:L{infoRow}").Merge();
                ws.Cell($"J{infoRow}").Value = val2;
                ws.Cell($"J{infoRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                StyleAllBorders(ws.Range($"A{infoRow}:L{infoRow}"), XLBorderStyleValues.Thin);
                ws.Row(infoRow).Height = 20;
                infoRow++;
            }

            // Net salary highlighted row
            ws.Range($"A{infoRow}:F{infoRow}").Merge();
            ws.Cell($"A{infoRow}").Value = "NET SALARY TO PAY";
            ws.Cell($"A{infoRow}").Style.Font.Bold = true;
            ws.Cell($"A{infoRow}").Style.Font.FontSize = 13;
            ws.Cell($"A{infoRow}").Style.Font.FontColor = XLColor.White;
            ws.Cell($"A{infoRow}").Style.Fill.BackgroundColor = headerBg;
            ws.Cell($"A{infoRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Range($"G{infoRow}:L{infoRow}").Merge();
            ws.Cell($"G{infoRow}").Value = $"SGD {netSalary:N2}";
            ws.Cell($"G{infoRow}").Style.Font.Bold = true;
            ws.Cell($"G{infoRow}").Style.Font.FontSize = 13;
            ws.Cell($"G{infoRow}").Style.Font.FontColor = XLColor.White;
            ws.Cell($"G{infoRow}").Style.Fill.BackgroundColor = XLColor.FromHtml("#1A7F37");
            ws.Cell($"G{infoRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            StyleAllBorders(ws.Range($"A{infoRow}:L{infoRow}"), XLBorderStyleValues.Medium);
            ws.Row(infoRow).Height = 28;

            // ── Column widths ─────────────────────────────────────────
            ws.Column("A").Width = 6;
            ws.Column("B").Width = 14;
            ws.Column("C").Width = 8;
            ws.Column("D").Width = 12;
            ws.Column("E").Width = 12;
            ws.Column("F").Width = 12;
            ws.Column("G").Width = 10;
            ws.Column("H").Width = 12;
            ws.Column("I").Width = 10;
            ws.Column("J").Width = 10;
            ws.Column("K").Width = 10;
            ws.Column("L").Width = 10;

            // Freeze top rows
            ws.SheetView.FreezeRows(infoRow - daysInMonth - 8);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GeneratePaymentVoucherPdfAsync(ExcelReportRequest req)
    {
        await _publicHolidayService.EnsureYearAsync(req.Year);
        var holidayDates = await _publicHolidayService.GetHolidayDatesAsync(req.Year);

        var profileIdsActive = req.ProfileIds != null && req.ProfileIds.Count > 0;
        var employeeIdsActive = req.EmployeeIds != null && req.EmployeeIds.Count > 0;

        var payrolls = await _db.PayrollRecords
            .Include(p => p.Employee).ThenInclude(e => e.Department)
            .Include(p => p.EmployeePayrollProfile)
            .Where(p => p.Employee.Status == "Active" &&
                        p.EmployeePayrollProfile.Status == "Active" &&
                        p.Month == req.Month &&
                        p.Year == req.Year &&
                        (profileIdsActive ? req.ProfileIds!.Contains(p.EmployeePayrollProfileId)
                                          : (!employeeIdsActive || req.EmployeeIds!.Contains(p.EmployeeId))))
            .OrderBy(p => p.Employee.EmployeeCode)
            .ThenBy(p => p.EmployeePayrollProfile.ProfileName)
            .ToListAsync();

        var vouchers = new List<PaymentVoucherData>();
        foreach (var payroll in payrolls)
        {
            var emp = payroll.Employee;
            var profile = payroll.EmployeePayrollProfile;

            var periodLabel = new DateTime(req.Year, req.Month, 1).ToString("MMM yyyy");
            var payTo = string.IsNullOrWhiteSpace(emp.FinNo)
                ? $"{emp.FirstName} {emp.LastName}".Trim()
                : $"{emp.FirstName} {emp.LastName} ({emp.FinNo})".Trim();
            var processedAt = payroll.ProcessedAt == default ? DateTime.UtcNow : payroll.ProcessedAt;
            var voucherDate = processedAt.AddHours(8).Date;
            var workingDays = payroll.WorkingDays > 0
                ? payroll.WorkingDays
                : Enumerable.Range(1, DateTime.DaysInMonth(req.Year, req.Month))
                    .Count(d =>
                    {
                        var date = new DateOnly(req.Year, req.Month, d);
                        return date.DayOfWeek != DayOfWeek.Sunday && !holidayDates.Contains(date);
                    });

            var salaryMode = string.IsNullOrWhiteSpace(profile.SalaryMode) ? "Monthly" : profile.SalaryMode;
            var otRate = payroll.TotalOTHours > 0
                ? (profile.OTRatePerHour > 0 ? profile.OTRatePerHour : payroll.OTAmount / payroll.TotalOTHours)
                : profile.OTRatePerHour;
            var baseLabel = salaryMode.Equals("Daily", StringComparison.OrdinalIgnoreCase)
                ? $"Salary For {periodLabel}"
                : $"Monthly Salary ({periodLabel})";
            var baseAmount = payroll.BasicSalary;
            var dailyRate = payroll.DailyRate > 0
                ? payroll.DailyRate
                : (salaryMode.Equals("Daily", StringComparison.OrdinalIgnoreCase) ? profile.DailyRate : (workingDays > 0 ? payroll.BasicSalary / workingDays : 0));

            var attendanceOtHours = payroll.TotalOTHours - (profile.SundayPhOtDays * profile.StandardWorkHours) - profile.PublicHolidayOtHours;
            if (attendanceOtHours < 0) attendanceOtHours = 0;

            var attendanceOtAmount = Math.Round(attendanceOtHours * otRate, 2, MidpointRounding.AwayFromZero);
            var sundayPhAmount = Math.Round(profile.SundayPhOtDays * profile.StandardWorkHours * otRate, 2, MidpointRounding.AwayFromZero);
            var publicHolidayAmount = Math.Round(profile.PublicHolidayOtHours * otRate, 2, MidpointRounding.AwayFromZero);
            var noWorkDeduction = Math.Round(payroll.AbsentDays * dailyRate, 2, MidpointRounding.AwayFromZero);
            var fixedDeduction = Math.Round(profile.DeductionNoWork4Days, 2, MidpointRounding.AwayFromZero);
            var advanceSalary = Math.Round(profile.AdvanceSalary, 2, MidpointRounding.AwayFromZero);

            var lines = new List<VoucherLineItem>
            {
                new(baseLabel, baseAmount, false),
                new("Shift Allowance", profile.ShiftAllowance, false),
                new($"Total OT Hours {attendanceOtHours:N1} ({otRate:N2}/hr)", attendanceOtAmount, false),
                new("OT @ Sunday/P.H (days)", sundayPhAmount, false),
                new("OT @ Public Holiday (hrs)", publicHolidayAmount, false),
                new("Transportation Fee", profile.TransportationFee, false),
                new(payroll.AbsentDays > 0
                    ? $"Deduction (No Work / {payroll.AbsentDays} days)"
                    : "Deduction (No Work)", -noWorkDeduction, true),
                new("Deduction (No Work / 4 days)", -fixedDeduction, true),
                new("Advance Salary Deduction", -advanceSalary, true)
            };

            var totalAmount = lines.Sum(x => x.Amount);

            vouchers.Add(new PaymentVoucherData
            {
                CompanyName = GetReportSetting("CompanyName", "HR Payroll").ToUpperInvariant(),
                SignerName = GetReportSetting("SignerName", "HR"),
                Footnote = GetReportSetting(
                    "Footnote",
                    "Please review this payslip carefully. If you have any questions or concerns regarding your salary, please contact HR within 7 working days. No-reply if acceptance."),
                PaymentMethodLabel = GetReportSetting("PaymentMethodLabel", "Cheque / Cash :"),
                PaymentMethodValue = GetReportSetting("PaymentMethodValue", string.Empty),
                VoucherNumber = $"PV{voucherDate:yyMM}{vouchers.Count + 1:00}",
                VoucherDate = voucherDate,
                PayTo = payTo,
                PeriodLabel = periodLabel,
                Lines = lines,
                TotalAmount = totalAmount
            });
        }

        if (vouchers.Count == 0)
        {
            throw new InvalidOperationException("No payroll records were found for the selected period.");
        }

        var document = Document.Create(container =>
        {
            foreach (var voucher in vouchers)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(9));
                    page.Content().Element(content => RenderVoucher(content, voucher));
                });
            }
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    private static void StyleAllBorders(IXLRange range, XLBorderStyleValues style)
    {
        range.Style.Border.OutsideBorder = style;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    private string GetReportSetting(string key, string fallback)
        => _configuration[$"Report:{key}"]?.Trim() is { Length: > 0 } value ? value : fallback;

    private static void RenderVoucher(IContainer container, PaymentVoucherData voucher)
    {
        container.Column(column =>
        {
            column.Spacing(8);

            column.Item().AlignCenter().Text(voucher.CompanyName)
                .SemiBold()
                .FontSize(15);

            column.Item().AlignCenter().Text("Payment Voucher")
                .SemiBold()
                .FontSize(11);

            column.Item().Row(row =>
            {
                row.RelativeItem().AlignLeft().Text(text =>
                {
                    text.Span("Pay to : ").SemiBold();
                    text.Span(voucher.PayTo);
                });

                row.ConstantItem(190).AlignRight().Column(info =>
                {
                    info.Item().Row(r =>
                    {
                        r.RelativeItem().AlignRight().Text("PV No:").SemiBold();
                        r.ConstantItem(85).BorderBottom(1).AlignLeft().Text(voucher.VoucherNumber);
                    });

                    info.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().AlignRight().Text("Date:").SemiBold();
                        r.ConstantItem(85).BorderBottom(1).AlignLeft().Text(voucher.VoucherDate.ToString("d/M/yy"));
                    });
                });
            });

            column.Item().Height(1).Background(Colors.Grey.Darken3);

            column.Item().PaddingTop(2).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4.6f);
                    columns.ConstantColumn(28);
                    columns.ConstantColumn(74);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Black).Padding(4).Text("Description")
                        .SemiBold().FontColor(Colors.White).AlignCenter();
                    header.Cell().Background(Colors.Black).Padding(4).Text("")
                        .SemiBold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Black).Padding(4).Text("Amount")
                        .SemiBold().FontColor(Colors.White).AlignCenter();
                });

                foreach (var line in voucher.Lines)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(line.Label)
                        .FontColor(line.IsDeduction ? Colors.Red.Darken2 : Colors.Black);

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("S$")
                        .AlignCenter()
                        .FontColor(line.IsDeduction ? Colors.Red.Darken2 : Colors.Black);

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(FormatMoney(line.Amount))
                        .AlignRight()
                        .FontColor(line.IsDeduction ? Colors.Red.Darken2 : Colors.Black);
                }

                table.Cell().ColumnSpan(2).BorderTop(1).Padding(4).AlignLeft().Text(text =>
                {
                    text.Span($"{voucher.PaymentMethodLabel} ").SemiBold();
                    text.Span(voucher.PaymentMethodValue);
                });

                table.Cell().BorderTop(1).Padding(4).AlignRight().Text(text =>
                {
                    text.Span("TOTAL: ").SemiBold();
                    text.Span("S$ ").SemiBold();
                    text.Span(FormatMoney(voucher.TotalAmount)).SemiBold();
                });
            });

            column.Item().PaddingTop(8).Text(voucher.Footnote)
                .FontSize(7)
                .Italic()
                .FontColor(Colors.Blue.Darken2);

            column.Item().PaddingTop(18).AlignCenter().Text(voucher.SignerName)
                .SemiBold()
                .Italic()
                .FontSize(12);

            column.Item().PaddingTop(14).Row(row =>
            {
                row.RelativeItem().PaddingTop(8).BorderTop(1).AlignCenter().Text("Prepaid By");
                row.RelativeItem().PaddingTop(8).BorderTop(1).AlignCenter().Text("Received By (Sign by Employee)");
            });
        });
    }

    private static string FormatMoney(decimal amount)
        => Math.Abs(amount) < 0.005m ? "0.00" : amount.ToString("N2");
}

internal sealed record VoucherLineItem(string Label, decimal Amount, bool IsDeduction);

internal sealed class PaymentVoucherData
{
    public string CompanyName { get; set; } = string.Empty;
    public string SignerName { get; set; } = string.Empty;
    public string Footnote { get; set; } = string.Empty;
    public string PaymentMethodLabel { get; set; } = string.Empty;
    public string PaymentMethodValue { get; set; } = string.Empty;
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; }
    public string PayTo { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public List<VoucherLineItem> Lines { get; set; } = new();
    public decimal TotalAmount { get; set; }
}
