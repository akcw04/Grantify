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
public class EligibilityService
{
    // Checks one student against one scholarship. Pure logic, no database calls.
    public EligibilityResult Check(StudentProfile profile, Scholarship scholarship)
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

        // TODO (team): add more rules here when Admin adds more criteria fields,
        // e.g. course must match, deadline not passed. Announce in group chat first.

        result.IsEligible = result.Reasons.Count == 0;
        return result;
    }
}
