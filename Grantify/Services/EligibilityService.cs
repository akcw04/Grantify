using Grantify.Models;

namespace Grantify.Services;

// The answer the eligibility check gives back.
public class EligibilityResult
{
    // True when the student passes every rule of the scholarship.
    public bool IsEligible { get; set; }

    // One plain-English sentence for every rule the student did NOT pass.
    // Empty list = eligible.
    public List<string> Reasons { get; set; } = new();
}

// Our showcase feature: automated eligibility screening.
// It compares a student's profile against a scholarship's rules and
// explains the result in plain English.
//
// Task 1: this stays a normal C# class inside the web app.
// Task 2: this same logic moves into an AWS Lambda function (serverless).
// Keep the logic in this one class so the move later is easy.
//
// SHARED FILE: used by the Student pages (can I apply?) and by the Officer
// review page (was this applicant eligible?). Announce changes in the group chat.
public class EligibilityService
{
    // Checks one student against one scholarship. Pure logic, no database calls.
    //
    // applicationDate is the date the rules are judged against:
    //
    //   - Leave it NULL when a student is browsing or about to apply. The rules
    //     are then judged against today, which is what "can I apply?" means.
    //
    //   - Pass the application's submission date when an OFFICER is reviewing an
    //     application that was already sent in. This matters: an officer often
    //     reviews after the closing date has passed, and judging that
    //     application against today would wrongly report a student who applied
    //     in good time as "not eligible" — and could get them rejected for
    //     something that was the office's delay, not their fault.
    public EligibilityResult Check(
        StudentProfile profile,
        Scholarship scholarship,
        DateTime? applicationDate = null)
    {
        var result = new EligibilityResult();

        // Rule 1: CGPA must be at least the scholarship's minimum.
        if (profile.Cgpa < scholarship.MinimumCgpa)
        {
            result.Reasons.Add(
                $"Your CGPA ({profile.Cgpa:0.00}) is below the required minimum of {scholarship.MinimumCgpa:0.00}.");
        }

        // Rule 2: household income must not be above the limit (if the scholarship has one).
        if (scholarship.MaximumHouseholdIncome is not null
            && profile.HouseholdIncome > scholarship.MaximumHouseholdIncome)
        {
            result.Reasons.Add(
                $"Your household income (RM {profile.HouseholdIncome:0}) is above the limit of RM {scholarship.MaximumHouseholdIncome:0}.");
        }

        // Rule 3: the scholarship must still have been open on the date we judge.
        // Compare whole days only — applying at 11pm on the closing day counts.
        var judgedOn = applicationDate?.Date ?? DateTime.Today;
        if (judgedOn > scholarship.Deadline.Date)
        {
            // The wording changes with who is reading it.
            result.Reasons.Add(applicationDate is null
                ? $"The closing date for this scholarship passed on {scholarship.Deadline:dd MMM yyyy}."
                : $"This application was submitted on {judgedOn:dd MMM yyyy}, after the closing date of {scholarship.Deadline:dd MMM yyyy}.");
        }

        // TODO (team): add more rules here when Admin adds more criteria fields,
        // e.g. course must match. Announce in group chat first.

        result.IsEligible = result.Reasons.Count == 0;
        return result;
    }
}
