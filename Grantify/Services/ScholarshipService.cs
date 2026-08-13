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

    // The scholarships a VISITOR sees on the public home page: published, and
    // still open. Closest deadline first, so the most urgent is at the top.
    //
    // The deadline test matters. Without it the page listed scholarships whose
    // closing date had already gone by, under a heading that says "Open
    // scholarships" — and a visitor who registered because of one of them would
    // find they could not apply. A student browsing inside the site sees closed
    // ones marked as such (see StudentService.GetCatalogueAsync); out here,
    // where nothing can be marked, they simply do not belong.
    public async Task<List<Scholarship>> GetPublishedAsync()
    {
        var today = DateTime.Today;

        return await _db.Scholarships
            .Where(s => s.IsPublished && s.Deadline >= today)
            .OrderBy(s => s.Deadline)
            .ToListAsync();
    }

    // The three numbers shown across the top of the public home page.
    //
    // They are counted from the real database rather than typed into the page.
    // A landing page that claims figures the system cannot back up is the kind
    // of thing a marker checks, and it would be wrong the moment an admin
    // published anything.
    public async Task<PublicStats> GetPublicStatsAsync()
    {
        var today = DateTime.Today;

        return new PublicStats
        {
            OpenScholarships = await _db.Scholarships
                .CountAsync(s => s.IsPublished && s.Deadline >= today),

            // Distinct providers, so listing five scholarships from one
            // university does not read as five partners.
            Providers = await _db.Scholarships
                .Where(s => s.IsPublished)
                .Select(s => s.Provider)
                .Distinct()
                .CountAsync(),

            AwardsMade = await _db.ScholarshipApplications
                .CountAsync(a => a.Status == ApplicationStatus.Approved)
        };
    }
}

// The headline figures on the public home page.
public class PublicStats
{
    public int OpenScholarships { get; set; }
    public int Providers { get; set; }
    public int AwardsMade { get; set; }
}
