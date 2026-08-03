using System.ComponentModel.DataAnnotations;

namespace Grantify.Models;

// What an officer decided about one uploaded document.
public enum DocumentStatus
{
    Pending,    // not checked yet
    Verified,   // officer confirmed the document is correct
    Flagged     // something looks wrong, needs attention
}

// One file a student uploaded with an application (transcript, income proof...).
public class ApplicationDocument
{
    public int Id { get; set; }

    // Which application this file belongs to.
    public int ScholarshipApplicationId { get; set; }
    public ScholarshipApplication? Application { get; set; }

    // The name the student's file had, e.g. "transcript.pdf".
    [Required, StringLength(300)]
    public string FileName { get; set; } = string.Empty;

    // Where the file is stored.
    // Task 1: a path on the server. Task 2: this becomes the Amazon S3 key.
    [Required, StringLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    public DateTime UploadedOn { get; set; }

    // ----- Filled in by the officer when they check the document -----
    // Added by Member B. In Task 2, Textract fills OfficerNote automatically
    // with what it read from the file, and the officer only confirms it.

    // Why the document was flagged, or a short note about what was checked.
    [StringLength(500)]
    public string? OfficerNote { get; set; }

    [StringLength(450)]
    public string? VerifiedByUserId { get; set; }

    [StringLength(200)]
    public string? VerifiedByName { get; set; }

    public DateTime? VerifiedOn { get; set; }
}
