using Grantify.Data;
using Grantify.Models;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Services;

// OWNER: Member B (Officer role).
//
// All the backend logic for the Scholarship Officer. The Officer pages stay
// thin: they collect what the officer typed, call one method here, and show the
// answer. Every rule about "what an officer is allowed to do" lives in this one
// class, which is what makes it testable and easy to explain in the video.
//
// Registered in Program.cs: builder.Services.AddScoped<OfficerService>();
public class OfficerService
{
    private readonly AppDbContext _db;
    private readonly EligibilityService _eligibility;

    public OfficerService(AppDbContext db, EligibilityService eligibility)
    {
        _db = db;
        _eligibility = eligibility;
    }

    // ------------------------------------------------------------------
    // 1. Rules about which status can follow which
    // ------------------------------------------------------------------

    // An application may only move along this path. Anything else is refused.
    //
    //   Submitted -> UnderReview -> Shortlisted -> Approved
    //        |            |              |
    //        +------------+--------------+------> Rejected
    //
    // Approved and Rejected are final: once a student has been told the result,
    // an officer must not quietly change it.
    private static readonly Dictionary<ApplicationStatus, ApplicationStatus[]> AllowedNextStatuses = new()
    {
        [ApplicationStatus.Submitted] = new[] { ApplicationStatus.UnderReview, ApplicationStatus.Rejected },
        [ApplicationStatus.UnderReview] = new[] { ApplicationStatus.UnderReview, ApplicationStatus.Shortlisted, ApplicationStatus.Approved, ApplicationStatus.Rejected },
        [ApplicationStatus.Shortlisted] = new[] { ApplicationStatus.Shortlisted, ApplicationStatus.Approved, ApplicationStatus.Rejected },
        [ApplicationStatus.Approved] = Array.Empty<ApplicationStatus>(),
        [ApplicationStatus.Rejected] = Array.Empty<ApplicationStatus>()
    };

    // True when nothing more can be done to this application.
    public static bool IsFinal(ApplicationStatus status) =>
        status is ApplicationStatus.Approved or ApplicationStatus.Rejected;

    // The statuses the officer is allowed to pick right now. The page uses this
    // to build the dropdown, so the officer never sees an option that would fail.
    public static IReadOnlyList<ApplicationStatus> NextStatusesFor(ApplicationStatus current) =>
        AllowedNextStatuses.TryGetValue(current, out var next) ? next : Array.Empty<ApplicationStatus>();

    // ------------------------------------------------------------------
    // 2. Dashboard numbers
    // ------------------------------------------------------------------

    // The counts shown on the officer's landing page.
    public async Task<OfficerDashboard> GetDashboardAsync()
    {
        var today = DateTime.Today;

        // Group once in the database instead of running five separate counts.
        var byStatus = await _db.ScholarshipApplications
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int CountOf(ApplicationStatus status) =>
            byStatus.FirstOrDefault(x => x.Status == status)?.Count ?? 0;

        return new OfficerDashboard
        {
            Submitted = CountOf(ApplicationStatus.Submitted),
            UnderReview = CountOf(ApplicationStatus.UnderReview),
            Shortlisted = CountOf(ApplicationStatus.Shortlisted),
            Approved = CountOf(ApplicationStatus.Approved),
            Rejected = CountOf(ApplicationStatus.Rejected),

            PendingDocuments = await _db.ApplicationDocuments
                .CountAsync(d => d.Status == DocumentStatus.Pending),

            FlaggedDocuments = await _db.ApplicationDocuments
                .CountAsync(d => d.Status == DocumentStatus.Flagged),

            // Applications still waiting for a decision even though the
            // scholarship's closing date has already passed — these are the
            // ones an officer should open first.
            // The status test is written out in full (instead of calling
            // IsFinal) because EF Core has to turn this into SQL.
            OverdueApplications = await _db.ScholarshipApplications
                .CountAsync(a => a.Status != ApplicationStatus.Approved
                                 && a.Status != ApplicationStatus.Rejected
                                 && a.Scholarship!.Deadline < today)
        };
    }

