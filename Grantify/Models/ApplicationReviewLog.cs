using System.ComponentModel.DataAnnotations;

namespace Grantify.Models;

// OWNER: Member B (Officer role).
//
// One line of history for an application. Every time an officer changes the
// status, scores an application or verifies a document, we write one row here.
// Nothing in this table is ever edited or deleted — it is the audit trail that
// answers "who did what, when, and why".
//
// Why we need it: the marking scheme asks for rigorous, trustworthy handling of
// decisions. A student who is rejected must be traceable to a named officer.
// In Task 2 this same record is the natural trigger point for the SNS/SES
// notification that tells the student their result.
public class ApplicationReviewLog
{
    public int Id { get; set; }

    // Which application this history line belongs to.
    public int ScholarshipApplicationId { get; set; }
    public ScholarshipApplication? Application { get; set; }

    // The officer who did it. We keep the id for traceability and the name as a
    // snapshot, so the history still reads correctly even if the account is
    // renamed or deactivated later.
    [Required, StringLength(450)]
    public string OfficerUserId { get; set; } = string.Empty;

    [StringLength(200)]
    public string OfficerName { get; set; } = string.Empty;

    // Short label of what happened, e.g. "Started review", "Decision: Approved",
    // "Document verified: transcript.pdf".
    [Required, StringLength(200)]
    public string Action { get; set; } = string.Empty;

    // The officer's note, or the reason they typed in. May be empty.
    [StringLength(1000)]
    public string? Details { get; set; }

    // Status before and after the change. Null when the action did not change
    // the status (for example verifying a document).
    public ApplicationStatus? FromStatus { get; set; }
    public ApplicationStatus? ToStatus { get; set; }

    // May the STUDENT be shown this entry?
    //
    // The history is a full internal record, so most of it is not written for
    // the applicant. A document note like "suspect forged stamp, check with the
    // registry" is a working note between officers, not a message to send out.
    // Only a real decision on the application is meant for the student.
    //
    // Defaults to false: a new kind of log entry stays private until somebody
    // deliberately decides otherwise. Member A's notification feature filters
    // on this flag, so the Officer module — which knows what is safe to send —
    // is the one that decides.
    public bool IsStudentVisible { get; set; }

    public DateTime CreatedOn { get; set; }
}
