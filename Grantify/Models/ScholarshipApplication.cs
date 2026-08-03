using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    //
    // The [ForeignKey] line matters: without it EF Core does not realise that
    // StudentUserId is the link to the Student below (it only recognises names
    // like "StudentId" on its own). It would then quietly build a second,
    // hidden column and Application.Student would always come back empty.
    // Fixed by Member B on 31 July 2026 — announced in the group chat.
    [ForeignKey(nameof(Student))]
    [StringLength(450)]
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

    // Who last acted on this application, and when. Added by Member B so the
    // review queue can show "handled by ..." and so decisions are traceable.
    // Plain id, not a navigation property, to keep the database simple.
    [StringLength(450)]
    public string? ReviewedByUserId { get; set; }

    [StringLength(200)]
    public string? ReviewedByName { get; set; }

    public DateTime? ReviewedOn { get; set; }

    // Files the student uploaded to support this application.
    public List<ApplicationDocument> Documents { get; set; } = new();

    // Full history of what officers did to this application (audit trail).
    public List<ApplicationReviewLog> ReviewLogs { get; set; } = new();
}
