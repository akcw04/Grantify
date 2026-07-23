using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Models;

// The steps an application goes through, in order.
// Officers move an application from one status to the next.
public enum ApplicationStatus
{
    Submitted,      // student sent it in, nobody looked yet
    UnderReview,    // an officer is checking it
    Shortlisted,    // passed review, waiting for final decision
    Approved,       // student gets the scholarship
    Rejected        // student does not get the scholarship
}

// One student applying for one scholarship.
public class ScholarshipApplication
{
    public int Id { get; set; }

    // Which scholarship the student applied for.
    public int ScholarshipId { get; set; }
    public Scholarship? Scholarship { get; set; }

    // Which student sent this application.
    public string StudentUserId { get; set; } = string.Empty;
    public ApplicationUser? Student { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;

    public DateTime SubmittedOn { get; set; }

    // ----- Filled in by the officer during review -----

    // Score given by the officer (0-100). Null until the officer scores it.
    public int? Score { get; set; }

    // Officer's notes, e.g. why it was rejected.
    [StringLength(1000)]
    public string? OfficerRemarks { get; set; }

    // Files the student uploaded to support this application.
    public List<ApplicationDocument> Documents { get; set; } = new();
}
