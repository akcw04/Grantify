using Grantify.Data;
using Grantify.Models;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Services;

// OWNER: Member A (Student role).
// Backend logic for the student pages, so the pages stay thin.
//
// GetOrCreateProfileAsync / SaveProfileAsync   - #1 profile
// GetCatalogueAsync                            - #2 browse + eligibility
// ApplyAsync / SaveDocumentAsync               - #3 apply + upload
// GetMyApplicationsAsync / GetApplicationAsync - #4 track
public class StudentService
{
    private readonly AppDbContext _db;
    private readonly EligibilityService _eligibility;
    private readonly IWebHostEnvironment _environment;

    public StudentService(AppDbContext db, EligibilityService eligibility, IWebHostEnvironment environment)
    {
        _db = db;
        _eligibility = eligibility;
        _environment = environment;
    }

    // ---------- #1 Profile ----------

    // Every student gets a profile row the first time they open the page, so the
    // eligibility check always has something to read instead of crashing on null.
    public async Task<StudentProfile> GetOrCreateProfileAsync(string userId)
    {
        var profile = await _db.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile is not null) return profile;

        profile = new StudentProfile { UserId = userId };
        _db.StudentProfiles.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    public async Task SaveProfileAsync(string userId, decimal cgpa, decimal householdIncome,
                                       string course, string institution)
    {
        var profile = await GetOrCreateProfileAsync(userId);

        profile.Cgpa = cgpa;
        profile.HouseholdIncome = householdIncome;
        profile.Course = course;
        profile.Institution = institution;

        await _db.SaveChangesAsync();
    }

    // A profile is only useful once the numbers the rules read are filled in.
    public static bool IsProfileComplete(StudentProfile profile)
    {
        return profile.Cgpa > 0
            && !string.IsNullOrWhiteSpace(profile.Course)
            && !string.IsNullOrWhiteSpace(profile.Institution);
    }

    // ---------- #2 Browse and eligibility ----------

    // One scholarship as the student sees it: the listing, whether they qualify,
    // whether it is still open, and whether they already applied.
    public class CatalogueItem
    {
        public Scholarship Scholarship { get; set; } = null!;
        public EligibilityResult Eligibility { get; set; } = new();
        public bool IsOpen { get; set; }
        public int DaysLeft { get; set; }
        public ScholarshipApplication? MyApplication { get; set; }
    }

