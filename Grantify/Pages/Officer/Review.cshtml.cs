using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Officer;

// Officer functionalities 2, 3 and 4 on one page:
//   - see the automated eligibility screening result for this applicant
//   - verify or flag each uploaded document
//   - score the application and move it to its next status
//
// The page itself holds no rules. Every decision is checked inside
// OfficerService, so the same rules would still apply if we later called it
// from an API instead of a page.
public class ReviewModel : OfficerPageModel
{
    private readonly OfficerService _officerService;

    public ReviewModel(OfficerService officerService, UserManager<ApplicationUser> userManager)
        : base(userManager)
    {
        _officerService = officerService;
    }

    // Everything the page displays. Null means the id was not found.
    public ReviewDetail? Detail { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Detail = await _officerService.GetReviewDetailAsync(id);

        if (Detail is null)
        {
            return NotFound();
        }

        return Page();
    }

    // Claim the application: Submitted -> UnderReview.
    public async Task<IActionResult> OnPostStartReviewAsync(int id)
    {
        var officer = await GetOfficerAsync();
        ShowResult(await _officerService.StartReviewAsync(id, officer.UserId, officer.Name));
        return RedirectToPage(new { id });
    }

    // Save the score, the remarks and the new status.
    public async Task<IActionResult> OnPostDecisionAsync(
        int id, ApplicationStatus newStatus, int? score, string? remarks)
    {
        var officer = await GetOfficerAsync();
        ShowResult(await _officerService.SaveDecisionAsync(
            id, newStatus, score, remarks, officer.UserId, officer.Name));

        // Redirect after POST: refreshing the page must not save twice.
        return RedirectToPage(new { id });
    }

    // Mark one uploaded file as verified or flagged.
    public async Task<IActionResult> OnPostDocumentAsync(
        int id, int documentId, DocumentStatus documentStatus, string? note)
    {
        var officer = await GetOfficerAsync();
        ShowResult(await _officerService.SetDocumentStatusAsync(
            documentId, documentStatus, note, officer.UserId, officer.Name));

        return RedirectToPage(new { id });
    }
}
