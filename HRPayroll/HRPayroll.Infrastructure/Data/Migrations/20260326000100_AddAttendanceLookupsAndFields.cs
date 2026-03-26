using HRPayroll.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRPayroll.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260326000100_AddAttendanceLookupsAndFields")]
public partial class AddAttendanceLookupsAndFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SiteProject",
            table: "SLE_Attendances",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Transport",
            table: "SLE_Attendances",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "SLE_AttendanceLookups",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SLE_AttendanceLookups", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SLE_AttendanceLookups_Category_Name",
            table: "SLE_AttendanceLookups",
            columns: new[] { "Category", "Name" },
            unique: true);

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SLE_AttendanceLookups");

        migrationBuilder.DropColumn(
            name: "SiteProject",
            table: "SLE_Attendances");

        migrationBuilder.DropColumn(
            name: "Transport",
            table: "SLE_Attendances");
    }
}
