using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.Institutions;

// OWNER: Member C (Admin role). Master data - institutions.
//
// Deleting is refused while any scholarship still points at this row. The check
// runs BOTH when the page is drawn (so the button is disabled and the reason is
// on screen) and again on the POST, because a disabled button is a courtesy,
// not a rule — another administrator could link a scholarship to it in between.
public class DeleteModel : AdminPageModel
{
    private readonly InstitutionService _institutions;

    public DeleteModel(InstitutionService institutions)
    {
        _institutions = institutions;
    }

    [BindProperty]
    public int Id { get; set; }

    public Institution? Institution { get; private set; }

    // True when a scholarship still links to this institution.
    public bool InUse { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Institution = await _institutions.GetByIdAsync(id);
        if (Institution is null)
        {
            return NotFound();
        }

        Id = Institution.Id;
        InUse = await _institutions.IsInUseAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Institution = await _institutions.GetByIdAsync(Id);
        if (Institution is null)
        {
            return NotFound();
        }

        if (await _institutions.IsInUseAsync(Id))
        {
            // Stay on the page with the reason showing, rather than bouncing
            // back to the list where it is not obvious what was refused.
            InUse = true;
            ModelState.AddModelError(string.Empty,
                "This institution is still assigned to one or more scholarships and cannot be deleted.");
            return Page();
        }

        var name = Institution.Name;
        await _institutions.DeleteAsync(Id);

        SetMessage($"Institution \"{name}\" was deleted.");
        return RedirectToPage("Index");
    }
}
