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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Category = await _categories.GetByIdAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Category is not null)
        {
            var name = (await _categories.GetByIdAsync(Category.Id))?.Name ?? "Category";
            var deleted = await _categories.DeleteAsync(Category.Id);
            if (deleted)
            {
                SetMessage($"Category \"{name}\" was deleted.");
            }
        }

        return RedirectToPage("Index");
    }
}
