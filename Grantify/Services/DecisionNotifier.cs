using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Grantify.Models;

namespace Grantify.Services;

// OWNER: Member B (Officer role). Task 2 service.
//
// Sends the award / rejection notice out of the system by email, using Amazon
// SNS. Until now a decision only appeared inside the site, so a student had to
// keep logging in to find out. The problem background calls this "award
// notification services".
//
// HOW IT IS SWITCHED ON
//   Notifications__TopicArn set  -> publish to that SNS topic
//   not set                      -> do nothing, just write a line in the log
//
// The second mode is what makes local development work: teammates press F5 with
// no AWS credentials and nothing here fails or slows them down.
//
// WHY SNS RATHER THAN SENDING EMAIL OURSELVES
// The app publishes one message describing what happened; SNS decides who
// receives it. Anyone who needs to know (a student's address, an admin mailbox,
// later a Lambda or an SQS queue) subscribes to the topic without us changing a
// line of code. That separation is the point of a message service, and it is
// what makes this part of the system genuinely cloud-native rather than an
// email call bolted onto a web page.
public class DecisionNotifier
{
    private readonly ILogger<DecisionNotifier> _logger;
    private readonly string? _topicArn;

    public DecisionNotifier(IConfiguration configuration, ILogger<DecisionNotifier> logger)
    {
        _logger = logger;

        var arn = configuration["Notifications:TopicArn"];
        _topicArn = string.IsNullOrWhiteSpace(arn) ? null : arn.Trim();
    }

    public bool IsConfigured => _topicArn is not null;

    // Announces one decision.
    //
    // NEVER throws. A notification failing must not undo an approval that is
    // already saved — the officer did their job, and the student can still see
    // the result on the site. We log the problem and carry on.
    public async Task PublishDecisionAsync(
        string studentName,
        string studentEmail,
        string scholarshipName,
        ApplicationStatus decision,
        string? officerRemarks)
    {
        if (_topicArn is null)
        {
            _logger.LogInformation(
                "Notifications are switched off, so no message was sent for {Student} ({Decision}).",
                studentName, decision);
            return;
        }

        try
        {
            var subject = decision switch
            {
                ApplicationStatus.Approved => "Your scholarship application was approved",
                ApplicationStatus.Rejected => "Your scholarship application was not successful",
                ApplicationStatus.Shortlisted => "You have been shortlisted",
                _ => "Update on your scholarship application"
            };

            var body = BuildMessage(studentName, studentEmail, scholarshipName, decision, officerRemarks);

            using var sns = new AmazonSimpleNotificationServiceClient();
            await sns.PublishAsync(new PublishRequest
            {
                TopicArn = _topicArn,
                // SNS rejects subjects over 100 characters or containing newlines.
                Subject = subject.Length > 100 ? subject[..100] : subject,
                Message = body
            });

            _logger.LogInformation("Sent a {Decision} notification for {Student}.", decision, studentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Could not send the {Decision} notification for {Student}. The decision itself was saved.",
                decision, studentName);
        }
    }

    // The text the recipient reads. Kept plain so it is readable in any mail app.
    private static string BuildMessage(
        string studentName,
        string studentEmail,
        string scholarshipName,
        ApplicationStatus decision,
        string? officerRemarks)
    {
        var opening = decision switch
        {
            ApplicationStatus.Approved =>
                $"Congratulations {studentName}. Your application for \"{scholarshipName}\" has been approved.",
            ApplicationStatus.Rejected =>
                $"Dear {studentName}, your application for \"{scholarshipName}\" was not successful on this occasion.",
            ApplicationStatus.Shortlisted =>
                $"Dear {studentName}, your application for \"{scholarshipName}\" has been shortlisted and is going forward for a final decision.",
            _ =>
                $"Dear {studentName}, there is an update on your application for \"{scholarshipName}\"."
        };

        var lines = new List<string> { opening, "" };

        // A rejection always carries its reason — the officer had to write one,
        // and it is unfair to send the outcome without it.
        if (!string.IsNullOrWhiteSpace(officerRemarks))
        {
            lines.Add($"Officer remarks: {officerRemarks}");
            lines.Add("");
        }

        lines.Add("Sign in to Grantify to see the full details of your application.");
        lines.Add("");
        lines.Add($"This message was sent to the applicant {studentEmail}.");

        return string.Join(Environment.NewLine, lines);
    }
}
