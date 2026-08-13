using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.ScholarshipCategories;

public class DeleteModel : AdminPageModel
{
    private readonly ScholarshipCategoryService _categories;

    public DeleteModel(ScholarshipCategoryService categories)
    {
        _categories = categories;
    }

    [BindProperty]
    public ScholarshipCategory? Category { get; set; }

    public bool InUse { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Category = await _categories.GetByIdAsync(id);
        if (Category is not null)
        {
            InUse = await _categories.IsInUseAsync(id);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Category is null)
        {
            return RedirectToPage("Index");
        }

        if (await _categories.IsInUseAsync(Category.Id))
        {
            SetMessage("This category is still assigned to one or more scholarships and cannot be deleted.", isError: true);
            return RedirectToPage("Index");
        }

        var name = (await _categories.GetByIdAsync(Category.Id))?.Name ?? "Category";
        var deleted = await _categories.DeleteAsync(Category.Id);
        if (deleted)
        {
            SetMessage($"Category \"{name}\" was deleted.");
        }

        return RedirectToPage("Index");
    }
}
