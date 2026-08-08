using Grantify.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Data;

// The one door to our database. Every table is listed here as a DbSet.
// It also inherits all the login tables (AspNetUsers, AspNetRoles...) from Identity.
// SHARED FILE: announce in the group chat before changing anything here.
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Each line below = one table in the database.
    public DbSet<Scholarship> Scholarships => Set<Scholarship>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<ScholarshipApplication> ScholarshipApplications => Set<ScholarshipApplication>();
    public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();

    // Audit trail of officer actions (added by Member B).
    public DbSet<ApplicationReviewLog> ApplicationReviewLogs => Set<ApplicationReviewLog>();

    // Messages shown to a student when their application changes (added by Member A).
    public DbSet<Notification> Notifications => Set<Notification>();
}
