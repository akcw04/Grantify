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
        return Page();
    }
}
