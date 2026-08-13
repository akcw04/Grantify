using Grantify.Models;
using Grantify.Services;

namespace Grantify.Pages.Admin.Analytics;

public class IndexModel : AdminPageModel
{
    private readonly AnalyticsService _analytics;

    public IndexModel(AnalyticsService analytics)
    {
        _analytics = analytics;
    }

    public AnalyticsSummary Summary { get; set; } = new();
    public List<ScholarshipStatusBreakdown> StatusBreakdown { get; set; } = new();
    public List<Scholarship> UpcomingDeadlines { get; set; } = new();

    public async Task OnGetAsync()
    {
        Summary = await _analytics.GetSummaryAsync();
        StatusBreakdown = await _analytics.GetStatusBreakdownAsync();
        UpcomingDeadlines = await _analytics.GetUpcomingDeadlinesAsync();
    }
}
