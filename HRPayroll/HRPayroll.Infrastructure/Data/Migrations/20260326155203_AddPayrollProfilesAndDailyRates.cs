using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HRPayroll.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollProfilesAndDailyRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SLE_PayrollRecords_EmployeeId_Month_Year",
                table: "SLE_PayrollRecords");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "JoinDate",
                table: "SLE_Employees",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<decimal>(
                name: "DailyRate",
                table: "SLE_Employees",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SalaryMode",
                table: "SLE_Employees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Monthly");

            migrationBuilder.CreateTable(
                name: "SLE_EmployeePayrollProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ProfileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SalaryMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Monthly"),
                    BasicSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShiftAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OTRatePerHour = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SundayPhOtDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PublicHolidayOtHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TransportationFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeductionNoWork4Days = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdvanceSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StandardWorkHours = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SLE_EmployeePayrollProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SLE_EmployeePayrollProfiles_SLE_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "SLE_Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "EmployeePayrollProfileId",
                table: "SLE_PayrollRecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SLE_EmployeePayrollProfiles_EmployeeId_ProfileName",
                table: "SLE_EmployeePayrollProfiles",
                columns: new[] { "EmployeeId", "ProfileName" },
                unique: true);

            migrationBuilder.Sql("""
INSERT INTO SLE_EmployeePayrollProfiles
(
    EmployeeId, ProfileName, SalaryMode, BasicSalary, DailyRate, ShiftAllowance, OTRatePerHour,
    SundayPhOtDays, PublicHolidayOtHours, TransportationFee, DeductionNoWork4Days, AdvanceSalary,
    StandardWorkHours, IsPrimary, Status, CreatedAt, UpdatedAt
)
SELECT
    e.Id,
    'Primary',
    COALESCE(e.SalaryMode, 'Monthly'),
    e.BasicSalary,
    COALESCE(e.DailyRate, 0),
    e.ShiftAllowance,
    e.OTRatePerHour,
    e.SundayPhOtDays,
    e.PublicHolidayOtHours,
    e.TransportationFee,
    e.DeductionNoWork4Days,
    e.AdvanceSalary,
    e.StandardWorkHours,
    1,
    e.Status,
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
FROM SLE_Employees e
WHERE NOT EXISTS
(
    SELECT 1
    FROM SLE_EmployeePayrollProfiles p
    WHERE p.EmployeeId = e.Id AND p.ProfileName = 'Primary'
);

UPDATE pr
SET pr.EmployeePayrollProfileId = p.Id
FROM SLE_PayrollRecords pr
INNER JOIN SLE_EmployeePayrollProfiles p
    ON p.EmployeeId = pr.EmployeeId
   AND p.ProfileName = 'Primary'
WHERE pr.EmployeePayrollProfileId IS NULL;
""");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeePayrollProfileId",
                table: "SLE_PayrollRecords",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SLE_PayrollRecords_EmployeePayrollProfileId_Month_Year",
                table: "SLE_PayrollRecords",
                columns: new[] { "EmployeePayrollProfileId", "Month", "Year" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SLE_PayrollRecords_SLE_EmployeePayrollProfiles_EmployeePayrollProfileId",
                table: "SLE_PayrollRecords",
                column: "EmployeePayrollProfileId",
                principalTable: "SLE_EmployeePayrollProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SLE_PayrollRecords_SLE_EmployeePayrollProfiles_EmployeePayrollProfileId",
                table: "SLE_PayrollRecords");

            migrationBuilder.DropTable(
                name: "SLE_EmployeePayrollProfiles");

            migrationBuilder.DropColumn(
                name: "EmployeePayrollProfileId",
                table: "SLE_PayrollRecords");

            migrationBuilder.DropColumn(
                name: "DailyRate",
                table: "SLE_Employees");

            migrationBuilder.DropColumn(
                name: "SalaryMode",
                table: "SLE_Employees");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "JoinDate",
                table: "SLE_Employees",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SLE_PayrollRecords_EmployeeId_Month_Year",
                table: "SLE_PayrollRecords",
                columns: new[] { "EmployeeId", "Month", "Year" },
                unique: true);
        }
    }
}
