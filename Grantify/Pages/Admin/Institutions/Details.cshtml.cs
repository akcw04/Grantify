using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.Institutions;

public class DetailsModel : AdminPageModel
{
    private readonly InstitutionService _institutions;

    public DetailsModel(InstitutionService institutions)
    {
        _institutions = institutions;
    }

    public Institution? Institution { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Institution = await _institutions.GetByIdAsync(id);
        return Page();
    }
}
