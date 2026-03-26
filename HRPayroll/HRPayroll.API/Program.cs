using System.Text;
using HRPayroll.API;
using HRPayroll.API.Middleware;
using HRPayroll.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddInfrastructure(builder.Configuration);

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Missing Jwt:Key configuration.");
}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// CORS — allow local dev and public frontend deployments.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .ToArray() ?? Array.Empty<string>();

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("AllowFrontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins(
                    "http://localhost:5173",
                    "http://localhost:3000"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HR Payroll API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Middleware pipeline ────────────────────────────────────────
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// ── Auto-migrate on startup ────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HRPayroll.Infrastructure.Data.AppDbContext>();
    if (builder.Configuration.GetValue<bool>("UseLocalDb"))
    {
        db.Database.EnsureCreated();
        SeedLocalData(db);
        SeedData.SeedAttendanceLookups(db);
    }
    else
    {
        db.Database.Migrate();
        SeedData.SeedAttendanceLookups(db);
    }
}

app.Run();

static void SeedLocalData(HRPayroll.Infrastructure.Data.AppDbContext db)
{
    if (!db.Users.Any())
    {
        db.Users.Add(new HRPayroll.Core.Entities.User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = "Admin@123",
            PasswordSalt = "local-demo",
            Role = "Admin",
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }

    if (!db.Departments.Any())
    {
        db.Departments.AddRange(
            new HRPayroll.Core.Entities.Department { Id = 1, Name = "Engineering", Description = "Software & IT", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HRPayroll.Core.Entities.Department { Id = 2, Name = "Human Resources", Description = "HR & Admin", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HRPayroll.Core.Entities.Department { Id = 3, Name = "Finance", Description = "Finance & Accounting", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HRPayroll.Core.Entities.Department { Id = 4, Name = "Operations", Description = "Operations & Logistics", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }

    db.SaveChanges();
}
