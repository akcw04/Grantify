using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.IntakePeriods;

public class DetailsModel : AdminPageModel
{
    private readonly IntakePeriodService _intakePeriods;

    public DetailsModel(IntakePeriodService intakePeriods)
    {
        _intakePeriods = intakePeriods;
    }

    public IntakePeriod? IntakePeriod { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        IntakePeriod = await _intakePeriods.GetByIdAsync(id);

        // A link to a record that has since been deleted is a "not found", not a
        // page with empty boxes on it.
        if (IntakePeriod is null)
        {
            return NotFound();
        }

        return Page();
    }
}
