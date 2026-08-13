using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.ScholarshipCategories;

public class DetailsModel : AdminPageModel
{
    private readonly ScholarshipCategoryService _categories;

    public DetailsModel(ScholarshipCategoryService categories)
    {
        _categories = categories;
    }

    public ScholarshipCategory? Category { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Category = await _categories.GetByIdAsync(id);

        // A link to a record that has since been deleted is a "not found", not a
        // page with empty boxes on it.
        if (Category is null)
        {
            return NotFound();
        }

        return Page();
    }
}
