using Grantify.Data;
using Grantify.Models;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Services;

// Manages the Institution master data table (Admin only).
public class InstitutionService
{
    private readonly AppDbContext _db;

    public InstitutionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Institution>> GetAllAsync()
    {
        return await _db.Institutions.OrderBy(i => i.Name).ToListAsync();
    }

    public async Task<Institution?> GetByIdAsync(int id)
    {
        return await _db.Institutions.FindAsync(id);
    }

    public async Task CreateAsync(Institution institution)
    {
        _db.Institutions.Add(institution);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(int id, string name, string location, string contactEmail)
    {
        var existing = await _db.Institutions.FindAsync(id);
        if (existing is null)
        {
            return false;
        }

        existing.Name = name;
        existing.Location = location;
        existing.ContactEmail = contactEmail;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _db.Institutions.FindAsync(id);
        if (existing is null)
        {
            return false;
        }

        _db.Institutions.Remove(existing);
        await _db.SaveChangesAsync();
        return true;
    }
}
