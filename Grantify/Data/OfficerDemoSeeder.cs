using Grantify.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Data;

// OWNER: Member B (Officer role). TEMPORARY — delete this file before hand-in.
//
// Why it exists: an officer reviews applications that students send in, but the
// Student pages (Member A) are not built yet, so the database has no
// applications and the review queue would be empty. This file creates a handful
// of fake students and applications purely so the Officer feature can be built
// and demonstrated on its own.
//
// Safety: it only runs in Development (see Program.cs) and it only runs when
// there are no applications at all, so it can never touch real data or add
// duplicates. Every account it creates uses a "demo." email prefix so they are
// easy to spot and remove.
//
// WHEN MEMBER A IS DONE: stop calling this from Program.cs, delete this file,
// and delete the demo.* accounts from the database.
public static class OfficerDemoSeeder
{
    private const string DemoPassword = "Grantify@123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Already have applications (real or demo)? Then do nothing.
        if (await db.ScholarshipApplications.AnyAsync())
        {
            return;
        }

        // We need scholarships to apply for. DbSeeder makes these first.
        var scholarships = await db.Scholarships.OrderBy(s => s.Id).ToListAsync();
        if (scholarships.Count == 0)
        {
            return;
        }

        var merit = scholarships[0];                                    // high CGPA, no income limit
        var financialAid = scholarships.Count > 1 ? scholarships[1] : merit; // income capped

        // A scholarship whose closing date has already gone by. Without one of
        // these, the deadline rule in EligibilityService cannot be tested by
        // hand — every seeded scholarship closes months from now.
        var closed = await GetOrCreateClosedScholarshipAsync(db);

        // ----- Fake students, each one designed to test a different case -----

        // Passes both rules comfortably.
        var aisyah = await CreateDemoStudentAsync(userManager, db,
            "demo.aisyah@grantify.test", "Aisyah Binti Rahman",
            cgpa: 3.85m, income: 3200m, course: "Computer Science");

        // Good CGPA but household income is over the aid limit -> should FAIL
        // the automatic screening, so the officer can see a "not eligible" case.
        var kumar = await CreateDemoStudentAsync(userManager, db,
            "demo.kumar@grantify.test", "Kumar A/L Ravindran",
            cgpa: 3.60m, income: 9500m, course: "Software Engineering");

        // CGPA below the merit minimum -> also fails screening.
        var chen = await CreateDemoStudentAsync(userManager, db,
            "demo.chen@grantify.test", "Chen Wei Ling",
            cgpa: 2.90m, income: 2100m, course: "Information Technology");

        // Passes, and their documents are already checked, so the officer can
        // walk the full approve path without verifying anything first.
        var daniel = await CreateDemoStudentAsync(userManager, db,
            "demo.daniel@grantify.test", "Daniel Tan Wei Jie",
            cgpa: 3.70m, income: 4200m, course: "Data Engineering");

        // Deliberately has NO profile row, to prove the review page handles a
        // student who never filled in their details.
        var incomplete = await CreateDemoUserAsync(userManager,
            "demo.noprofile@grantify.test", "Nurul Huda (no profile)");

        // Applied in good time to a scholarship that has since closed. Proves
        // the officer still sees "Eligible": the closing date is judged against
        // the day the student submitted, not the day the officer got round to it.
        var punctual = await CreateDemoStudentAsync(userManager, db,
            "demo.farah@grantify.test", "Farah Iskandar",
            cgpa: 3.90m, income: 2800m, course: "Actuarial Studies");

        if (aisyah is null || kumar is null || chen is null || daniel is null
            || incomplete is null || punctual is null)
        {
            return; // account creation failed; leave the database untouched
        }

        // ----- Fake applications -----

