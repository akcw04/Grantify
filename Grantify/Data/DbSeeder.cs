using Grantify.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Data;

// Fills an empty database with starting data so everyone on the team
// sees the same roles, test accounts and sample scholarships.
// Runs automatically every time the app starts (see Program.cs).
// It only adds things that are missing — it never doubles the data.
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();

        // First, bring the database up to date with the latest migrations.
        // This means teammates do not need to run Update-Database by hand.
        await db.Database.MigrateAsync();

        await CreateRolesAsync(services);
        await CreateTestAccountsAsync(services, db);
        await CreateSampleScholarshipsAsync(db);
    }

    // The three roles of our system. A user's role decides which pages they can open.
    private static async Task CreateRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = { "Student", "Officer", "Admin" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    // Test accounts for development only. NEVER use these in the real deployed app.
    // Password for all of them: Grantify@123
    private static async Task CreateTestAccountsAsync(IServiceProvider services, AppDbContext db)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var student = await CreateUserAsync(userManager, "student@grantify.test", "Test Student", "Student");
        await CreateUserAsync(userManager, "officer@grantify.test", "Test Officer", "Officer");
        await CreateUserAsync(userManager, "admin@grantify.test", "Test Admin", "Admin");

        // Give the test student a profile so the eligibility check has numbers to work with.
        if (student is not null && !await db.StudentProfiles.AnyAsync(p => p.UserId == student.Id))
        {
            db.StudentProfiles.Add(new StudentProfile
            {
                UserId = student.Id,
                Cgpa = 3.50m,
                HouseholdIncome = 4000m,
                Course = "Software Engineering",
                Institution = "Asia Pacific University"
            });
            await db.SaveChangesAsync();
        }
    }

    // Makes one user with the given role, or returns the existing one if already made.
    private static async Task<ApplicationUser?> CreateUserAsync(
        UserManager<ApplicationUser> userManager, string email, string fullName, string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, "Grantify@123");
        if (!result.Succeeded)
        {
            return null;
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    // A few sample scholarships so list pages are not empty during development.
    private static async Task CreateSampleScholarshipsAsync(AppDbContext db)
    {
        if (await db.Scholarships.AnyAsync())
        {
            return; // already seeded
        }

        db.Scholarships.AddRange(
            new Scholarship
            {
                Name = "APU Merit Scholarship",
                Provider = "Asia Pacific University",
                Description = "For students with strong academic results.",
                MinimumCgpa = 3.50m,
                MaximumHouseholdIncome = null, // merit-based, income does not matter
                Deadline = DateTime.Today.AddDays(60),
                IsPublished = true
            },
            new Scholarship
            {
                Name = "B40 Financial Aid Grant",
                Provider = "Ministry of Higher Education",
                Description = "Support for students from lower-income households.",
                MinimumCgpa = 2.50m,
                MaximumHouseholdIncome = 4850m,
                Deadline = DateTime.Today.AddDays(45),
                IsPublished = true
            },
            new Scholarship
            {
                Name = "Tech Talent Bursary",
                Provider = "TechCorp Malaysia",
                Description = "For IT and engineering students. Draft — not visible to students yet.",
                MinimumCgpa = 3.00m,
                MaximumHouseholdIncome = 8000m,
                Deadline = DateTime.Today.AddDays(90),
                IsPublished = false // stays hidden until an admin publishes it
            });

        await db.SaveChangesAsync();
    }
}