    // ------------------------------------------------------------------
    // 3. The application queue
    // ------------------------------------------------------------------

    // Returns the applications an officer has to work through, after applying
    // whatever filters they chose on the page.
    public async Task<List<QueueItem>> GetQueueAsync(QueueFilter filter)
    {
        // Start with a query, add each filter only if the officer set it.
        var query = _db.ScholarshipApplications
            .Include(a => a.Scholarship)
            .Include(a => a.Student)
            .Include(a => a.Documents)
            .AsQueryable();

        if (filter.ScholarshipId is not null)
        {
            query = query.Where(a => a.ScholarshipId == filter.ScholarshipId);
        }

        if (filter.Status is not null)
        {
            query = query.Where(a => a.Status == filter.Status);
        }

        // "Needs action" = everything that is not finished yet. This is the
        // default view because it is what an officer actually works on.
        if (filter.OnlyOpen)
        {
            query = query.Where(a => a.Status != ApplicationStatus.Approved
                                     && a.Status != ApplicationStatus.Rejected);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(a =>
                a.Student!.FullName.Contains(term) ||
                a.Student!.Email!.Contains(term) ||
                a.Scholarship!.Name.Contains(term));
        }

        // Sorting. Oldest first by default so nobody's application is forgotten.
        query = filter.Sort switch
        {
            QueueSort.NewestFirst => query.OrderByDescending(a => a.SubmittedOn),
            QueueSort.DeadlineSoonest => query.OrderBy(a => a.Scholarship!.Deadline),
            _ => query.OrderBy(a => a.SubmittedOn)
        };

        var applications = await query.ToListAsync();

        // Turn the database rows into the small shape the table needs.
        return applications.Select(a => new QueueItem
        {
            ApplicationId = a.Id,
            StudentName = a.Student?.FullName ?? "(unknown student)",
            StudentEmail = a.Student?.Email ?? string.Empty,
            ScholarshipName = a.Scholarship?.Name ?? "(deleted scholarship)",
            Deadline = a.Scholarship?.Deadline,
            Status = a.Status,
            Score = a.Score,
            SubmittedOn = a.SubmittedOn,
            ReviewedByName = a.ReviewedByName,
            DocumentCount = a.Documents.Count,
            PendingDocumentCount = a.Documents.Count(d => d.Status == DocumentStatus.Pending),
            FlaggedDocumentCount = a.Documents.Count(d => d.Status == DocumentStatus.Flagged),
            IsOverdue = a.Scholarship is not null
                        && a.Scholarship.Deadline < DateTime.Today
                        && !IsFinal(a.Status)
        }).ToList();
    }

