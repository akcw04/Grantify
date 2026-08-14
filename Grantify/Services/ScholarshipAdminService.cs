using Grantify.Data;
using Grantify.Models;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Services;

// OWNER: Member C (Admin role). Scholarship CRUD for the admin pages.
public class ScholarshipAdminService
{
    private readonly AppDbContext _db;
    private readonly ScholarshipService _publicCatalogue;

    public ScholarshipAdminService(AppDbContext db, ScholarshipService publicCatalogue)
    {
        _db = db;
        // The public landing page caches its scholarship list (see
        // ScholarshipService). Creating or editing a listing here must clear
        // that cache, or the public page keeps showing the old data for up to
        // a minute after the admin pressed Save.
        _publicCatalogue = publicCatalogue;
    }

    public async Task<List<Scholarship>> GetAllAsync()
    {
        return await _db.Scholarships
            .Include(s => s.Institution)
            .Include(s => s.Category)
            .Include(s => s.IntakePeriod)
            .OrderByDescending(s => s.Deadline)
            .ToListAsync();
    }

    public async Task<Scholarship?> GetByIdAsync(int id)
    {
        return await _db.Scholarships
            .Include(s => s.Institution)
            .Include(s => s.Category)
            .Include(s => s.IntakePeriod)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Scholarship> CreateAsync(Scholarship scholarship)
    {
        _db.Scholarships.Add(scholarship);
        await _db.SaveChangesAsync();

        await _publicCatalogue.InvalidateLandingCacheAsync();
        return scholarship;
    }

    public async Task<bool> UpdateAsync(Scholarship updated)
    {
        var existing = await _db.Scholarships.FindAsync(updated.Id);
        if (existing is null)
        {
            return false;
        }

        existing.Name = updated.Name;
        existing.Provider = updated.Provider;
        existing.Description = updated.Description;
        existing.MinimumCgpa = updated.MinimumCgpa;
        existing.MaximumHouseholdIncome = updated.MaximumHouseholdIncome;
        existing.Deadline = updated.Deadline;
        existing.IsPublished = updated.IsPublished;
        existing.InstitutionId = updated.InstitutionId;
        existing.ScholarshipCategoryId = updated.ScholarshipCategoryId;
        existing.IntakePeriodId = updated.IntakePeriodId;

        await _db.SaveChangesAsync();

        await _publicCatalogue.InvalidateLandingCacheAsync();
        return true;
    }

    public Task<List<Institution>> GetInstitutionOptionsAsync()
    {
        return _db.Institutions.OrderBy(i => i.Name).ToListAsync();
    }

    public Task<List<ScholarshipCategory>> GetCategoryOptionsAsync()
    {
        return _db.ScholarshipCategories.OrderBy(c => c.Name).ToListAsync();
    }

    public Task<List<IntakePeriod>> GetIntakePeriodOptionsAsync()
    {
        return _db.IntakePeriods.OrderBy(p => p.StartDate).ToListAsync();
    }
}
