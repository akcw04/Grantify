using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.IntakePeriods;

// OWNER: Member C (Admin role). Master data - intake periods.
//
// Deleting is refused while any scholarship still points at this row. The check
// runs BOTH when the page is drawn (so the button is disabled and the reason is
// on screen) and again on the POST, because a disabled button is a courtesy,
// not a rule — another administrator could link a scholarship to it in between.
public class DeleteModel : AdminPageModel
{
    private readonly IntakePeriodService _intakePeriods;

    public DeleteModel(IntakePeriodService intakePeriods)
    {
        _intakePeriods = intakePeriods;
    }

    [BindProperty]
    public int Id { get; set; }

    public IntakePeriod? IntakePeriod { get; private set; }

    // True when a scholarship still links to this intake period.
    public bool InUse { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        IntakePeriod = await _intakePeriods.GetByIdAsync(id);
        if (IntakePeriod is null)
        {
            return NotFound();
        }

        Id = IntakePeriod.Id;
        InUse = await _intakePeriods.IsInUseAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        IntakePeriod = await _intakePeriods.GetByIdAsync(Id);
        if (IntakePeriod is null)
        {
            return NotFound();
        }

        if (await _intakePeriods.IsInUseAsync(Id))
        {
            // Stay on the page with the reason showing, rather than bouncing
            // back to the list where it is not obvious what was refused.
            InUse = true;
            ModelState.AddModelError(string.Empty,
                "This intake period is still assigned to one or more scholarships and cannot be deleted.");
            return Page();
        }

        var name = IntakePeriod.PeriodName;
        await _intakePeriods.DeleteAsync(Id);

        SetMessage($"Intake period \"{name}\" was deleted.");
        return RedirectToPage("Index");
    }
}
