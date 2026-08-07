using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Student;

// #2 - browse and search the published scholarships. Every listing is screened
// against this student's profile, and the reason for each failed rule is shown.
public class ScholarshipsModel : StudentPageModel
{
    private readonly StudentService _students;

    public ScholarshipsModel(StudentService students)
    {
        _students = students;
    }

    // Typed into the search box; kept in the URL so the results can be shared.
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    // Tick box: hide anything the student does not qualify for.
    [BindProperty(SupportsGet = true)]
    public bool EligibleOnly { get; set; }

    public List<StudentService.CatalogueItem> Items { get; private set; } = new();
    public bool ProfileComplete { get; private set; }

    public async Task OnGetAsync()
    {
        var profile = await _students.GetOrCreateProfileAsync(CurrentUserId);
        ProfileComplete = StudentService.IsProfileComplete(profile);

        Items = await _students.GetCatalogueAsync(CurrentUserId, Search);

        if (EligibleOnly)
        {
            Items = Items.Where(i => i.Eligibility.IsEligible).ToList();
        }
    }

    // Apply straight from the list.
    public async Task<IActionResult> OnPostApplyAsync(int scholarshipId)
    {
        var result = await _students.ApplyAsync(CurrentUserId, scholarshipId);

        SetMessage(result.Message, !result.Success);

        return result.Success
            ? RedirectToPage("Apply", new { id = result.ApplicationId })
            : RedirectToPage(new { Search, EligibleOnly });
    }
}