    // Fills the "scholarship" dropdown on the queue page. Only scholarships
    // that actually have applications are listed, to keep the list short.
    public async Task<List<Scholarship>> GetScholarshipsWithApplicationsAsync()
    {
        return await _db.Scholarships
            .Where(s => s.Applications.Any())
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    // ------------------------------------------------------------------
    // 4. One application, in full
    // ------------------------------------------------------------------

    // Everything the review page needs: the application, the student's profile,
    // the automatic eligibility result, the uploaded files and the history.
    // Returns null when the id does not exist.
    public async Task<ReviewDetail?> GetReviewDetailAsync(int applicationId)
    {
        var application = await _db.ScholarshipApplications
            .Include(a => a.Scholarship)
            .Include(a => a.Student)
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
        {
            return null;
        }

        // The student's academic and income details live in a separate table.
        var profile = await _db.StudentProfiles
            .FirstOrDefaultAsync(p => p.UserId == application.StudentUserId);

        var detail = new ReviewDetail
        {
            Application = application,
            Profile = profile,
            Documents = application.Documents.OrderBy(d => d.UploadedOn).ToList(),
            History = await GetHistoryAsync(applicationId),
            AllowedNextStatuses = NextStatusesFor(application.Status)
        };

        // Run the automated screening again, live, so the officer sees the same
        // answer the student saw. If the student never filled in their profile
        // we cannot screen them — say so instead of guessing.
        if (profile is not null && application.Scholarship is not null)
        {
            detail.Eligibility = _eligibility.Check(profile, application.Scholarship);
        }

        return detail;
    }

    // The audit trail for one application, newest entry first.
    public async Task<List<ApplicationReviewLog>> GetHistoryAsync(int applicationId)
    {
        return await _db.ApplicationReviewLogs
            .Where(l => l.ScholarshipApplicationId == applicationId)
            .OrderByDescending(l => l.CreatedOn)
            .ThenByDescending(l => l.Id)
            .ToListAsync();
    }

    // ------------------------------------------------------------------
    // 5. Actions an officer can take
    // ------------------------------------------------------------------

    // Officer opens an application and claims it. Moves Submitted -> UnderReview
    // so other officers can see somebody is already on it.
    public async Task<ServiceResult> StartReviewAsync(int applicationId, string officerUserId, string officerName)
    {
        var application = await _db.ScholarshipApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
        {
            return ServiceResult.Fail("That application no longer exists.");
        }

        if (application.Status != ApplicationStatus.Submitted)
        {
            return ServiceResult.Fail("This application is already being reviewed or is finished.");
        }

        var from = application.Status;
        application.Status = ApplicationStatus.UnderReview;
        Stamp(application, officerUserId, officerName);

        AddLog(applicationId, officerUserId, officerName, "Started review", null, from, application.Status);

        await _db.SaveChangesAsync();
        return ServiceResult.Ok("Review started. This application is now marked as under review.");
    }

    // The main decision method: score the application, write remarks and move it
    // to its next status. Every rule below exists so a decision cannot be saved
    // in a state we would not be able to defend to a student.
    public async Task<ServiceResult> SaveDecisionAsync(
        int applicationId,
        ApplicationStatus newStatus,
        int? score,
        string? remarks,
        string officerUserId,
        string officerName)
    {
        var application = await _db.ScholarshipApplications
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
        {
            return ServiceResult.Fail("That application no longer exists.");
        }

        // Rule 1: a finished application is read-only.
        if (IsFinal(application.Status))
        {
            return ServiceResult.Fail(
                $"This application was already {application.Status} and can no longer be changed.");
        }

        // Rule 2: the move must follow the allowed path.
        if (!NextStatusesFor(application.Status).Contains(newStatus))
        {
            return ServiceResult.Fail(
                $"An application cannot go from {application.Status} straight to {newStatus}.");
        }

        // Rule 3: a score, when given, is a mark out of 100.
        if (score is not null && (score < 0 || score > 100))
        {
            return ServiceResult.Fail("The score must be between 0 and 100.");
        }

        // Rule 4: you cannot shortlist or approve somebody you never scored.
        var needsScore = newStatus is ApplicationStatus.Shortlisted or ApplicationStatus.Approved;
        var effectiveScore = score ?? application.Score;
        if (needsScore && effectiveScore is null)
        {
            return ServiceResult.Fail($"Give the application a score before you mark it as {newStatus}.");
        }

        // Rule 5: a rejection must always carry a written reason, so the student
        // can be told why.
        var effectiveRemarks = string.IsNullOrWhiteSpace(remarks) ? application.OfficerRemarks : remarks.Trim();
        if (newStatus == ApplicationStatus.Rejected
            && string.IsNullOrWhiteSpace(effectiveRemarks))
        {
            return ServiceResult.Fail("Write a short reason in the remarks before rejecting an application.");
        }

        // Rule 6: do not approve or shortlist while a document is still
        // unchecked or has been flagged as wrong.
        if (needsScore)
        {
            if (application.Documents.Any(d => d.Status == DocumentStatus.Flagged))
            {
                return ServiceResult.Fail(
                    "One or more documents are flagged. Resolve them before shortlisting or approving.");
            }

            if (application.Documents.Any(d => d.Status == DocumentStatus.Pending))
            {
                return ServiceResult.Fail(
                    "Verify every uploaded document before shortlisting or approving.");
            }
        }

        var from = application.Status;

        application.Status = newStatus;
        if (score is not null)
        {
            application.Score = score;
        }
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            application.OfficerRemarks = remarks.Trim();
        }
        Stamp(application, officerUserId, officerName);

        // "Decision: Approved" reads better in the history than just "Approved".
        var action = from == newStatus ? "Updated review" : $"Decision: {newStatus}";
        AddLog(applicationId, officerUserId, officerName, action, effectiveRemarks, from, newStatus);

        await _db.SaveChangesAsync();

        // TASK 2 HOOK: this is the exact point where an approval or rejection
        // should publish an SNS/SES notification to the student.
        return ServiceResult.Ok(from == newStatus
            ? "Review saved."
            : $"Application moved to {newStatus}.");
    }

