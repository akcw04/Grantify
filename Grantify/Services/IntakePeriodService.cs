using Grantify.Data;
using Grantify.Models;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Services;

// Manages the IntakePeriod master data table (Admin only).
public class IntakePeriodService
{
    private readonly AppDbContext _db;

    public IntakePeriodService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<IntakePeriod>> GetAllAsync()
    {
        return await _db.IntakePeriods.OrderBy(p => p.StartDate).ToListAsync();
    }

    public async Task<IntakePeriod?> GetByIdAsync(int id)
    {
        return await _db.IntakePeriods.FindAsync(id);
    }

    public async Task CreateAsync(IntakePeriod period)
    {
        _db.IntakePeriods.Add(period);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(int id, string periodName, DateTime startDate, DateTime endDate)
    {
        var existing = await _db.IntakePeriods.FindAsync(id);
        if (existing is null)
        {
            return false;
        }

        existing.PeriodName = periodName;
        existing.StartDate = startDate;
        existing.EndDate = endDate;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _db.IntakePeriods.FindAsync(id);
        if (existing is null)
        {
            return false;
        }

        _db.IntakePeriods.Remove(existing);
        await _db.SaveChangesAsync();
        return true;
    }
}
