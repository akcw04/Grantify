using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.Institutions;

public class DeleteModel : AdminPageModel
{
    private readonly InstitutionService _institutions;

    public DeleteModel(InstitutionService institutions)
    {
        _institutions = institutions;
    }

    [BindProperty]
    public Institution? Institution { get; set; }

    public bool InUse { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Institution = await _institutions.GetByIdAsync(id);
        if (Institution is not null)
        {
            InUse = await _institutions.IsInUseAsync(id);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Institution is null)
        {
            return RedirectToPage("Index");
        }

        if (await _institutions.IsInUseAsync(Institution.Id))
        {
            SetMessage("This institution is still assigned to one or more scholarships and cannot be deleted.", isError: true);
            return RedirectToPage("Index");
        }

        var name = (await _institutions.GetByIdAsync(Institution.Id))?.Name ?? "Institution";
        var deleted = await _institutions.DeleteAsync(Institution.Id);
        if (deleted)
        {
            SetMessage($"Institution \"{name}\" was deleted.");
        }

        return RedirectToPage("Index");
    }
}
