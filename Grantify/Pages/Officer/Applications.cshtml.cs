using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Officer;

// Officer functionality 1: the incoming application queue, with filters.
//
// All filters travel in the query string (?status=Submitted&scholarshipId=2...).
// That means an officer can bookmark or share a filtered view, and pressing
// back/refresh keeps the same list.
public class ApplicationsModel : OfficerPageModel
{
    private readonly OfficerService _officerService;

    public ApplicationsModel(OfficerService officerService, UserManager<ApplicationUser> userManager)
        : base(userManager)
    {
        _officerService = officerService;
    }

    // ----- Filter values, read from the query string -----

    [BindProperty(SupportsGet = true)]
    public int? ScholarshipId { get; set; }

    [BindProperty(SupportsGet = true)]
    public ApplicationStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    // Default true: an officer normally only wants unfinished work.
    [BindProperty(SupportsGet = true)]
    public bool OnlyOpen { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public QueueSort Sort { get; set; } = QueueSort.OldestFirst;

    // ----- What the page shows -----

    public List<QueueItem> Items { get; private set; } = new();

    // Used to fill the "scholarship" dropdown.
    public List<Scholarship> Scholarships { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Scholarships = await _officerService.GetScholarshipsWithApplicationsAsync();

        Items = await _officerService.GetQueueAsync(new QueueFilter
        {
            ScholarshipId = ScholarshipId,
            Status = Status,
            Search = Search,
            OnlyOpen = OnlyOpen,
            Sort = Sort
        });
    }

    // "Start review" straight from the list, so an officer can claim an
    // application without opening it first.
    public async Task<IActionResult> OnPostStartReviewAsync(int id)
    {
        var officer = await GetOfficerAsync();
        ShowResult(await _officerService.StartReviewAsync(id, officer.UserId, officer.Name));

        // Redirect after POST so a browser refresh does not repeat the action,
        // and keep the officer's filters by passing them back in the URL.
        return RedirectToPage(new { ScholarshipId, Status, Search, OnlyOpen, Sort });
    }
}
