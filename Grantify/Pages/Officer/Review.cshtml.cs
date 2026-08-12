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

    // Which page the officer arrived from, so "Back" returns them there.
    // "documents" = the document verification queue; anything else = the
    // application queue. It travels in the URL and is passed through every
    // save, so the officer never loses their place.
    [BindProperty(SupportsGet = true)]
    public string? From { get; set; }

    // True when the officer came from the document queue.
    public bool CameFromDocuments => string.Equals(From, "documents", StringComparison.OrdinalIgnoreCase);

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
        return RedirectToPage(new { id, from = From });
    }

    // Save the score, the remarks and the new status.
    public async Task<IActionResult> OnPostDecisionAsync(
        int id, ApplicationStatus newStatus, int? score, string? remarks)
    {
        var officer = await GetOfficerAsync();
        ShowResult(await _officerService.SaveDecisionAsync(
            id, newStatus, score, remarks, officer.UserId, officer.Name));

        // Redirect after POST: refreshing the page must not save twice.
        return RedirectToPage(new { id, from = From });
    }

    // Opens the actual uploaded file so the officer can read it before deciding.
    // PDFs and images render in the browser; anything else downloads.
    //
    // Access is already restricted to the Officer role by the folder rule in
    // Program.cs, so there is no extra check here.
    public async Task<IActionResult> OnGetFileAsync(int id, int documentId)
    {
        var file = await _officerService.GetDocumentFileAsync(documentId);

        if (file is null)
        {
            // Most likely a demo record, whose StoragePath points at no real file.
            StatusMessage = "That file is not available. Demo applications have no real uploads behind them.";
            StatusIsError = true;
            return RedirectToPage(new { id, from = From });
        }

        // Stored in S3: send the officer to a temporary link. The bucket stays
        // private, so this link is the only way in and it expires shortly.
        if (file.IsRemote)
        {
            return Redirect(file.Url);
        }

        // Stored on this machine (local development): serve the file ourselves.
        // No download name means the browser shows it in a tab, which is what an
        // officer actually wants when checking a transcript.
        return PhysicalFile(file.Url, file.ContentType);
    }

    // Mark one uploaded file as verified or flagged.
    public async Task<IActionResult> OnPostDocumentAsync(
        int id, int documentId, DocumentStatus documentStatus, string? note)
    {
        var officer = await GetOfficerAsync();
        ShowResult(await _officerService.SetDocumentStatusAsync(
            documentId, documentStatus, note, officer.UserId, officer.Name));

        return RedirectToPage(new { id, from = From });
    }
}
