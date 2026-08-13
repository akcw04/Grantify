using Grantify.Models;
using Grantify.Services;

namespace Grantify.Pages.Admin.Institutions;

public class IndexModel : AdminPageModel
{
    private readonly InstitutionService _institutions;

    public IndexModel(InstitutionService institutions)
    {
        _institutions = institutions;
    }

    public List<Institution> Institutions { get; set; } = new();

    public async Task OnGetAsync()
    {
        Institutions = await _institutions.GetAllAsync();
    }
}
