using Grantify.Data;
using Grantify.Models;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Services;

public class AnalyticsService
{
    private readonly AppDbContext _db;

    public AnalyticsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AnalyticsSummary> GetSummaryAsync()
    {
        var today = DateTime.Today;

        var totalActiveScholarships = await _db.Scholarships
            .CountAsync(s => s.IsPublished && s.Deadline.Date >= today);

        var totalRegisteredUsers = await _db.Users.CountAsync();

        var totalApplications = await _db.ScholarshipApplications.CountAsync();

        var approvedApplications = await _db.ScholarshipApplications
            .CountAsync(a => a.Status == ApplicationStatus.Approved);

        var approvalRate = totalApplications == 0
            ? 0m
            : Math.Round(approvedApplications * 100m / totalApplications, 1);

        return new AnalyticsSummary
        {
            TotalActiveScholarships = totalActiveScholarships,
            TotalRegisteredUsers = totalRegisteredUsers,
            TotalApplications = totalApplications,
            ApprovalRatePercent = approvalRate
        };
    }

    public async Task<List<ScholarshipStatusBreakdown>> GetStatusBreakdownAsync()
    {
        var scholarships = await _db.Scholarships
            .Include(s => s.Applications)
            .ToListAsync();

        return scholarships
            .Select(s => new ScholarshipStatusBreakdown
            {
                ScholarshipName = s.Name,
                Pending = s.Applications
                    .Where(a => a.Status is ApplicationStatus.Submitted
                        or ApplicationStatus.UnderReview
                        or ApplicationStatus.Shortlisted)
                    .Count(),
                Approved = s.Applications.Where(a => a.Status == ApplicationStatus.Approved).Count(),
                Rejected = s.Applications.Where(a => a.Status == ApplicationStatus.Rejected).Count(),
                Total = s.Applications.Count
            })
            .OrderByDescending(b => b.Total)
            .ToList();
    }

    public async Task<List<Scholarship>> GetUpcomingDeadlinesAsync(int count = 5)
    {
        var today = DateTime.Today;

        return await _db.Scholarships
            .Where(s => s.IsPublished && s.Deadline.Date >= today)
            .OrderBy(s => s.Deadline)
            .Take(count)
            .ToListAsync();
    }
}

public class AnalyticsSummary
{
    public int TotalActiveScholarships { get; set; }
    public int TotalRegisteredUsers { get; set; }
    public int TotalApplications { get; set; }
    public decimal ApprovalRatePercent { get; set; }
}

public class ScholarshipStatusBreakdown
{
    public string ScholarshipName { get; set; } = string.Empty;
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Total { get; set; }
}
