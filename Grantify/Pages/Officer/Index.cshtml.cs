using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Identity;

namespace Grantify.Pages.Officer;

// Landing page of the Officer area: a summary of how much work is waiting.
// Access rule: only the "Officer" role can open pages in this folder (Program.cs).
public class IndexModel : OfficerPageModel
{
    private readonly OfficerService _officerService;

    public IndexModel(OfficerService officerService, UserManager<ApplicationUser> userManager)
        : base(userManager)
    {
        _officerService = officerService;
    }

    // The counts shown in the tiles. Filled in by OnGetAsync.
    public OfficerDashboard Dashboard { get; private set; } = new();

    // The few applications the officer should look at first.
    public List<QueueItem> NextUp { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Dashboard = await _officerService.GetDashboardAsync();

        // Oldest unfinished applications — the ones that have been waiting longest.
        var queue = await _officerService.GetQueueAsync(new QueueFilter
        {
            OnlyOpen = true,
            Sort = QueueSort.OldestFirst
        });

        NextUp = queue.Take(5).ToList();
    }
}
