using Grantify.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Data;

// Demonstration data: scholarships, students and applications in every state
// the system knows, so all three role dashboards have something real to show.
// (This file replaced OfficerDemoSeeder, which only fed the officer queue.)
//
// WHERE IT RUNS
//   Our own machines  - always (DbSeeder passes the shared dev password).
//   The deployed site - only when the environment properties say so:
//                         Seed__DemoData     = true
//                         Seed__DemoPassword = <password for the demo accounts>
//   Without those two the deployed database stays empty, which is the safe
//   default. The password comes from configuration for the same reason the
//   admin one does: nothing that can sign in to the live site may have its
//   password written in this repository.
//
// WHY THE DEPLOYED SITE NEEDS IT AT ALL
//   A fresh RDS database has accounts and nothing else. The video has to show
//   an officer queue with work in it, decided applications, a shortlist, a
//   scholarship that is closing and one that has closed — building all of that
//   by hand through the UI at the start of every lab session wastes the
//   session. One property gives the same starting point every time.
//
// SAFETY
//   Every record is checked before it is added (matched by name, email, or
//   scholarship+student pair), so running this on every startup never
//   duplicates anything and never touches data a real user created.
//
//   The demo document rows point at files that do not exist ("demo-uploads/"),
//   so opening one shows the officer page's "file not available" message.
//   That is expected — upload a real file live when demonstrating documents.
public static class DemoDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, string demoPassword)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // ----- Scholarships: one of every kind the UI can show -----

        var merit = await GetOrCreateScholarshipAsync(db, new Scholarship
        {
            Name = "APU Merit Scholarship",
            Provider = "Asia Pacific University",
            Description = "For students with strong academic results.",
            MinimumCgpa = 3.50m,
            MaximumHouseholdIncome = null,          // merit-based, income does not matter
            Deadline = DateTime.Today.AddDays(57),
            IsPublished = true
        });

        var financialAid = await GetOrCreateScholarshipAsync(db, new Scholarship
        {
            Name = "B40 Financial Aid Grant",
            Provider = "Ministry of Higher Education",
            Description = "Support for students from lower-income households.",
            MinimumCgpa = 2.50m,
            MaximumHouseholdIncome = 4850m,
            Deadline = DateTime.Today.AddDays(42),
            IsPublished = true
        });

        // Closes inside a fortnight, so the red "closes in N days" flag shows.
        var stem = await GetOrCreateScholarshipAsync(db, new Scholarship
        {
            Name = "STEM Excellence Award",
            Provider = "TechCorp Malaysia",
            Description = "For high-achieving students in science, technology, engineering and mathematics.",
            MinimumCgpa = 3.30m,
            MaximumHouseholdIncome = null,
            Deadline = DateTime.Today.AddDays(10),
            IsPublished = true
        });

        // A draft: students must NOT see this one, which is itself a demo point.
        await GetOrCreateScholarshipAsync(db, new Scholarship
        {
            Name = "Tech Talent Bursary",
            Provider = "TechCorp Malaysia",
            Description = "For IT and engineering students. Draft — not visible to students yet.",
            MinimumCgpa = 3.00m,
            MaximumHouseholdIncome = 8000m,
            Deadline = DateTime.Today.AddDays(90),
            IsPublished = false
        });

        // Already closed. Proves the deadline rule, and that an officer can
        // still fairly review somebody who applied before it closed.
        var closed = await GetOrCreateScholarshipAsync(db, new Scholarship
        {
            Name = "Legacy Excellence Award (closed)",
            Provider = "Grantify Alumni Trust",
            Description = "Applications have closed. Kept so the deadline rule can be demonstrated.",
            MinimumCgpa = 3.00m,
            MaximumHouseholdIncome = null,
            Deadline = DateTime.Today.AddDays(-10),
            IsPublished = true
        });

        // ----- Demo students, each designed to show a different case -----

        // Passes both rules comfortably.
        var aisyah = await CreateDemoStudentAsync(userManager, db, demoPassword,
            "demo.aisyah@grantify.test", "Aisyah Binti Rahman",
            cgpa: 3.85m, income: 3200m, course: "Computer Science");

        // Good CGPA but income is over the aid limit -> fails screening.
        var kumar = await CreateDemoStudentAsync(userManager, db, demoPassword,
            "demo.kumar@grantify.test", "Kumar A/L Ravindran",
            cgpa: 3.60m, income: 9500m, course: "Software Engineering");

        // CGPA below the merit minimum -> also fails screening.
        var chen = await CreateDemoStudentAsync(userManager, db, demoPassword,
            "demo.chen@grantify.test", "Chen Wei Ling",
            cgpa: 2.90m, income: 2100m, course: "Information Technology");

        // Passes, documents already checked -> the full approve path is open.
        var daniel = await CreateDemoStudentAsync(userManager, db, demoPassword,
            "demo.daniel@grantify.test", "Daniel Tan Wei Jie",
            cgpa: 3.70m, income: 4200m, course: "Data Engineering");

        // Deliberately has NO profile row: proves the review page copes with a
        // student who never filled in their details.
        var noProfile = await CreateDemoUserAsync(userManager, demoPassword,
            "demo.noprofile@grantify.test", "Nurul Huda (no profile)");

        // Applied in good time to the scholarship that has since closed.
        var farah = await CreateDemoStudentAsync(userManager, db, demoPassword,
            "demo.farah@grantify.test", "Farah Iskandar",
            cgpa: 3.90m, income: 2800m, course: "Actuarial Studies");

        if (aisyah is null || kumar is null || chen is null || daniel is null
            || noProfile is null || farah is null)
        {
            return; // an account could not be created; leave the rest untouched
        }

        // ----- Applications: one in every status -----

        // Waiting in the queue (Submitted).
        await AddApplicationAsync(db, merit, aisyah, ApplicationStatus.Submitted, daysAgo: 9,
            documents: new[] { "aisyah-transcript.pdf", "aisyah-ic-copy.pdf" });

        await AddApplicationAsync(db, financialAid, kumar, ApplicationStatus.Submitted, daysAgo: 7,
            documents: new[] { "kumar-transcript.pdf", "kumar-payslip.pdf" });

        await AddApplicationAsync(db, merit, noProfile, ApplicationStatus.Submitted, daysAgo: 2,
            documents: Array.Empty<string>());

        // Submitted 20 days ago; the scholarship closed 10 days ago. It WAS
        // open when she applied, so the officer must still see "Eligible".
        await AddApplicationAsync(db, closed, farah, ApplicationStatus.Submitted, daysAgo: 20,
            documents: new[] { "farah-transcript.pdf" });

        // Part-way through review.
        await AddApplicationAsync(db, merit, chen, ApplicationStatus.UnderReview, daysAgo: 12,
            documents: new[] { "chen-transcript.pdf" });

        await AddApplicationAsync(db, financialAid, daniel, ApplicationStatus.UnderReview, daysAgo: 5,
            documents: new[] { "daniel-transcript.pdf", "daniel-income-letter.pdf" },
            documentsVerified: true);

        // Shortlisted, scored, waiting on the final decision.
        await AddApplicationAsync(db, stem, aisyah, ApplicationStatus.Shortlisted, daysAgo: 6,
            documents: new[] { "aisyah-transcript.pdf" },
            documentsVerified: true, score: 82);

        // Already decided — one approval, one rejection with its reason. These
        // give the student dashboards history and the analytics page numbers.
        await AddApplicationAsync(db, financialAid, farah, ApplicationStatus.Approved, daysAgo: 10,
            documents: new[] { "farah-income-statement.pdf" },
            documentsVerified: true, score: 90,
            remarks: "Excellent academic record and complete documentation.");

        await AddApplicationAsync(db, financialAid, chen, ApplicationStatus.Rejected, daysAgo: 15,
            documents: new[] { "chen-payslip.pdf" },
            documentsVerified: true, score: 40,
            remarks: "Household income documentation did not support the declared amount.");
    }

    // Finds a scholarship by name, or creates it. Matching by name is what
    // makes the seeder safe to run on a database that already has data.
    private static async Task<Scholarship> GetOrCreateScholarshipAsync(AppDbContext db, Scholarship scholarship)
    {
        var existing = await db.Scholarships.FirstOrDefaultAsync(s => s.Name == scholarship.Name);
        if (existing is not null)
        {
            return existing;
        }

        db.Scholarships.Add(scholarship);
        await db.SaveChangesAsync();
        return scholarship;
    }

    // One application with its documents — skipped entirely if this student
    // already has an application for this scholarship (from an earlier run, or
    // made live during a demonstration).
    private static async Task AddApplicationAsync(
        AppDbContext db,
        Scholarship scholarship,
        ApplicationUser student,
        ApplicationStatus status,
        int daysAgo,
        string[] documents,
        bool documentsVerified = false,
        int? score = null,
        string? remarks = null)
    {
        var exists = await db.ScholarshipApplications
            .AnyAsync(a => a.ScholarshipId == scholarship.Id && a.StudentUserId == student.Id);
        if (exists)
        {
            return;
        }

        var submittedOn = DateTime.UtcNow.AddDays(-daysAgo);
        var reviewed = status != ApplicationStatus.Submitted;

        var application = new ScholarshipApplication
        {
            ScholarshipId = scholarship.Id,
            StudentUserId = student.Id,
            Status = status,
            SubmittedOn = submittedOn,
            Score = score,
            OfficerRemarks = remarks,
            // Anything past Submitted has been touched by an officer, so the
            // "handled by" column has a name in it.
            ReviewedByName = reviewed ? "Demo Officer" : null,
            ReviewedOn = reviewed ? submittedOn.AddDays(1) : null
        };

        foreach (var fileName in documents)
        {
            application.Documents.Add(new ApplicationDocument
            {
                FileName = fileName,
                // No real file exists behind these — see the note at the top.
                StoragePath = $"demo-uploads/{fileName}",
                Status = documentsVerified ? DocumentStatus.Verified : DocumentStatus.Pending,
                UploadedOn = submittedOn,
                VerifiedByName = documentsVerified ? "Demo Officer" : null,
                VerifiedOn = documentsVerified ? submittedOn.AddDays(1) : null
            });
        }

        // Decided and shortlisted applications get their history line, so the
        // review page shows an audit trail and the student gets a notification
        // (StudentService turns student-visible log entries into messages).
        if (reviewed)
        {
            application.ReviewLogs.Add(new ApplicationReviewLog
            {
                OfficerUserId = "demo-officer",
                OfficerName = "Demo Officer",
                Action = status == ApplicationStatus.UnderReview ? "Started review" : $"Decision: {status}",
                Details = remarks,
                FromStatus = ApplicationStatus.Submitted,
                ToStatus = status,
                IsStudentVisible = true,
                CreatedOn = submittedOn.AddDays(1)
            });
        }

        db.ScholarshipApplications.Add(application);
        await db.SaveChangesAsync();
    }

    // Creates a demo login plus the profile the eligibility check reads.
    private static async Task<ApplicationUser?> CreateDemoStudentAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        string demoPassword,
        string email,
        string fullName,
        decimal cgpa,
        decimal income,
        string course)
    {
        var user = await CreateDemoUserAsync(userManager, demoPassword, email, fullName);
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

    // Creates one Student-role login, or returns the existing one. The "demo."
    // email prefix keeps these easy to spot and easy to delete.
    private static async Task<ApplicationUser?> CreateDemoUserAsync(
        UserManager<ApplicationUser> userManager, string demoPassword, string email, string fullName)
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

        var result = await userManager.CreateAsync(user, demoPassword);
        if (!result.Succeeded)
        {
            return null;
        }

        await userManager.AddToRoleAsync(user, "Student");
        return user;
    }
}
