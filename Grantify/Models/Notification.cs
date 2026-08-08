using System.ComponentModel.DataAnnotations;

namespace Grantify.Models;

// OWNER: Member A (Student role).
//
// One message telling a student something happened to their application:
// an officer started the review, verified a document, or made a decision.
//
// Where the rows come from: nothing writes here directly. StudentService reads
// Member B's ApplicationReviewLog (the officer audit trail) and turns each new
// entry into one notification. That keeps the two modules apart - the officer
// pages do not need to know notifications exist.
//
// SourceLogId is what stops duplicates: a log line is only ever turned into a
// notification once.
//
// Task 2: the same trigger point sends the message by Amazon SNS or SES as well
// as storing it here.
public class Notification
{
    public int Id { get; set; }

    // Who this message is for.
    [Required, StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    // Which application it is about, so the page can link straight to it.
    public int ScholarshipApplicationId { get; set; }
    public ScholarshipApplication? Application { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    // The officer audit line this was generated from. One notification per log
    // line, so opening the page twice does not create the message twice.
    public int SourceLogId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedOn { get; set; }
}
