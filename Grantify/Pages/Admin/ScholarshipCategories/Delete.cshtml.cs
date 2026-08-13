using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.ScholarshipCategories;

// OWNER: Member C (Admin role). Master data - scholarship categories.
//
// Deleting is refused while any scholarship still points at this row. The check
// runs BOTH when the page is drawn (so the button is disabled and the reason is
// on screen) and again on the POST, because a disabled button is a courtesy,
// not a rule — another administrator could link a scholarship to it in between.
public class DeleteModel : AdminPageModel
{
    private readonly ScholarshipCategoryService _categories;

    public DeleteModel(ScholarshipCategoryService categories)
    {
        _categories = categories;
    }

    [BindProperty]
    public int Id { get; set; }

    public ScholarshipCategory? Category { get; private set; }

    // True when a scholarship still links to this category.
    public bool InUse { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Category = await _categories.GetByIdAsync(id);
        if (Category is null)
        {
            return NotFound();
        }

        Id = Category.Id;
        InUse = await _categories.IsInUseAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Category = await _categories.GetByIdAsync(Id);
        if (Category is null)
        {
            return NotFound();
        }

        if (await _categories.IsInUseAsync(Id))
        {
            // Stay on the page with the reason showing, rather than bouncing
            // back to the list where it is not obvious what was refused.
            InUse = true;
            ModelState.AddModelError(string.Empty,
                "This category is still assigned to one or more scholarships and cannot be deleted.");
            return Page();
        }

        var name = Category.Name;
        await _categories.DeleteAsync(Id);

        SetMessage($"Category \"{name}\" was deleted.");
        return RedirectToPage("Index");
    }
}
