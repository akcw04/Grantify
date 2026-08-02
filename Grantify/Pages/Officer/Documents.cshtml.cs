using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Officer;

// Officer functionality 2: document verification, seen across ALL applications
// at once. The Review page checks documents for one student; this page is for
// working through a pile of them quickly.
//
// Task 2: this is the page that will show what Amazon Textract read out of each
// file, so the officer only confirms instead of reading every document.
public class DocumentsModel : OfficerPageModel
{
    private readonly OfficerService _officerService;

    public DocumentsModel(OfficerService officerService, UserManager<ApplicationUser> userManager)
        : base(userManager)
    {
        _officerService = officerService;
    }

    // Empty = show everything not yet settled (pending + flagged).
    [BindProperty(SupportsGet = true)]
    public DocumentStatus? Status { get; set; }

    public List<DocumentQueueItem> Items { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Items = await _officerService.GetDocumentQueueAsync(Status);
    }

    // Verify or flag one document straight from the list.
    public async Task<IActionResult> OnPostSetStatusAsync(
        int documentId, DocumentStatus documentStatus, string? note)
    {
        var officer = await GetOfficerAsync();
        ShowResult(await _officerService.SetDocumentStatusAsync(
            documentId, documentStatus, note, officer.UserId, officer.Name));

        // Keep the officer on the same filtered list after the redirect.
        return RedirectToPage(new { Status });
    }
}