    // Officer checked one uploaded file and says whether it is acceptable.
    public async Task<ServiceResult> SetDocumentStatusAsync(
        int documentId,
        DocumentStatus newStatus,
        string? note,
        string officerUserId,
        string officerName)
    {
        var document = await _db.ApplicationDocuments
            .Include(d => d.Application)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document is null)
        {
            return ServiceResult.Fail("That document no longer exists.");
        }

        // Documents belonging to a finished application are read-only too.
        if (document.Application is not null && IsFinal(document.Application.Status))
        {
            return ServiceResult.Fail(
                "This application is already finished, so its documents can no longer be changed.");
        }

        // "Pending" means nobody has looked yet — an officer cannot set that.
        if (newStatus == DocumentStatus.Pending)
        {
            return ServiceResult.Fail("Choose either Verified or Flagged.");
        }

        // Flagging is an accusation, so it must come with an explanation.
        if (newStatus == DocumentStatus.Flagged && string.IsNullOrWhiteSpace(note))
        {
            return ServiceResult.Fail("Say what is wrong with the document before flagging it.");
        }

        document.Status = newStatus;
        document.OfficerNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        document.VerifiedByUserId = officerUserId;
        document.VerifiedByName = officerName;
        document.VerifiedOn = DateTime.UtcNow;

        AddLog(document.ScholarshipApplicationId, officerUserId, officerName,
            $"Document {newStatus.ToString().ToLower()}: {document.FileName}",
            document.OfficerNote, null, null);

        await _db.SaveChangesAsync();
        return ServiceResult.Ok($"\"{document.FileName}\" marked as {newStatus}.");
    }

    // Every document across the whole system that still needs checking.
    // This is the officer's second working list, next to the application queue.
    public async Task<List<DocumentQueueItem>> GetDocumentQueueAsync(DocumentStatus? status = null)
    {
        var query = _db.ApplicationDocuments
            .Include(d => d.Application!).ThenInclude(a => a.Student)
            .Include(d => d.Application!).ThenInclude(a => a.Scholarship)
            .AsQueryable();

        // Default view: everything not yet settled (pending or flagged).
        query = status is null
            ? query.Where(d => d.Status != DocumentStatus.Verified)
            : query.Where(d => d.Status == status);

        var documents = await query
            .OrderBy(d => d.UploadedOn)
            .ToListAsync();

        return documents.Select(d => new DocumentQueueItem
        {
            DocumentId = d.Id,
            ApplicationId = d.ScholarshipApplicationId,
            FileName = d.FileName,
            Status = d.Status,
            UploadedOn = d.UploadedOn,
            OfficerNote = d.OfficerNote,
            StudentName = d.Application?.Student?.FullName ?? "(unknown student)",
            ScholarshipName = d.Application?.Scholarship?.Name ?? "(deleted scholarship)"
        }).ToList();
    }

    // ------------------------------------------------------------------
    // 6. Small helpers used by the methods above
    // ------------------------------------------------------------------

    // Records who touched the application last.
    private static void Stamp(ScholarshipApplication application, string officerUserId, string officerName)
    {
        application.ReviewedByUserId = officerUserId;
        application.ReviewedByName = officerName;
        application.ReviewedOn = DateTime.UtcNow;
    }

    // Adds one history row. Note it only stages the row — the calling method
    // saves, so the change and its log entry are written together.
    private void AddLog(
        int applicationId,
        string officerUserId,
        string officerName,
        string action,
        string? details,
        ApplicationStatus? from,
        ApplicationStatus? to)
    {
        _db.ApplicationReviewLogs.Add(new ApplicationReviewLog
        {
            ScholarshipApplicationId = applicationId,
            OfficerUserId = officerUserId,
            OfficerName = officerName,
            Action = action,
            Details = details,
            FromStatus = from,
            ToStatus = to,
            CreatedOn = DateTime.UtcNow
        });
    }
}

