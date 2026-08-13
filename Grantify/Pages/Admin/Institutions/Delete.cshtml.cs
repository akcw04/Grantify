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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Institution = await _institutions.GetByIdAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Institution is not null)
        {
            var name = (await _institutions.GetByIdAsync(Institution.Id))?.Name ?? "Institution";
            var deleted = await _institutions.DeleteAsync(Institution.Id);
            if (deleted)
            {
                SetMessage($"Institution \"{name}\" was deleted.");
            }
        }

        return RedirectToPage("Index");
    }
}
