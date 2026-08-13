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
        return Page();
    }
}
