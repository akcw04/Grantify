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

    // Opens the uploaded file from the verification queue, so an officer working
    // through the pile does not have to open each application first.
    // Same behaviour as the review page: readable types show in the browser.
    public async Task<IActionResult> OnGetFileAsync(int documentId)
    {
        var file = await _officerService.GetDocumentFileAsync(documentId);

        if (file is null)
        {
            StatusMessage = "That file is not available. Demo applications have no real uploads behind them.";
            StatusIsError = true;
            return RedirectToPage(new { Status });
        }

        var inline = file.ContentType is "application/pdf" or "text/plain"
                     || file.ContentType.StartsWith("image/");

        return inline
            ? PhysicalFile(file.FullPath, file.ContentType)
            : PhysicalFile(file.FullPath, file.ContentType, file.FileName);
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
