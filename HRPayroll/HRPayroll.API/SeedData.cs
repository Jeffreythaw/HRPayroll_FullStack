namespace HRPayroll.API;

public static class SeedData
{
    public static void SeedAttendanceLookups(HRPayroll.Infrastructure.Data.AppDbContext db)
    {
        if (db.AttendanceLookups.Any())
            return;

        db.AttendanceLookups.AddRange(
            new HRPayroll.Core.Entities.AttendanceLookup { Category = "SiteProject", Name = "HQ", SortOrder = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HRPayroll.Core.Entities.AttendanceLookup { Category = "SiteProject", Name = "Client Site", SortOrder = 2, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HRPayroll.Core.Entities.AttendanceLookup { Category = "Transport", Name = "Company Transport", SortOrder = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HRPayroll.Core.Entities.AttendanceLookup { Category = "Transport", Name = "Self Transport", SortOrder = 2, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        db.SaveChanges();
    }
}
