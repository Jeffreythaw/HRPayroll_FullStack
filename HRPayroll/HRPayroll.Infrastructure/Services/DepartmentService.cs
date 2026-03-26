using HRPayroll.Core.DTOs;
using HRPayroll.Core.Entities;
using HRPayroll.Core.Interfaces;
using HRPayroll.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRPayroll.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly AppDbContext _db;
    public DepartmentService(AppDbContext db) => _db = db;

    public async Task<List<DepartmentDto>> GetAllAsync()
    {
        return await _db.Departments
            .Include(d => d.Employees)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                EmployeeCount = d.Employees.Count(e => e.Status == "Active")
            })
            .ToListAsync();
    }

    public async Task<DepartmentDto?> GetByIdAsync(int id)
    {
        var d = await _db.Departments.Include(d => d.Employees).FirstOrDefaultAsync(d => d.Id == id);
        if (d == null) return null;
        return new DepartmentDto { Id = d.Id, Name = d.Name, Description = d.Description, EmployeeCount = d.Employees.Count };
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentRequest req)
    {
        var dept = new Department { Name = req.Name, Description = req.Description };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();
        return new DepartmentDto { Id = dept.Id, Name = dept.Name, Description = dept.Description };
    }

    public async Task<DepartmentDto?> UpdateAsync(int id, CreateDepartmentRequest req)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept == null) return null;
        dept.Name = req.Name;
        dept.Description = req.Description;
        await _db.SaveChangesAsync();
        return new DepartmentDto { Id = dept.Id, Name = dept.Name, Description = dept.Description };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var dept = await _db.Departments.Include(d => d.Employees).FirstOrDefaultAsync(d => d.Id == id);
        if (dept == null || dept.Employees.Any()) return false;
        _db.Departments.Remove(dept);
        await _db.SaveChangesAsync();
        return true;
    }
}
