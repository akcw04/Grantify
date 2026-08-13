using Grantify.Models;
using Grantify.Services;

namespace Grantify.Pages.Admin.ScholarshipCategories;

public class IndexModel : AdminPageModel
{
    private readonly ScholarshipCategoryService _categories;

    public IndexModel(ScholarshipCategoryService categories)
    {
        _categories = categories;
    }

    public List<ScholarshipCategory> Categories { get; set; } = new();

    public async Task OnGetAsync()
    {
        Categories = await _categories.GetAllAsync();
    }
}
