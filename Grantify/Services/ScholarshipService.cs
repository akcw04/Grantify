using Grantify.Data;
using Grantify.Models;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Services;

// EXAMPLE SERVICE — copy this pattern for your own features.
//
// A "service" is a backend class that holds the real logic and talks to the database.
// Pages should stay thin: they call a service method and show the result.
// Rule of thumb: if it is more than a few lines of logic, it belongs in a service.
//
// Every new service must also be registered in Program.cs (one line, see there).
public class ScholarshipService
{
    private readonly AppDbContext _db;

    // The database context is handed to us automatically (dependency injection).
    public ScholarshipService(AppDbContext db)
    {
        _db = db;
    }

    // Returns all scholarships students are allowed to see,
    // closest deadline first.
    public async Task<List<Scholarship>> GetPublishedAsync()
    {
        return await _db.Scholarships
            .Where(s => s.IsPublished)
            .OrderBy(s => s.Deadline)
            .ToListAsync();
    }
}
