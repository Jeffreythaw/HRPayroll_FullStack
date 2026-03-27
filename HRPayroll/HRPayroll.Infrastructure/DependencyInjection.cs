using HRPayroll.Core.Interfaces;
using HRPayroll.Infrastructure.Data;
using HRPayroll.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRPayroll.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var useLocalDb = bool.TryParse(config["UseLocalDb"], out var parsedUseLocalDb) && parsedUseLocalDb;
        var connectionString = config.GetConnectionString("LS")
            ?? config.GetConnectionString("DefaultConnection");

        if (!useLocalDb && string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing connection string. Expected ConnectionStrings:LS or ConnectionStrings:DefaultConnection.");
        }

        services.AddHttpClient();
        services.AddDbContext<AppDbContext>(options =>
        {
            if (useLocalDb)
            {
                options.UseInMemoryDatabase("HRPayrollLocal");
            }
            else
            {
                options.UseSqlServer(connectionString,
                    b => b.MigrationsAssembly("HRPayroll.Infrastructure"));
            }
        });

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAttendanceLookupService, AttendanceLookupService>();
        services.AddScoped<IPublicHolidayService, PublicHolidayService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IExcelReportService, ExcelReportService>();

        return services;
    }
}
