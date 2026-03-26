using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace HRPayroll.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SLE_Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_SLE_Departments", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "SLE_Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Admin"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_SLE_Users", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "SLE_Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BasicSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OTRatePerHour = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StandardWorkHours = table.Column<int>(type: "int", nullable: false),
                    JoinDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SLE_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SLE_Employees_SLE_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "SLE_Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SLE_Attendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckIn = table.Column<TimeOnly>(type: "time", nullable: true),
                    CheckOut = table.Column<TimeOnly>(type: "time", nullable: true),
                    WorkHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OTHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SLE_Attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SLE_Attendances_SLE_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "SLE_Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SLE_PayrollRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    WorkingDays = table.Column<int>(type: "int", nullable: false),
                    PresentDays = table.Column<int>(type: "int", nullable: false),
                    AbsentDays = table.Column<int>(type: "int", nullable: false),
                    LeaveDays = table.Column<int>(type: "int", nullable: false),
                    TotalWorkHours = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    TotalOTHours = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    BasicSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OTAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Deductions = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrossSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SLE_PayrollRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SLE_PayrollRecords_SLE_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "SLE_Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Unique indexes
            migrationBuilder.CreateIndex(name: "IX_SLE_Users_Username", table: "SLE_Users", column: "Username", unique: true);
            migrationBuilder.CreateIndex(name: "IX_SLE_Employees_EmployeeCode", table: "SLE_Employees", column: "EmployeeCode", unique: true);
            migrationBuilder.CreateIndex(name: "IX_SLE_Employees_Email", table: "SLE_Employees", column: "Email", unique: true);
            migrationBuilder.CreateIndex(name: "IX_SLE_Employees_DepartmentId", table: "SLE_Employees", column: "DepartmentId");
            migrationBuilder.CreateIndex(name: "IX_SLE_Attendances_EmployeeId_Date", table: "SLE_Attendances", columns: new[] { "EmployeeId", "Date" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_SLE_PayrollRecords_EmployeeId_Month_Year", table: "SLE_PayrollRecords", columns: new[] { "EmployeeId", "Month", "Year" }, unique: true);

            // Seed data
            migrationBuilder.InsertData(table: "SLE_Users", columns: new[] { "Id", "Username", "PasswordHash", "PasswordSalt", "Role", "CreatedAt" },
                values: new object[] { 1, "admin", "Admin@123", "local-demo", "Admin", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(table: "SLE_Departments",
                columns: new[] { "Id", "Name", "Description", "CreatedAt" },
                values: new object[,]
                {
                    { 1, "Engineering", "Software & IT", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "Human Resources", "HR & Admin", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "Finance", "Finance & Accounting", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "Operations", "Operations & Logistics", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SLE_Attendances");
            migrationBuilder.DropTable(name: "SLE_PayrollRecords");
            migrationBuilder.DropTable(name: "SLE_Employees");
            migrationBuilder.DropTable(name: "SLE_Departments");
            migrationBuilder.DropTable(name: "SLE_Users");
        }
    }
}
