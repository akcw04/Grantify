using Grantify.Models;
using Grantify.Services;

namespace Grantify.Pages.Admin.IntakePeriods;

public class IndexModel : AdminPageModel
{
    private readonly IntakePeriodService _intakePeriods;

    public IndexModel(IntakePeriodService intakePeriods)
    {
        _intakePeriods = intakePeriods;
    }

    public List<IntakePeriod> IntakePeriods { get; set; } = new();

    public async Task OnGetAsync()
    {
        IntakePeriods = await _intakePeriods.GetAllAsync();
    }
}
