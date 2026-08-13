using Grantify.Data;
using Grantify.Models;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Services;

// Manages the ScholarshipCategory master data table (Admin only).
public class ScholarshipCategoryService
{
    private readonly AppDbContext _db;

    public ScholarshipCategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ScholarshipCategory>> GetAllAsync()
    {
        return await _db.ScholarshipCategories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<ScholarshipCategory?> GetByIdAsync(int id)
    {
        return await _db.ScholarshipCategories.FindAsync(id);
    }

    public async Task CreateAsync(ScholarshipCategory category)
    {
        _db.ScholarshipCategories.Add(category);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(int id, string name, string description)
    {
        var existing = await _db.ScholarshipCategories.FindAsync(id);
        if (existing is null)
        {
            return false;
        }

        existing.Name = name;
        existing.Description = description;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _db.ScholarshipCategories.FindAsync(id);
        if (existing is null)
        {
            return false;
        }

        _db.ScholarshipCategories.Remove(existing);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<bool> IsInUseAsync(int id)
    {
        return _db.Scholarships.AnyAsync(s => s.ScholarshipCategoryId == id);
    }
}
