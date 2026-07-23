using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Grantify.Pages;

// EXAMPLE PAGE — this shows the pattern every page in this project follows:
//   1. Ask for a service in the constructor.
//   2. In OnGet, call the service and put the result in a property.
//   3. The .cshtml file shows that property. No database code in pages!
public class IndexModel : PageModel
{
    private readonly ScholarshipService _scholarshipService;

    public IndexModel(ScholarshipService scholarshipService)
    {
        _scholarshipService = scholarshipService;
    }

    // The list the .cshtml file displays.
    public List<Scholarship> Scholarships { get; set; } = new();

    public async Task OnGetAsync()
    {
        Scholarships = await _scholarshipService.GetPublishedAsync();
    }
}
