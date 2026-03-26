# HRPayroll_FullStack

Full-stack HR and payroll management system built with ASP.NET Core, EF Core, SQL Server, React, and Vite.

## Features

- Employee management
- Attendance tracking with `Start` / `End`
- Backend-only payroll calculation
- Editable attendance dropdowns for `Site / Project` and `Transport`
- Dashboard summaries and monthly reports
- Excel export for payroll reporting

## Tech Stack

- Backend: ASP.NET Core 8, EF Core, SQL Server
- Frontend: React 18, Vite, TypeScript, Tailwind CSS

## Project Structure

- `HRPayroll/` - backend solution
- `hr-payroll-frontend/` - React frontend

## Prerequisites

- .NET 8 SDK
- Node.js 18+
- SQL Server or SQL Server-compatible instance

## Local Setup

### 1. Configure backend secrets

Set the SQL connection string and JWT values with `dotnet user-secrets`:

```bash
dotnet user-secrets set "ConnectionStrings:LS" "Server=YOUR_SERVER;Database=LS;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True;" --project HRPayroll/HRPayroll.API/HRPayroll.API.csproj
dotnet user-secrets set "Jwt:Key" "YOUR_JWT_SECRET" --project HRPayroll/HRPayroll.API/HRPayroll.API.csproj
dotnet user-secrets set "Jwt:Issuer" "HRPayrollAPI" --project HRPayroll/HRPayroll.API/HRPayroll.API.csproj
dotnet user-secrets set "Jwt:Audience" "HRPayrollClient" --project HRPayroll/HRPayroll.API/HRPayroll.API.csproj
```

### 2. Run the backend

```bash
dotnet run --project HRPayroll/HRPayroll.API/HRPayroll.API.csproj
```

The API will be available at:

- `https://localhost:5001`
- `http://localhost:5000`

### 3. Run the frontend

```bash
cd hr-payroll-frontend
npm install
npm run dev
```

Frontend runs at:

- `http://localhost:5173`

## Default Dev Login

The seeded local/demo admin account is:

- Username: `admin`
- Password: `Admin@123`

## Useful Commands

```bash
# Backend build
dotnet build HRPayroll/HRPayroll.sln

# Frontend build
cd hr-payroll-frontend
npm run build
```

## Notes

- Backend payroll calculations are kept on the server.
- Attendance dropdown values are editable from the app.
- `UseLocalDb` is available for local in-memory development, but the default setup is SQL Server.
