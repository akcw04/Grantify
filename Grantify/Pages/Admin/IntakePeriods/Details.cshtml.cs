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
        return Page();
    }
}
