using HRPayroll.Core.DTOs;
using HRPayroll.Core.Entities;
using HRPayroll.Core.Interfaces;
using HRPayroll.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRPayroll.Infrastructure.Services;

public class AttendanceLookupService : IAttendanceLookupService
{
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "SiteProject",
        "Transport"
    };

    private readonly AppDbContext _db;
    public AttendanceLookupService(AppDbContext db) => _db = db;

    private static AttendanceLookupDto ToDto(AttendanceLookup lookup) => new()
    {
        Id = lookup.Id,
        Category = lookup.Category,
        Name = lookup.Name,
        IsActive = lookup.IsActive,
        SortOrder = lookup.SortOrder
    };

    private static string NormalizeCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new InvalidOperationException("Category is required.");

        var normalized = category.Trim();
        if (!AllowedCategories.Contains(normalized))
            throw new InvalidOperationException("Invalid category. Use SiteProject or Transport.");

        return AllowedCategories.First(x => x.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<AttendanceLookupDto>> GetAllAsync(string? category = null)
    {
        var query = _db.AttendanceLookups.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = NormalizeCategory(category);
            query = query.Where(x => x.Category == normalized);
        }

        return await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new AttendanceLookupDto
            {
                Id = x.Id,
                Category = x.Category,
                Name = x.Name,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder
            })
            .ToListAsync();
    }

    public async Task<AttendanceLookupDto?> GetByIdAsync(int id)
    {
        var lookup = await _db.AttendanceLookups.FindAsync(id);
        return lookup == null ? null : ToDto(lookup);
    }

    public async Task<AttendanceLookupDto> CreateAsync(CreateAttendanceLookupRequest request)
    {
        var lookup = new AttendanceLookup
        {
            Category = NormalizeCategory(request.Category),
            Name = request.Name.Trim(),
            IsActive = request.IsActive,
            SortOrder = request.SortOrder
        };

        _db.AttendanceLookups.Add(lookup);
        await _db.SaveChangesAsync();
        return ToDto(lookup);
    }

    public async Task<AttendanceLookupDto?> UpdateAsync(int id, UpdateAttendanceLookupRequest request)
    {
        var lookup = await _db.AttendanceLookups.FindAsync(id);
        if (lookup == null) return null;

        lookup.Category = NormalizeCategory(request.Category);
        lookup.Name = request.Name.Trim();
        lookup.IsActive = request.IsActive;
        lookup.SortOrder = request.SortOrder;
        lookup.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(lookup);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var lookup = await _db.AttendanceLookups.FindAsync(id);
        if (lookup == null) return false;
        _db.AttendanceLookups.Remove(lookup);
        await _db.SaveChangesAsync();
        return true;
    }
}