// ----------------------------------------------------------------------
// Small classes used only by the officer feature.
// They live here so everything an officer needs is in one file.
// ----------------------------------------------------------------------

// The answer every "do something" method gives back: did it work, and what do
// we tell the officer? Pages show Message either as a green or a red bar.
public class ServiceResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ServiceResult Ok(string message) => new() { Success = true, Message = message };
    public static ServiceResult Fail(string message) => new() { Success = false, Message = message };
}

// How the officer wants the queue sorted.
public enum QueueSort
{
    OldestFirst,        // fairest default: first come, first served
    NewestFirst,
    DeadlineSoonest
}

// What the officer typed into the filter bar on the queue page.
public class QueueFilter
{
    public int? ScholarshipId { get; set; }
    public ApplicationStatus? Status { get; set; }
    public string? Search { get; set; }

    // Hide applications that are already approved or rejected.
    public bool OnlyOpen { get; set; } = true;

    public QueueSort Sort { get; set; } = QueueSort.OldestFirst;
}

// One row of the application queue table.
public class QueueItem
{
    public int ApplicationId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string ScholarshipName { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
    public ApplicationStatus Status { get; set; }
    public int? Score { get; set; }
    public DateTime SubmittedOn { get; set; }
    public string? ReviewedByName { get; set; }
    public int DocumentCount { get; set; }
    public int PendingDocumentCount { get; set; }
    public int FlaggedDocumentCount { get; set; }
    public bool IsOverdue { get; set; }
}

// One row of the document verification table.
public class DocumentQueueItem
{
    public int DocumentId { get; set; }
    public int ApplicationId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
    public DateTime UploadedOn { get; set; }
    public string? OfficerNote { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ScholarshipName { get; set; } = string.Empty;
}

// Everything shown on the single-application review page.
public class ReviewDetail
{
    public ScholarshipApplication Application { get; set; } = null!;

    // Null when the student never completed their profile — then we cannot
    // run the automatic screening.
    public StudentProfile? Profile { get; set; }

    // Null for the same reason. The page shows a warning instead.
    public EligibilityResult? Eligibility { get; set; }

    public List<ApplicationDocument> Documents { get; set; } = new();
    public List<ApplicationReviewLog> History { get; set; } = new();
    public IReadOnlyList<ApplicationStatus> AllowedNextStatuses { get; set; } = Array.Empty<ApplicationStatus>();

    // Handy shortcuts for the page.
    public bool IsFinished => OfficerService.IsFinal(Application.Status);
    public int PendingDocumentCount => Documents.Count(d => d.Status == DocumentStatus.Pending);
    public int FlaggedDocumentCount => Documents.Count(d => d.Status == DocumentStatus.Flagged);
}

// The counts on the officer landing page.
public class OfficerDashboard
{
    public int Submitted { get; set; }
    public int UnderReview { get; set; }
    public int Shortlisted { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int PendingDocuments { get; set; }
    public int FlaggedDocuments { get; set; }
    public int OverdueApplications { get; set; }

    // Everything still waiting for an officer to act.
    public int OpenTotal => Submitted + UnderReview + Shortlisted;
}