    // Published scholarships, each screened against this student's profile.
    // "search" filters on name or provider; pass null for everything.
    public async Task<List<CatalogueItem>> GetCatalogueAsync(string userId, string? search = null)
    {
        var profile = await GetOrCreateProfileAsync(userId);

        var query = _db.Scholarships.Where(s => s.IsPublished);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Name.Contains(term) || s.Provider.Contains(term));
        }

        var scholarships = await query.OrderBy(s => s.Deadline).ToListAsync();

        // One query for every application this student already has, so the loop
        // below does not hit the database once per scholarship.
        var myApplications = await _db.ScholarshipApplications
            .Where(a => a.StudentUserId == userId)
            .ToListAsync();

        var today = DateTime.Today;
        var items = new List<CatalogueItem>();

        foreach (var scholarship in scholarships)
        {
            var daysLeft = (scholarship.Deadline.Date - today).Days;

            items.Add(new CatalogueItem
            {
                Scholarship = scholarship,
                Eligibility = _eligibility.Check(profile, scholarship),
                IsOpen = daysLeft >= 0,
                DaysLeft = daysLeft,
                MyApplication = myApplications.FirstOrDefault(a => a.ScholarshipId == scholarship.Id)
            });
        }

        return items;
    }

    public async Task<CatalogueItem?> GetScholarshipAsync(string userId, int scholarshipId)
    {
        var scholarship = await _db.Scholarships
            .FirstOrDefaultAsync(s => s.Id == scholarshipId && s.IsPublished);

        if (scholarship is null) return null;

        var profile = await GetOrCreateProfileAsync(userId);
        var daysLeft = (scholarship.Deadline.Date - DateTime.Today).Days;

        return new CatalogueItem
        {
            Scholarship = scholarship,
            Eligibility = _eligibility.Check(profile, scholarship),
            IsOpen = daysLeft >= 0,
            DaysLeft = daysLeft,
            MyApplication = await _db.ScholarshipApplications
                .FirstOrDefaultAsync(a => a.ScholarshipId == scholarshipId && a.StudentUserId == userId)
        };
    }

    // ---------- #3 Apply ----------

    // What happened when the student pressed Apply.
    public record ApplyResult(bool Success, string Message, int ApplicationId = 0);

    public async Task<ApplyResult> ApplyAsync(string userId, int scholarshipId)
    {
        var scholarship = await _db.Scholarships
            .FirstOrDefaultAsync(s => s.Id == scholarshipId && s.IsPublished);

        if (scholarship is null)
            return new ApplyResult(false, "That scholarship is not available.");

        // Checked again here even though the page hides the button: the browser
        // is never trusted on its own.
        if (scholarship.Deadline.Date < DateTime.Today)
            return new ApplyResult(false, $"Applications closed on {scholarship.Deadline:dd MMM yyyy}.");

        var already = await _db.ScholarshipApplications
            .FirstOrDefaultAsync(a => a.ScholarshipId == scholarshipId && a.StudentUserId == userId);

        if (already is not null)
            return new ApplyResult(false, "You have already applied for this scholarship.", already.Id);

        var profile = await GetOrCreateProfileAsync(userId);
        if (!IsProfileComplete(profile))
            return new ApplyResult(false, "Complete your profile before applying.");

        var application = new ScholarshipApplication
        {
            ScholarshipId = scholarshipId,
            StudentUserId = userId,
            Status = ApplicationStatus.Submitted,
            SubmittedOn = DateTime.Now
        };

        _db.ScholarshipApplications.Add(application);
        await _db.SaveChangesAsync();

        return new ApplyResult(true, "Application submitted.", application.Id);
    }

    // ---------- #3 Supporting documents ----------

    public static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    // Files are stored outside wwwroot so nobody can reach them by guessing a
    // URL. Downloads go through the page handler below, which checks ownership.
    // Task 2: StoragePath becomes the Amazon S3 object key instead.
    public async Task<ApplyResult> SaveDocumentAsync(string userId, int applicationId, IFormFile file)
    {
        var application = await _db.ScholarshipApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.StudentUserId == userId);

        if (application is null)
            return new ApplyResult(false, "Application not found.");

        if (file is null || file.Length == 0)
            return new ApplyResult(false, "Choose a file first.");

        if (file.Length > MaxFileSizeBytes)
            return new ApplyResult(false, "That file is larger than 5 MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return new ApplyResult(false, "Only PDF, JPG and PNG files are accepted.");

        var folder = Path.Combine(_environment.ContentRootPath, "App_Data", "uploads", applicationId.ToString());
        Directory.CreateDirectory(folder);

        // A new name per upload, so two files called "transcript.pdf" cannot
        // overwrite each other.
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, storedName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _db.ApplicationDocuments.Add(new ApplicationDocument
        {
            ScholarshipApplicationId = applicationId,
            FileName = Path.GetFileName(file.FileName),
            StoragePath = Path.Combine(applicationId.ToString(), storedName),
            Status = DocumentStatus.Pending,
            UploadedOn = DateTime.Now
        });

        await _db.SaveChangesAsync();
        return new ApplyResult(true, "Document uploaded.");
    }

    // Returns the file only if this student owns the application it belongs to.
    public async Task<(string FullPath, string FileName)?> GetMyDocumentAsync(string userId, int documentId)
    {
        var document = await _db.ApplicationDocuments
            .Include(d => d.Application)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document?.Application is null) return null;
        if (document.Application.StudentUserId != userId) return null;

        var fullPath = Path.Combine(_environment.ContentRootPath, "App_Data", "uploads", document.StoragePath);
        if (!File.Exists(fullPath)) return null;

        return (fullPath, document.FileName);
    }

    // ---------- #4 Track ----------

    public async Task<List<ScholarshipApplication>> GetMyApplicationsAsync(string userId)
    {
        return await _db.ScholarshipApplications
            .Include(a => a.Scholarship)
            .Include(a => a.Documents)
            .Where(a => a.StudentUserId == userId)
            .OrderByDescending(a => a.SubmittedOn)
            .ToListAsync();
    }

    public async Task<ScholarshipApplication?> GetApplicationAsync(string userId, int applicationId)
    {
        return await _db.ScholarshipApplications
            .Include(a => a.Scholarship)
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.StudentUserId == userId);
    }

    // Counts for the dashboard tiles.
    public class StudentDashboard
    {
        public int OpenScholarships { get; set; }
        public int TotalApplications { get; set; }
        public int InProgress { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public bool ProfileComplete { get; set; }
    }

    public async Task<StudentDashboard> GetDashboardAsync(string userId)
    {
        var applications = await _db.ScholarshipApplications
            .Where(a => a.StudentUserId == userId)
            .ToListAsync();

        var profile = await GetOrCreateProfileAsync(userId);

        return new StudentDashboard
        {
            OpenScholarships = await _db.Scholarships
                .CountAsync(s => s.IsPublished && s.Deadline >= DateTime.Today),
            TotalApplications = applications.Count,
            InProgress = applications.Count(a => a.Status == ApplicationStatus.Submitted
                                              || a.Status == ApplicationStatus.UnderReview
                                              || a.Status == ApplicationStatus.Shortlisted),
            Approved = applications.Count(a => a.Status == ApplicationStatus.Approved),
            Rejected = applications.Count(a => a.Status == ApplicationStatus.Rejected),
            ProfileComplete = IsProfileComplete(profile)
        };
    }
}