        var applications = new List<ScholarshipApplication>
        {
            NewApplication(merit, aisyah, ApplicationStatus.Submitted, daysAgo: 9,
                documents: new[] { "aisyah-transcript.pdf", "aisyah-ic-copy.pdf" }),

            NewApplication(financialAid, kumar, ApplicationStatus.Submitted, daysAgo: 7,
                documents: new[] { "kumar-transcript.pdf", "kumar-payslip.pdf" }),

            NewApplication(merit, chen, ApplicationStatus.UnderReview, daysAgo: 12,
                documents: new[] { "chen-transcript.pdf" }),

            NewApplication(financialAid, daniel, ApplicationStatus.UnderReview, daysAgo: 5,
                documents: new[] { "daniel-transcript.pdf", "daniel-income-letter.pdf" },
                documentsVerified: true),

            NewApplication(merit, incomplete, ApplicationStatus.Submitted, daysAgo: 2,
                documents: Array.Empty<string>()),

            // Submitted 20 days ago; that scholarship closed 10 days ago. So it
            // WAS open when she applied. The officer must still see "Eligible".
            NewApplication(closed, punctual, ApplicationStatus.Submitted, daysAgo: 20,
                documents: new[] { "farah-transcript.pdf" })
        };

        db.ScholarshipApplications.AddRange(applications);
        await db.SaveChangesAsync();
    }

    // A published scholarship whose closing date has already passed.
    // Normally an Admin would create this; there is no Admin UI yet, and this
    // whole file is temporary demo data anyway. Created once, then reused.
    private static async Task<Scholarship> GetOrCreateClosedScholarshipAsync(AppDbContext db)
    {
        const string name = "Legacy Excellence Award (closed)";

        var existing = await db.Scholarships.FirstOrDefaultAsync(s => s.Name == name);
        if (existing is not null)
        {
            return existing;
        }

        var closed = new Scholarship
        {
            Name = name,
            Provider = "Grantify Alumni Trust",
            Description = "Applications have closed. Kept so the deadline rule can be demonstrated.",
            MinimumCgpa = 3.00m,
            MaximumHouseholdIncome = null,
            Deadline = DateTime.Today.AddDays(-10),
            IsPublished = true
        };

        db.Scholarships.Add(closed);
        await db.SaveChangesAsync();
        return closed;
    }

    // Builds one application together with its uploaded files.
    private static ScholarshipApplication NewApplication(
        Scholarship scholarship,
        ApplicationUser student,
        ApplicationStatus status,
        int daysAgo,
        string[] documents,
        bool documentsVerified = false)
    {
        var submittedOn = DateTime.UtcNow.AddDays(-daysAgo);

        var application = new ScholarshipApplication
        {
            ScholarshipId = scholarship.Id,
            StudentUserId = student.Id,
            Status = status,
            SubmittedOn = submittedOn
        };

        foreach (var fileName in documents)
        {
            application.Documents.Add(new ApplicationDocument
            {
                FileName = fileName,
                // No real file exists — student upload is Member A's job, and in
                // Task 2 this value becomes the Amazon S3 object key.
                StoragePath = $"demo-uploads/{fileName}",
                Status = documentsVerified ? DocumentStatus.Verified : DocumentStatus.Pending,
                UploadedOn = submittedOn
            });
        }

        return application;
    }

    // Creates a demo login account plus the profile the eligibility check reads.
    private static async Task<ApplicationUser?> CreateDemoStudentAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        string email,
        string fullName,
        decimal cgpa,
        decimal income,
        string course)
    {
        var user = await CreateDemoUserAsync(userManager, email, fullName);
        if (user is null)
        {
            return null;
        }

        if (!await db.StudentProfiles.AnyAsync(p => p.UserId == user.Id))
        {
            db.StudentProfiles.Add(new StudentProfile
            {
                UserId = user.Id,
                Cgpa = cgpa,
                HouseholdIncome = income,
                Course = course,
                Institution = "Asia Pacific University"
            });
            await db.SaveChangesAsync();
        }

        return user;
    }

    // Creates a login account in the Student role, or returns the existing one.
    private static async Task<ApplicationUser?> CreateDemoUserAsync(
        UserManager<ApplicationUser> userManager, string email, string fullName)
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

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
        {
            return null;
        }

        await userManager.AddToRoleAsync(user, "Student");
        return user;
    }
}
