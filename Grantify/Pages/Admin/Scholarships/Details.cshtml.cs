using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.Scholarships;

public class DetailsModel : AdminPageModel
{
    private readonly ScholarshipAdminService _scholarships;

    public DetailsModel(ScholarshipAdminService scholarships)
    {
        _scholarships = scholarships;
    }

    public Scholarship? Scholarship { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Scholarship = await _scholarships.GetByIdAsync(id);

        // A link to a record that has since been deleted is a "not found", not a
        // page with empty boxes on it.
        if (Scholarship is null)
        {
            return NotFound();
        }

        return Page();
    }
}
