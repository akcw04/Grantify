using Grantify.Models;
using Grantify.Services;

namespace Grantify.Pages.Admin.Scholarships;

public class IndexModel : AdminPageModel
{
    private readonly ScholarshipAdminService _scholarships;

    public IndexModel(ScholarshipAdminService scholarships)
    {
        _scholarships = scholarships;
    }

    public List<Scholarship> Scholarships { get; set; } = new();

    public async Task OnGetAsync()
    {
        Scholarships = await _scholarships.GetAllAsync();
    }
}
