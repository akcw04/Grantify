using System.Text;
using System.Text.Json;
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
// TASK 2: THE CHECK IS NOW A MICROSERVICE.
// The same three rules also run as an AWS Lambda function behind API Gateway.
// When the environment property Eligibility__ApiUrl is set, CheckAsync sends
// the numbers there and uses the answer; the rules in Check() below then act
// as the FALLBACK. When it is not set (our laptops), Check() simply runs
// locally and nothing needs AWS.
//
// Why the fallback exists: screening is what the browse page is built on. If
// the remote service is slow or down, a student should get a slightly less
// cloud-flavoured answer, never a broken page. The call has a 3-second budget
// (set in Program.cs) and ANY failure quietly drops to the local rules.
//
// Keep the Lambda's rules in step with Check() — they are the same three
// rules in two runtimes, and the wording of the reasons must match so a
// student sees identical text either way.
//
// SHARED FILE: used by the Student pages (can I apply?) and by the Officer
// review page (was this applicant eligible?). Announce changes in the group chat.
public class EligibilityService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EligibilityService> _logger;
    private readonly string? _apiUrl;

    public EligibilityService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<EligibilityService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        var url = configuration["Eligibility:ApiUrl"];
        _apiUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
    }

    // What the web app sends to the Lambda: just the numbers the rules need,
    // never the student's name or any other personal detail.
    private record ScreeningRequest(
        decimal Cgpa,
        decimal HouseholdIncome,
        decimal MinimumCgpa,
        decimal? MaximumHouseholdIncome,
        string Deadline,     // yyyy-MM-dd
        string JudgedOn,     // yyyy-MM-dd
        bool StudentFacing); // decides the wording of the deadline reason

    private record ScreeningResponse(bool IsEligible, List<string> Reasons);

    // Checks one student against one scholarship, preferring the remote
    // microservice and falling back to the local rules. See the class comment
    // for applicationDate's meaning — it is explained on Check() below.
    public async Task<EligibilityResult> CheckAsync(
        StudentProfile profile,
        Scholarship scholarship,
        DateTime? applicationDate = null)
    {
        if (_apiUrl is null)
        {
            return Check(profile, scholarship, applicationDate);
        }

        try
        {
            var request = new ScreeningRequest(
                profile.Cgpa,
                profile.HouseholdIncome,
                scholarship.MinimumCgpa,
                scholarship.MaximumHouseholdIncome,
                scholarship.Deadline.ToString("yyyy-MM-dd"),
                (applicationDate?.Date ?? DateTime.Today).ToString("yyyy-MM-dd"),
                StudentFacing: applicationDate is null);

            var client = _httpClientFactory.CreateClient("eligibility");
            using var content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(_apiUrl, content);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var answer = JsonSerializer.Deserialize<ScreeningResponse>(body, JsonOptions);

            if (answer is null)
            {
                throw new InvalidOperationException("The screening service returned an empty answer.");
            }

            return new EligibilityResult { IsEligible = answer.IsEligible, Reasons = answer.Reasons };
        }
        catch (Exception ex)
        {
            // Slow, down, or answered nonsense — the reason does not matter to
            // the person waiting for the page. Use the local rules and note it.
            _logger.LogWarning(ex,
                "The eligibility microservice could not be used; screened locally instead.");
            return Check(profile, scholarship, applicationDate);
        }
    }

    // The three rules, run locally. Pure logic, no database calls — which is
    // exactly why the same logic could be lifted into a Lambda unchanged.
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

        result.IsEligible = result.Reasons.Count == 0;
        return result;
    }
}
