using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.IntakePeriods;

public class DeleteModel : AdminPageModel
{
    private readonly IntakePeriodService _intakePeriods;

    public DeleteModel(IntakePeriodService intakePeriods)
    {
        _intakePeriods = intakePeriods;
    }

    [BindProperty]
    public IntakePeriod? IntakePeriod { get; set; }

    public bool InUse { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        IntakePeriod = await _intakePeriods.GetByIdAsync(id);
        if (IntakePeriod is not null)
        {
            InUse = await _intakePeriods.IsInUseAsync(id);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (IntakePeriod is null)
        {
            return RedirectToPage("Index");
        }

        if (await _intakePeriods.IsInUseAsync(IntakePeriod.Id))
        {
            SetMessage("This intake period is still assigned to one or more scholarships and cannot be deleted.", isError: true);
            return RedirectToPage("Index");
        }

        var name = (await _intakePeriods.GetByIdAsync(IntakePeriod.Id))?.PeriodName ?? "Intake period";
        var deleted = await _intakePeriods.DeleteAsync(IntakePeriod.Id);
        if (deleted)
        {
            SetMessage($"Intake period \"{name}\" was deleted.");
        }

        return RedirectToPage("Index");
    }
}
