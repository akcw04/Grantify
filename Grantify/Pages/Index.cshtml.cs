using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;
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

    // The list the .cshtml file displays — the small public shape, which is
    // also what the landing page cache stores (see ScholarshipService).
    public List<LandingScholarship> Scholarships { get; set; } = new();

    // The headline figures across the top of the page.
    public PublicStats Stats { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        // Someone who is signed in goes straight to their own area.
        //
        // This page is the shop window for VISITORS — a public list of open
        // scholarships. An officer or an admin landing here sees nothing they
        // can act on, and having both "Home" and their dashboard in the navbar
        // just raises the question of which one is the real starting point.
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Officer")) return RedirectToPage("/Officer/Index");
            if (User.IsInRole("Admin")) return RedirectToPage("/Admin/Analytics/Index");
            if (User.IsInRole("Student")) return RedirectToPage("/Student/Index");
        }

        Scholarships = await _scholarshipService.GetPublishedAsync();
        Stats = await _scholarshipService.GetPublicStatsAsync();
        return Page();
    }
}
